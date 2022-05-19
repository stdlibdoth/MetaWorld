using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Rendering;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Mathematics;


public class MeshGenerator : MonoBehaviour,IMeshControl
{
    [SerializeField] private ColliderSpawner m_colliderSpawner;
    [SerializeField] private VoxelSpawner m_voxelChunkSpawner;

    [SerializeField] private float m_renderExtent;
    [SerializeField] private string m_voxelLayer;
    [SerializeField] private float m_updateDist;
    [SerializeField] private int m_meshGenBatchInterval;
    [SerializeField] private int m_meshGenBatchSize;


    private Dictionary<Vector3Int, Voxel[]> m_chunkData;
    private Dictionary<Vector3Int, VoxelChunk> m_chunks;
    private List<Vector3Int> m_chunkReleaseBuffer;
    private HashSet<Vector3Int> m_exportingChunks;
    private HashSet<Vector3Int> m_dataChangeFlags;

    //Mesh Generation
    private JobHandle m_MeshGenHandle;
    private Mesh.MeshDataArray m_meshDataArray;
    private NativeList<Vector3Int> m_genChunks;
    private int m_meshGenState;
    private NativeQueue<Vector3Int> m_meshGenBuffer;
    private NativeHashSet<Vector3Int> m_meshGenBufferHash;
    private int m_meshGenBufferCount;
    private bool m_continuousUpdateFlag;
    private FrameTimer m_chunkReleaseBufferTimer;

    //Generation Range
    private Vector3 m_prevDrawPos;
    private MinMaxInt m_rangeX;
    private MinMaxInt m_rangeY;
    private MinMaxInt m_rangeZ;
    private int m_voxelExtent;
    private Vector3 m_center;
    private Vector3Int m_centerCoord;

    public ColliderSpawner ColliderSpawner { get { return m_colliderSpawner; } }

    #region Mono Callbacks

    private void OnDestroy()
    {
        m_meshGenBuffer.Dispose();
        m_meshGenBufferHash.Dispose();
        m_genChunks.Dispose();
    }

    private void Awake()
    {
        m_chunkReleaseBuffer = new List<Vector3Int>();
        m_chunkData = new Dictionary<Vector3Int, Voxel[]>();
        m_chunks = new Dictionary<Vector3Int, VoxelChunk>();
        m_exportingChunks = new HashSet<Vector3Int>();
        m_dataChangeFlags = new HashSet<Vector3Int>();
        m_meshGenBuffer = new NativeQueue<Vector3Int>(Allocator.Persistent);
        m_meshGenBufferHash = new NativeHashSet<Vector3Int>(m_meshGenBatchSize, Allocator.Persistent);
        m_genChunks = new NativeList<Vector3Int>(Allocator.Persistent);
        m_meshGenBufferCount = 0;
        m_meshGenState = 0;
        m_continuousUpdateFlag = false;
        m_meshDataArray = new Mesh.MeshDataArray();
        m_chunkReleaseBufferTimer = FrameTimerManager.GetTimer(3, FrameTimerMode.Repeat);
        m_colliderSpawner.Init(m_voxelLayer);
        m_voxelChunkSpawner.Init();
    }


    private void Start()
    {
        m_chunkReleaseBufferTimer.OnTimeUp.AddListener(() =>
        {
            //clear chunk release buffer
            List<Vector3Int> buffer = new List<Vector3Int>(m_chunkReleaseBuffer);
            foreach (var chunk in buffer)
            {
                if (m_meshGenBufferHash.Contains(chunk) || m_genChunks.Contains(chunk))
                    continue;
                if (m_chunks.ContainsKey(chunk))
                {
                    m_voxelChunkSpawner.Release(m_chunks[chunk]);
                    m_chunks.Remove(chunk);
                }
                m_chunkReleaseBuffer.Remove(chunk);
            }
        });
        m_chunkReleaseBufferTimer.Start();
        //UpdateVoxelExtent();
        //LoadInitialChunkData();
        //string path = "F:/Eifle.txt";
        //ReadVoxelData(path);
    }


    private void Update()
    {
        //Update mesh generation range
        if (m_continuousUpdateFlag && Vector3.Distance(m_center, m_prevDrawPos) > m_updateDist)
        {
            UpdateVoxelExtent();
            UpdateAllChunksContinuous();
            m_prevDrawPos = m_center;
        }

        UpdateMeshes();

        UpdateDataExporting();
    }

    #endregion

    #region IMeshControl Implement

    public void SetCenter(Vector3 center, bool continuous_mode)
    {
        m_center = center;
        if(!continuous_mode)
            m_prevDrawPos = m_center;
        UpdateVoxelExtent();
    }

    public Vector3Int GetCoordRangeMin()
    {
        return new Vector3Int(m_rangeX.min, m_rangeY.min, m_rangeZ.min);
    }
    public Vector3Int GetCoordRangeMax()
    {
        return new Vector3Int(m_rangeX.max, m_rangeY.max, m_rangeZ.max);
    }

    public void SetRenderExtent(float extent)
    {
        m_renderExtent = extent;
        UpdateVoxelExtent();
        if (m_continuousUpdateFlag)
            UpdateAllChunksContinuous();
        else
        {
            ClearAllChunkInternal();
            Vector3Int min = GetChunk(new Vector3Int(m_rangeX.min, m_rangeY.min, m_rangeZ.min));
            Vector3Int max = GetChunk(new Vector3Int(m_rangeX.max, m_rangeY.max, m_rangeZ.max));
            LoadMeshdataInternal(min, max, true, true, true);
        }
    }

    public VoxelCoordinate GetVoxelCoordinate(Vector3Int world_coord)
    {
        return GetVoxelCoordInternal(world_coord,VoxelManager.chunkSize);
    }

    public Vector3Int GetWorldCoordinate(Vector3 world_pos)
    {
        return WorldCoordinate(world_pos);
    }

    public Vector3Int GetChunkCoordinate(Vector3Int world_coord)
    {
        return GetChunk(world_coord);
    }

    public Voxel GetVoxelData(Vector3Int world_coord)
    {
        VoxelCoordinate vc = GetVoxelCoordInternal(world_coord, VoxelManager.chunkSize);
        int localIndex = CoordToIndex(vc.localCoord, VoxelManager.chunkSize);
        return m_chunkData[vc.chunkCoord][localIndex];
    }

    public Voxel[] GetRangedVoxelData(Vector3Int min, Vector3Int max)
    {
        int sizeX = max.x - min.x + 1;
        int sizeY = max.y - min.y + 1;
        int sizeZ = max.z - min.z + 1;
        int length = sizeX * sizeY * sizeZ;
        Voxel[] data = new Voxel[length];
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                for (int z = min.z; z <= max.z; z++)
                {
                    VoxelCoordinate vc = GetVoxelCoordInternal(new Vector3Int(x,y,z), VoxelManager.chunkSize);
                    int localIndex = CoordToIndex(vc.localCoord, VoxelManager.chunkSize);
                    data[z * sizeX * sizeY + y * sizeX + x] = m_chunkData[vc.chunkCoord][localIndex];
                }
            }
        }
        return data;
    }

    public void SetVoxelData(Vector3Int world_coord, Voxel data)
    {
        VoxelCoordinate coord = GetVoxelCoordInternal(world_coord, VoxelManager.chunkSize);
        print("world:" + world_coord);
        print("chunk:" + coord.chunkCoord);
        print("local:" + coord.localCoord);
        if (!m_chunkData.ContainsKey(coord.chunkCoord))
            m_chunkData[coord.chunkCoord] = new Voxel[VoxelManager.dataLength];
        m_chunkData[coord.chunkCoord][CoordToIndex(coord.localCoord,VoxelManager.chunkSize)] = data;
        m_dataChangeFlags.Add(coord.chunkCoord);
    }

    public Voxel[] GetChunkData(Vector3Int chunk_coord, bool copy_data)
    {
        if (m_chunkData.ContainsKey(chunk_coord) && !copy_data)
            return m_chunkData[chunk_coord];
        else if(m_chunkData.ContainsKey(chunk_coord) && copy_data)
        {
            int length = m_chunkData[chunk_coord].Length;
            Voxel[] data = new Voxel[length];
            for (int i = 0; i < length; i++)
            {
                data[i] = m_chunkData[chunk_coord][i];
            }
            return data;
        }
        return new Voxel[VoxelManager.chunkSize* VoxelManager.chunkSize* VoxelManager.chunkSize];
    }

    public void SetChunkData(Vector3Int chunk_coord, Voxel[] data)
    {
        int length = VoxelManager.chunkSize * VoxelManager.chunkSize * VoxelManager.chunkSize;
        Voxel[] d = new Voxel[length];
        length = length < data.Length ? length : data.Length;
        for (int i = 0; i < length; i++)
        {
            d[i] = data[i];
        }
        m_chunkData[chunk_coord] = d;
    }

    public void UpdateChunkMeshes(Vector3Int min, Vector3Int max, bool load_data = false, bool check_range = false)
    {
        if (check_range)
            UpdateMeshGenerationRangeForChunks(min, max);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                for (int z = min.z; z <= max.z; z++)
                {
                    Vector3Int chunkCoord = new Vector3Int(x, y, z);
                    if (!m_chunks.ContainsKey(chunkCoord))
                        SpawnVoxelChunk(new Vector3Int(x, y, z)).gameObject.SetActive(true);
                    if (load_data)
                        m_chunkData[chunkCoord] = RequestChuckData(chunkCoord, true);
                    else
                    {
                        print("bg1");
                        BufferMeshGen(chunkCoord);
                    }
                }
            }
        }
    }
    public void UpdateChunkMesh(Vector3Int chunk, bool load_data = false, bool check_range = false)
    {
        if (check_range)
            UpdateMeshGenerationRangeForChunks(chunk, chunk);

        if (!m_chunks.ContainsKey(chunk))
            SpawnVoxelChunk(chunk).gameObject.SetActive(true);
        if (load_data)
            m_chunkData[chunk] = RequestChuckData(chunk, true);
        else
        {
            print("bg2");
            BufferMeshGen(chunk);
        }
    }

    public void LoadMeshData(Vector3Int min, Vector3Int max, bool update_mesh = false, bool show_mesh = false, bool check_range = false)
    {
        LoadMeshdataInternal(min, max, update_mesh, show_mesh, check_range);
    }

    public void LoadMeshData(Vector3Int chunk, bool update_mesh = false, bool show_mesh = false, bool check_range = false)
    {
        if (check_range)
            UpdateMeshGenerationRangeForChunks(chunk, chunk);

        if (update_mesh && !m_chunks.ContainsKey(chunk))
        {
            SpawnVoxelChunk(chunk).gameObject.SetActive(show_mesh);
        }
        m_chunkData[chunk] = RequestChuckData(chunk, update_mesh);
    }

    public void SetUpdateMode(bool continuous_update)
    {
        m_continuousUpdateFlag = continuous_update;
    }

    public void ClearChunk(Vector3Int chunk, bool clear_mesh = true, bool clear_data = true)
    {
        if(clear_mesh && m_chunks.ContainsKey(chunk))
        {
            m_voxelChunkSpawner.Release(m_chunks[chunk]);
            m_chunks.Remove(chunk);
        }
        if(clear_data)
            m_chunkData.Remove(chunk);
    }

    public void ClearAllChunk(bool clear_mesh = true, bool clear_data = true)
    {
        ClearAllChunkInternal(clear_mesh, clear_data);
    }
    #endregion

    private void LoadMeshdataInternal(Vector3Int min, Vector3Int max, bool update_mesh = false, bool show_mesh = false, bool check_range = false)
    {
        if (check_range)
            UpdateMeshGenerationRangeForChunks(min, max);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                for (int z = min.z; z <= max.z; z++)
                {
                    Vector3Int chunkCoord = new Vector3Int(x, y, z);
                    if (update_mesh && !m_chunks.ContainsKey(chunkCoord))
                    {
                        SpawnVoxelChunk(new Vector3Int(x, y, z)).gameObject.SetActive(show_mesh);
                    }
                    m_chunkData[chunkCoord] = RequestChuckData(chunkCoord, update_mesh);
                }
            }
        }
    }

    private void ClearAllChunkInternal(bool clear_mesh = true, bool clear_data = true)
    {
        m_MeshGenHandle.Complete();
        if (m_meshGenState !=0)
            m_meshDataArray.Dispose();
        m_meshGenState = 0;
        m_meshGenBufferCount = 0;
        m_meshGenBuffer.Clear();
        m_meshGenBufferHash.Clear();
        m_genChunks.Clear();


        List<Vector3Int> c = new List<Vector3Int>(m_chunkData.Keys);
        if (clear_data)
        {
            foreach (var v3 in c)
                m_chunkData.Remove(v3);
        }
        if (clear_mesh)
        {
            c = new List<Vector3Int>(m_chunks.Keys);
            foreach (var v3 in c)
            {
                m_voxelChunkSpawner.Release(m_chunks[v3]);
                m_chunks.Remove(v3);
            }
        }
    }

    private VoxelChunk SpawnVoxelChunk(Vector3Int chunk_coord)
    {
        VoxelChunk chunk = m_voxelChunkSpawner.Get().Init(chunk_coord, VoxelManager.chunkSize, this);
        chunk.transform.position = chunk_coord * VoxelManager.chunkSize;
        m_chunks[chunk_coord] = chunk;
        return chunk;
    }

    private void LoadInitialChunkData()
    {
        Vector3Int min = GetChunk(new Vector3Int(m_rangeX.min, m_rangeY.min, m_rangeZ.min));
        Vector3Int max = GetChunk(new Vector3Int(m_rangeX.max, m_rangeY.max, m_rangeZ.max));

        UpdateMeshGenerationRangeForChunks(min, max);
        int size = VoxelManager.chunkSize;
        for (int x = min.x -1; x <= max.x+1; x++)
        {
            for (int y = min.y -1; y <= max.y+1; y++)
            {
                for (int z = min.z-1; z <= max.z+1; z++)
                {
                    Vector3Int chunkCoord = new Vector3Int(x, y, z);
                    bool updateMesh = (x == min.x - 1 || x == max.x + 1 || y == min.y - 1 || y == max.y + 1 || z == min.z - 1 || z == max.z + 1);
                    m_chunkData[chunkCoord] = RequestChuckData(chunkCoord, !updateMesh);
                    if (!m_chunks.ContainsKey(chunkCoord))
                    {
                        SpawnVoxelChunk(new Vector3Int(x, y, z)).gameObject.SetActive(true);
                    }
                }
            }
        }
    }

    private Vector3Int WorldCoordinate(Vector3 world_pos)
    {
        Vector3 v3 = world_pos / VoxelManager.voxelSize;
        Vector3Int coord = new Vector3Int((int)math.floor(v3.x), (int)math.floor(v3.y), (int)math.floor(v3.z));
        return coord;
    }

    private void UpdateVoxelExtent()
    {
        float vSize = VoxelManager.voxelSize;
        m_voxelExtent = Mathf.FloorToInt(m_renderExtent / VoxelManager.voxelSize);
        m_centerCoord = new Vector3Int(Mathf.FloorToInt(m_center.x / vSize), Mathf.FloorToInt(m_center.y / vSize), Mathf.FloorToInt(m_center.z / vSize));
        m_rangeX = new MinMaxInt(m_centerCoord.x - m_voxelExtent, m_centerCoord.x + m_voxelExtent - 1);
        m_rangeY = new MinMaxInt(m_centerCoord.y - m_voxelExtent, m_centerCoord.y + m_voxelExtent - 1);
        m_rangeZ = new MinMaxInt(m_centerCoord.z - m_voxelExtent, m_centerCoord.z + m_voxelExtent - 1);
    }

    private void UpdateMeshGenerationRangeForChunks(Vector3Int min, Vector3Int max)
    {
        int size = VoxelManager.chunkSize;
        int xN = size - Mathf.Abs(m_rangeX.min - (min.x + 1) * size);
        int xP = Mathf.Abs(m_rangeX.max - max.x * size);
        int yN = size - Mathf.Abs(m_rangeY.min - (min.y + 1) * size);
        int yP = Mathf.Abs(m_rangeY.max - max.y * size);
        int zN = size - Mathf.Abs(m_rangeZ.min - (min.z + 1) * size);
        int zP = Mathf.Abs(m_rangeZ.max - max.z * size);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                for (int z = min.z; z <= max.z; z++)
                {
                    Vector3Int chunkCoord = new Vector3Int(x, y, z);
                    int xMin, yMin, zMin, xMax, yMax, zMax;
                    xMin = yMin = zMin = 0;
                    xMax = yMax = zMax = VoxelManager.chunkSize - 1;

                    if (x == min.x)
                        xMin = xN;
                    if (x == max.x)
                        xMax = xP;
                    if (y == min.y)
                        yMin = yN;
                    if (y == max.y)
                        yMax = yP;
                    if (z == min.z)
                        zMin = zN;
                    if (z == max.z)
                        zMax = zP;

                    Vector3Int drawRangeMin = new Vector3Int(xMin, yMin, zMin);
                    Vector3Int drawRangeMax = new Vector3Int(xMax, yMax, zMax);

                    if (!m_chunks.ContainsKey(chunkCoord))
                    {
                        SpawnVoxelChunk(new Vector3Int(x, y, z)).gameObject.SetActive(true);
                    }

                    if (m_chunks[chunkCoord].drawRangeMin != drawRangeMin
                        || m_chunks[chunkCoord].drawRangeMax != drawRangeMax)
                    {
                        m_chunks[chunkCoord].SetDrawRange(drawRangeMin, drawRangeMax);
                    }
                }
            }
        }
    }

    private void UpdateMeshes()
    {
        //Mesh calculation
        if (m_meshGenState == 0 && m_meshGenBufferCount > 0)
        {
            m_meshGenState = 1;
            int batchCount = m_meshGenBufferCount < m_meshGenBatchSize ? m_meshGenBufferCount : m_meshGenBatchSize;
            Vector3Int[] min = new Vector3Int[batchCount];
            Vector3Int[] max = new Vector3Int[batchCount];
            for (int i = 0; i < batchCount; i++)
            {
                Vector3Int genChunk = m_meshGenBuffer.Dequeue();
                min[i] = m_chunks[genChunk].drawRangeMin;
                max[i] = m_chunks[genChunk].drawRangeMax;
                m_meshGenBufferHash.Remove(genChunk);
                m_genChunks.Add(genChunk);
                m_meshGenBufferCount--;
            }
            GenerateMeshes(m_genChunks, min, max);
        }
        //Apply mesh
        else if (m_meshGenState == 1 && m_MeshGenHandle.IsCompleted)
        {
            m_meshGenState = 2;
            Mesh[] meshes = new Mesh[m_genChunks.Length];
            int chunkCount = m_genChunks.Length;
            float length = VoxelManager.chunkSize * VoxelManager.voxelSize;
            for (int i = 0; i < chunkCount; i++)
            {
                meshes[i] = m_chunks[m_genChunks[i]].meshFilter.mesh;
                Vector3 center = new Vector3(length * 0.5f, length * 0.5f, length * 0.5f);
                meshes[i].bounds = new Bounds(center, new Vector3(length, length, length));
            }
            m_MeshGenHandle.Complete();
            Mesh.ApplyAndDisposeWritableMeshData(m_meshDataArray, meshes);
            m_genChunks.Clear();
            m_meshGenState = 0;
        }
    }

    private void UpdateAllChunksContinuous()
    {
        Vector3Int min = GetChunk(new Vector3Int(m_rangeX.min, m_rangeY.min, m_rangeZ.min));
        Vector3Int max = GetChunk(new Vector3Int(m_rangeX.max, m_rangeY.max, m_rangeZ.max));

        int size = VoxelManager.chunkSize;
        int xN = size - Mathf.Abs(m_rangeX.min - (min.x + 1) * size);
        int xP = Mathf.Abs(m_rangeX.max - max.x * size);
        int yN = size - Mathf.Abs(m_rangeY.min - (min.y + 1) * size);
        int yP = Mathf.Abs(m_rangeY.max - max.y * size);
        int zN = size - Mathf.Abs(m_rangeZ.min - (min.z + 1) * size);
        int zP = Mathf.Abs(m_rangeZ.max - max.z * size);

        for (int x = min.x - 2; x <= max.x + 2; x++)
        {
            for (int y = min.y - 2; y <= max.y + 2; y++)
            {
                for (int z = min.z - 2; z <= max.z + 2; z++)
                {

                    //clear data
                    if (x == min.x - 2 || x == max.x + 2
                        || y == min.y - 2 || y == max.y + 2
                        || z == min.z - 2 || z == max.z + 2)
                    {
                        Vector3Int coord = new Vector3Int(x, y, z);
                        if(m_chunkData.ContainsKey(coord)&&
                            !m_exportingChunks.Contains(coord))
                        {
                            m_chunkData.Remove(coord);
                        }
                        continue;
                    }

                    //Deactivate chunks that are not in range,
                    //Load chunk data beyond the range,
                    if (x == min.x - 1 || x == max.x + 1
                        || y == min.y - 1 || y == max.y + 1
                        || z == min.z - 1 || z == max.z + 1)
                    {
                        Vector3Int coord = new Vector3Int(x, y, z);
                        if (m_chunks.ContainsKey(coord))
                        {
                            m_chunkReleaseBuffer.Add(coord);
                            //m_voxelChunkSpawner.Release(m_chunks[coord]);
                            //m_chunks.Remove(coord);
                        }
                        if (!m_chunkData.ContainsKey(coord))
                            m_chunkData[coord] = RequestChuckData(coord, false);
                        continue;
                    }

                    Vector3Int chunkCoord = new Vector3Int(x, y, z);

                    //Check draw range for outmost chunks
                    int xMin, yMin, zMin, xMax, yMax, zMax;
                    xMin = yMin = zMin = 0;
                    xMax = yMax = zMax = VoxelManager.chunkSize - 1;

                    if (x == min.x)
                        xMin = xN;
                    if (x == max.x)
                        xMax = xP;
                    if (y == min.y)
                        yMin = yN;
                    if (y == max.y)
                        yMax = yP;
                    if (z == min.z)
                        zMin = zN;
                    if (z == max.z)
                        zMax = zP;

                    Vector3Int drawRangeMin = new Vector3Int(xMin, yMin, zMin);
                    Vector3Int drawRangeMax = new Vector3Int(xMax, yMax, zMax);
                    //print("Set: "+ chunkCoord+ " " + drawRangeMin + " " + drawRangeMax + " " + m_rangeX.max + "  " + max);


                    if (!m_chunks.ContainsKey(chunkCoord))
                    {
                        SpawnVoxelChunk(new Vector3Int(x, y, z)).gameObject.SetActive(true);
                    }


                    if (m_chunks[chunkCoord].drawRangeMin != drawRangeMin
                        || m_chunks[chunkCoord].drawRangeMax != drawRangeMax)
                    {
                        m_chunks[chunkCoord].SetDrawRange(drawRangeMin, drawRangeMax);
                        BufferMeshGen(chunkCoord);
                    }
                }
            }
        }
    }

    private void UpdateDataExporting()
    {
        if (m_dataChangeFlags.Count == 0)
            return;
        List<Vector3Int> coords = new List<Vector3Int>(m_dataChangeFlags);
        foreach (Vector3Int coord in coords)
        {
            if (m_chunkData.ContainsKey(coord) && !m_exportingChunks.Contains(coord))
            {
                print("Export:" + coord);
                m_exportingChunks.Add(coord);
                VoxelManager.ExportData(m_chunkData[coord], coord,
                    () =>
                    {
                        m_exportingChunks.Remove(coord);
                    });
            }
            m_dataChangeFlags.Remove(coord);
        }
    }

    private Voxel[] RequestChuckData(Vector3Int chunk_coord, bool updateMesh)
    {
        int chunkSize = VoxelManager.chunkSize;
        Voxel[] v = new Voxel[chunkSize * chunkSize * chunkSize];

        string path = VoxelManager.VoxelDataDir + "/" + chunk_coord.ToString() + ".txt";
        bool dataChanged = !File.Exists(path);
        VoxelManager.LoadData(chunk_coord, (data) =>
         {
             m_chunkData[chunk_coord] = data;
             if (m_chunks.ContainsKey(chunk_coord) && updateMesh)
             {
                 BufferMeshGen(chunk_coord);
                 //print("Read Done: " + chunk_coord);
             }
             if(dataChanged)
                 m_dataChangeFlags.Add(chunk_coord);
         });
        return v;
    }

    private Vector3Int GetChunk(Vector3Int voxel_coord)
    {
        int[] xyz = new int[] { voxel_coord.x, voxel_coord.y, voxel_coord.z };

        for (int i = 0; i < xyz.Length; i++)
        {
            xyz[i] = ((xyz[i] - Step(xyz[i])) / VoxelManager.chunkSize) + Step(xyz[i]);
        }
        return new Vector3Int(xyz[0], xyz[1], xyz[2]);
    }
    
    [BurstCompile]
    private VoxelCoordinate GetVoxelCoordInternal(Vector3Int world_coord,int chunk_size)
    {
        NativeArray<int> xyz = new NativeArray<int>(3, Allocator.Temp);
        NativeArray<int> xyz1 = new NativeArray<int>(3, Allocator.Temp);
        xyz[0] = xyz1[0] = world_coord.x;
        xyz[1] = xyz1[1] = world_coord.y;
        xyz[2] = xyz1[2] = world_coord.z;

        for (int i = 0; i < xyz.Length; i++)
        {
            int step = math.select(0, -1, xyz[i] < 0);
            int sign = math.select(1, -1, xyz[i] < 0);
            int coord = xyz[i];
            xyz[i] = ((xyz[i] - step) / chunk_size) + step;
            //xyz1[i] = (-step * chunk_size) + sign * (math.abs(xyz1[i]) % (chunk_size - step));
            xyz1[i] = math.abs(xyz[i] * chunk_size - coord);
        }

        Vector3Int chunk = new Vector3Int(xyz[0], xyz[1], xyz[2]);
        Vector3Int local = new Vector3Int(xyz1[0], xyz1[1], xyz1[2]);
        xyz.Dispose();
        xyz1.Dispose();
        return new VoxelCoordinate
        {
            chunkCoord = chunk,
            localCoord = local,
        };
    }

    [BurstCompile]
    private int Step(int num)
    {
        return math.select(0, -1, num < 0);
    }

    private int CoordToIndex(Vector3Int coord, int dimension)
    {
        return coord.z * (dimension * dimension) + coord.y * dimension + coord.x;
    }

    public void ReadVoxelData(string path)
    {
        //Read data from file
        Voxel[] data = new Voxel[120 * 120 * 120];
        if (!File.Exists(path))
            return;
        using (StreamReader sr = new StreamReader(path))
        {
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                int index = int.Parse(line);
                data[index].render = 1;
                data[index].color = Color.blue;
            }
        }

        for (int x = -3; x < 3; x++)
        {
            for (int y = -3; y < 3; y++)
            {
                for (int z = -3; z < 3; z++)
                {
                    Vector3Int chunkCoord = new Vector3Int(x, y, z);
                    Voxel[] chunkData = new Voxel[20*20*20];
                    m_chunkData[chunkCoord] = chunkData;
                }
            }
        }

        for (int i = 0; i < data.Length; i++)
        {
            int z = i / (120 * 120);
            int y = (i % (120 * 120)) / 120;
            int x = (i % (120 * 120)) % 120;
            int cx = (x / 20) - 3;
            int cy = (y / 20) - 3;
            int cz = (z / 20) - 3;

            int lx = x % 20;
            int ly = y % 20;
            int lz = z % 20;

            Vector3Int chunkCoord = new Vector3Int(cx, cy, cz);
            int localIndex = lz * (20 * 20) + ly * 20 + lx;
            m_chunkData[chunkCoord][localIndex] = data[i];
        }


        for (int x = -3; x < 3; x++)
        {
            for (int y = -3; y < 3; y++)
            {
                for (int z = -3; z < 3; z++)
                {
                    //serilaize data
                    Vector3Int chunkCoord = new Vector3Int(x, y, z);
                    m_exportingChunks.Add(chunkCoord);
                    m_dataChangeFlags.Add(chunkCoord);
                    VoxelManager.ExportData(m_chunkData[chunkCoord], chunkCoord,
                        () =>
                        {
                            m_exportingChunks.Remove(chunkCoord);
                            m_dataChangeFlags.Remove(chunkCoord);
                        });
                }
            }
        }
    }


    #region Mesh Generation

    private void BufferMeshGen(Vector3Int coord)
    {
        if (m_meshGenBufferHash.Contains(coord))
            return;
        m_meshGenBuffer.Enqueue(coord);
        m_meshGenBufferHash.Add(coord);
        m_meshGenBufferCount++;
    }

    private void GenerateMeshes(NativeArray<Vector3Int> chunks,Vector3Int[] min, Vector3Int[] max)
    {
        int chunkCount = chunks.Length;

        //Mesh
        m_meshDataArray = Mesh.AllocateWritableMeshData(chunkCount);
        MeshGenJobs meshGenJobs = new MeshGenJobs();
        meshGenJobs.meshDataArray = m_meshDataArray;

        //Range
        meshGenJobs.drawRangeMin = new NativeArray<Vector3Int>(min.Length, Allocator.Persistent);
        meshGenJobs.drawRangeMax = new NativeArray<Vector3Int>(max.Length, Allocator.Persistent);

        //chunk data
        meshGenJobs.chunkCoords = new NativeArray<Vector3Int>(chunkCount, Allocator.Persistent);
        for (int i = 0; i < chunkCount; i++)
        {
            meshGenJobs.chunkCoords[i] = chunks[i];
            meshGenJobs.drawRangeMin[i] = min[i];
            meshGenJobs.drawRangeMax[i] = max[i];
        }

        //voxel data
        int length = VoxelManager.chunkSize * VoxelManager.chunkSize * VoxelManager.chunkSize;
        int dataCount = chunkCount * length;
        NativeList<Voxel> data = new NativeList<Voxel>(dataCount, Allocator.Temp);
        for (int i = 0; i < chunkCount; i++)
        {
            NativeArray<Voxel> d = new NativeArray<Voxel>(m_chunkData[chunks[i]], Allocator.Persistent);
            data.AddRange(d);
            d.Dispose();
        }
        meshGenJobs.voxelData = data.ToArray(Allocator.Persistent);
        data.Dispose();


        //size
        meshGenJobs._chunkSize = new NativeArray<int>(1, Allocator.Persistent);
        meshGenJobs._chunkSize[0] = VoxelManager.chunkSize;
        meshGenJobs._voxelSize = new NativeArray<float>(1, Allocator.Persistent);
        meshGenJobs._voxelSize[0] = VoxelManager.voxelSize;



        m_MeshGenHandle = meshGenJobs.Schedule(chunkCount, 10);
    }


    [BurstCompile]
    private struct MeshGenJobs : IJobParallelFor
    {

        public Mesh.MeshDataArray meshDataArray;

        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<Voxel> voxelData;

        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<Vector3Int> chunkCoords;

        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<Vector3Int> drawRangeMin;

        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<Vector3Int> drawRangeMax;

        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<int> _chunkSize;

        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<float> _voxelSize;

        public void Execute(int index)
        {
            Mesh.MeshData meshData = meshDataArray[index];

            NativeList<Vector3> verts = new NativeList<Vector3>(Allocator.Temp);
            NativeList<int> tris = new NativeList<int>(Allocator.Temp);
            NativeList<Vector3> normals = new NativeList<Vector3>(Allocator.Temp);
            NativeList<Color> colors = new NativeList<Color>(Allocator.Temp);
            int quadCount = 0;


            void DrawQuad(NativeArray<Vector3> vertices, VoxelDirection dir, Color color)
            {
                verts.AddRange(vertices);

                int i = quadCount * 4;
                NativeArray<int> topology = new NativeArray<int>(6, Allocator.Temp);
                topology[0] = i;
                topology[1] = i + 1;
                topology[2] = i + 3;
                topology[3] = i + 3;
                topology[4] = i + 1;
                topology[5] = i + 2;
                tris.AddRange(topology);
                topology.Dispose();


                NativeArray<Color> cs = new NativeArray<Color>(4, Allocator.Temp);
                cs[0] = color;
                cs[1] = color;
                cs[2] = color;
                cs[3] = color;
                colors.AddRange(cs);
                cs.Dispose();

                Vector3 normal = dir.ToVector3();
                NativeArray<Vector3> norm = new NativeArray<Vector3>(4,Allocator.Temp);
                norm[0] = normal;
                norm[1] = normal;
                norm[2] = normal;
                norm[3] = normal;
                normals.AddRange(norm);
                norm.Dispose();
                quadCount++;
            }

            int Coord2Index(Vector3Int coord, int dimension)
            {
                return coord.z * (dimension * dimension) + coord.y * dimension + coord.x;
            }
            int startIndex = index * _chunkSize[0] * _chunkSize[0] * _chunkSize[0];
            Vector3Int chunkCoord = chunkCoords[index];
            for (int x = drawRangeMin[index].x; x <= drawRangeMax[index].x; x++)
            {
                for (int y = drawRangeMin[index].y; y <= drawRangeMax[index].y; y++)
                {
                    for (int z = drawRangeMin[index].z; z <= drawRangeMax[index].z; z++)
                    {
                        Vector3Int localCoord = new Vector3Int(x, y, z);
                        int localIndex = Coord2Index(localCoord, _chunkSize[0]);
                        //print(localCoord + "  " + localIndex);
                        Voxel voxel = voxelData[startIndex + localIndex];
                        if (voxel.render == 0)
                            continue;

                        //back-----------------------------------------------------
                        Voxel back;
                        if (localCoord.z == 0)
                        {
                            Vector3Int otherLocalCoord = new Vector3Int(localCoord.x, localCoord.y, _chunkSize[0] - 1);
                            int i = chunkCoords.IndexOf(chunkCoord + Vector3Int.back);
                            if (i!= -1)
                            {
                                int otherStartIndex = i * _chunkSize[0] * _chunkSize[0] * _chunkSize[0];
                                back = voxelData[otherStartIndex + Coord2Index(otherLocalCoord, _chunkSize[0])];                          
                            }
                            else
                                back = new Voxel();
                        }
                        else
                            back = voxelData[startIndex + Coord2Index(localCoord + Vector3Int.back, _chunkSize[0])];
                        if (back.render == 0)
                        {
                            //print(chunkCoord + " " + localCoord);
                            NativeArray<Vector3> vertices = new NativeArray<Vector3>(4, Allocator.Temp);
                            vertices[0] = new Vector3(localCoord.x * _voxelSize[0], localCoord.y * _voxelSize[0], localCoord.z * _voxelSize[0]);
                            vertices[1] = new Vector3(localCoord.x * _voxelSize[0], (localCoord.y + 1) * _voxelSize[0], localCoord.z * _voxelSize[0]);
                            vertices[2] = new Vector3((localCoord.x + 1) * _voxelSize[0], (localCoord.y + 1) * _voxelSize[0], localCoord.z * _voxelSize[0]);
                            vertices[3] = new Vector3((localCoord.x + 1) * _voxelSize[0], localCoord.y * _voxelSize[0], localCoord.z * _voxelSize[0]);
                            DrawQuad(vertices, VoxelDirection.Back, voxel.color);
                            vertices.Dispose();
                        }


                        //forward---------------------------------------------------
                        Voxel forward;
                        if (localCoord.z == _chunkSize[0] - 1)
                        {
                            Vector3Int otherLocalCoord = new Vector3Int(localCoord.x, localCoord.y, 0);
                            int i = chunkCoords.IndexOf(chunkCoord + Vector3Int.forward);
                            if (i != -1)
                            {
                                int otherStartIndex = i * _chunkSize[0] * _chunkSize[0] * _chunkSize[0];
                                forward = voxelData[otherStartIndex + Coord2Index(otherLocalCoord, _chunkSize[0])];
                            }
                            else
                                forward = new Voxel();
                        }
                        else
                            forward = voxelData[startIndex + Coord2Index(localCoord + Vector3Int.forward, _chunkSize[0])];
                        if (forward.render == 0)
                        {
                            NativeArray<Vector3> vertices = new NativeArray<Vector3>(4, Allocator.Temp);
                            vertices[0] = new Vector3((localCoord.x + 1) * _voxelSize[0], localCoord.y * _voxelSize[0], (localCoord.z + 1) * _voxelSize[0]);
                            vertices[1] = new Vector3((localCoord.x + 1) * _voxelSize[0], (localCoord.y + 1) * _voxelSize[0], (localCoord.z + 1) * _voxelSize[0]);
                            vertices[2] = new Vector3(localCoord.x * _voxelSize[0], (localCoord.y + 1) * _voxelSize[0], (localCoord.z + 1) * _voxelSize[0]);
                            vertices[3] = new Vector3(localCoord.x * _voxelSize[0], localCoord.y * _voxelSize[0], (localCoord.z + 1) * _voxelSize[0]);
                            DrawQuad(vertices, VoxelDirection.Forward, voxel.color);
                            vertices.Dispose();
                        }

                        //left------------------------------------------------------------
                        Voxel left;
                        if (localCoord.x == 0)
                        {
                            Vector3Int otherLocalCoord = new Vector3Int(_chunkSize[0] - 1, localCoord.y, localCoord.z);
                            int i = chunkCoords.IndexOf(chunkCoord + Vector3Int.left);
                            if (i != -1)
                            {
                                int otherStartIndex = i * _chunkSize[0] * _chunkSize[0] * _chunkSize[0];
                                left = voxelData[otherStartIndex + Coord2Index(otherLocalCoord, _chunkSize[0])];
                            }
                            else
                                left = new Voxel();
                        }
                        else
                            left = voxelData[startIndex + Coord2Index(localCoord + Vector3Int.left, _chunkSize[0])];
                        if (left.render == 0)
                        {
                            NativeArray<Vector3> vertices = new NativeArray<Vector3>(4, Allocator.Temp);
                            vertices[0] = new Vector3(localCoord.x * _voxelSize[0], localCoord.y * _voxelSize[0], (localCoord.z + 1) * _voxelSize[0]);
                            vertices[1] = new Vector3(localCoord.x * _voxelSize[0], (localCoord.y + 1) * _voxelSize[0], (localCoord.z + 1) * _voxelSize[0]);
                            vertices[2] = new Vector3(localCoord.x * _voxelSize[0], (localCoord.y + 1) * _voxelSize[0], localCoord.z * _voxelSize[0]);
                            vertices[3] = new Vector3(localCoord.x * _voxelSize[0], localCoord.y * _voxelSize[0], localCoord.z * _voxelSize[0]);
                            DrawQuad(vertices, VoxelDirection.Left, voxel.color);
                            vertices.Dispose();
                        }

                        //right-------------------------------------------------------------
                        Voxel right;
                        if (localCoord.x == _chunkSize[0] - 1)
                        {
                            Vector3Int otherLocalCoord = new Vector3Int(0, localCoord.y, localCoord.z);
                            int i = chunkCoords.IndexOf(chunkCoord + Vector3Int.right);
                            if (i != -1)
                            {
                                int otherStartIndex = i * _chunkSize[0] * _chunkSize[0] * _chunkSize[0];
                                right = voxelData[otherStartIndex + Coord2Index(otherLocalCoord, _chunkSize[0])];
                            }
                            else
                                right = new Voxel();
                        }
                        else
                            right = voxelData[startIndex + Coord2Index(localCoord + Vector3Int.right, _chunkSize[0])];
                        if (right.render == 0)
                        {
                            NativeArray<Vector3> vertices = new NativeArray<Vector3>(4, Allocator.Temp);
                            vertices[0] = new Vector3((localCoord.x + 1) * _voxelSize[0], localCoord.y * _voxelSize[0], localCoord.z * _voxelSize[0]);
                            vertices[1] = new Vector3((localCoord.x + 1) * _voxelSize[0], (localCoord.y + 1) * _voxelSize[0], localCoord.z * _voxelSize[0]);
                            vertices[2] = new Vector3((localCoord.x + 1) * _voxelSize[0], (localCoord.y + 1) * _voxelSize[0], (localCoord.z + 1) * _voxelSize[0]);
                            vertices[3] = new Vector3((localCoord.x + 1) * _voxelSize[0], localCoord.y * _voxelSize[0], (localCoord.z + 1) * _voxelSize[0]);
                            DrawQuad(vertices, VoxelDirection.Right, voxel.color);
                            vertices.Dispose();
                        }

                        //down---------------------------------------------------------------
                        Voxel down;
                        if (localCoord.y == 0)
                        {
                            Vector3Int otherLocalCoord = new Vector3Int(localCoord.x, _chunkSize[0] - 1, localCoord.z);
                            int i = chunkCoords.IndexOf(chunkCoord + Vector3Int.down);
                            if (i != -1)
                            {
                                int otherStartIndex = i * _chunkSize[0] * _chunkSize[0] * _chunkSize[0];
                                down = voxelData[otherStartIndex + Coord2Index(otherLocalCoord, _chunkSize[0])];
                            }
                            else
                                down = new Voxel();
                        }
                        else
                            down = voxelData[startIndex + Coord2Index(localCoord + Vector3Int.down, _chunkSize[0])];
                        if (down.render == 0)
                        {
                            NativeArray<Vector3> vertices = new NativeArray<Vector3>(4, Allocator.Temp);
                            vertices[0] = new Vector3(localCoord.x * _voxelSize[0], localCoord.y * _voxelSize[0], (localCoord.z + 1) * _voxelSize[0]);
                            vertices[1] = new Vector3(localCoord.x * _voxelSize[0], localCoord.y * _voxelSize[0], localCoord.z * _voxelSize[0]);
                            vertices[2] = new Vector3((localCoord.x + 1) * _voxelSize[0], localCoord.y * _voxelSize[0], localCoord.z * _voxelSize[0]);
                            vertices[3] = new Vector3((localCoord.x + 1) * _voxelSize[0], localCoord.y * _voxelSize[0], (localCoord.z + 1) * _voxelSize[0]);
                            DrawQuad(vertices, VoxelDirection.Down, voxel.color);
                            vertices.Dispose();
                        }

                        //up--------------------------------------------------------------
                        Voxel up;
                        if (localCoord.y == _chunkSize[0] - 1)
                        {
                            Vector3Int otherLocalCoord = new Vector3Int(localCoord.x, 0, localCoord.z);
                            int i = chunkCoords.IndexOf(chunkCoord + Vector3Int.up);
                            if (i != -1)
                            {
                                int otherStartIndex = i * _chunkSize[0] * _chunkSize[0] * _chunkSize[0];
                                up = voxelData[otherStartIndex + Coord2Index(otherLocalCoord, _chunkSize[0])];
                            }
                            else
                                up = new Voxel();
                        }
                        else
                            up = voxelData[startIndex + Coord2Index(localCoord + Vector3Int.up, _chunkSize[0])];
                        if (up.render == 0)
                        {
                            NativeArray<Vector3> vertices = new NativeArray<Vector3>(4, Allocator.Temp);
                            vertices[0] = new Vector3(localCoord.x * _voxelSize[0], (localCoord.y + 1) * _voxelSize[0], localCoord.z * _voxelSize[0]);
                            vertices[1] = new Vector3(localCoord.x * _voxelSize[0], (localCoord.y + 1) * _voxelSize[0], (localCoord.z + 1) * _voxelSize[0]);
                            vertices[2] = new Vector3((localCoord.x + 1) * _voxelSize[0], (localCoord.y + 1) * _voxelSize[0], (localCoord.z + 1) * _voxelSize[0]);
                            vertices[3] = new Vector3((localCoord.x + 1) * _voxelSize[0], (localCoord.y + 1) * _voxelSize[0], localCoord.z * _voxelSize[0]);
                            DrawQuad(vertices, VoxelDirection.Up, voxel.color);
                            vertices.Dispose();
                        }
                    }
                }
            }
            var vertDiscriptors = new NativeArray<UnityEngine.Rendering.VertexAttributeDescriptor>(3, Allocator.Temp);
            vertDiscriptors[0] = new UnityEngine.Rendering.VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.Position);
            vertDiscriptors[1] = new UnityEngine.Rendering.VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.Normal, stream: 1);
            vertDiscriptors[2] = new UnityEngine.Rendering.VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.Color, dimension: 4, stream: 2);
            meshData.SetVertexBufferParams(verts.Length, vertDiscriptors);
            NativeArray<Vector3> vertsData = meshData.GetVertexData<Vector3>();
            NativeArray<Vector3> normalData = meshData.GetVertexData<Vector3>(stream: 1);
            NativeArray<Color> colorData = meshData.GetVertexData<Color>(stream: 2);
            vertsData.CopyFrom(verts);
            normalData.CopyFrom(normals);
            colorData.CopyFrom(colors);

            meshData.SetIndexBufferParams(tris.Length, UnityEngine.Rendering.IndexFormat.UInt32);
            NativeArray<int> indexData = meshData.GetIndexData<int>();
            indexData.CopyFrom(tris);

            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new UnityEngine.Rendering.SubMeshDescriptor(0, tris.Length),UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds);
            tris.Dispose();
            verts.Dispose();
            normals.Dispose();
            colors.Dispose();
        }

    }
}
#endregion
