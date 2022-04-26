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


public class MeshGenerator : MonoBehaviour
{
    [SerializeField] private VoxelChunk m_chunkPrefab;
    [SerializeField] private ColliderSpawner m_colliderSpawner;

    [SerializeField] private float renderExtent;
    [SerializeField] private string m_voxelLayer;
    [SerializeField] private float m_updateDist;
    [SerializeField] private int m_meshGenBatchInterval;
    [SerializeField] private int m_meshGenBatchSize;


    private Dictionary<Vector3Int, Voxel[]> m_chunkData;
    private Dictionary<Vector3Int, VoxelChunk> m_chunks;
    private HashSet<Vector3Int> m_exportingChunks;
    private HashSet<Vector3Int> m_dataChangeFlag;

    //Mesh Generation
    private JobHandle m_MeshGenHandle;
    private Mesh.MeshDataArray m_meshDataArray;
    private NativeList<Vector3Int> m_genChunks;
    private int m_meshGenState;
    private NativeQueue<Vector3Int> m_meshGenBuffer;
    private int m_meshGenBufferCount;


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
        m_genChunks.Dispose();
    }

    private void Awake()
    {
        m_chunkData = new Dictionary<Vector3Int, Voxel[]>();
        m_chunks = new Dictionary<Vector3Int, VoxelChunk>();
        m_exportingChunks = new HashSet<Vector3Int>();
        m_dataChangeFlag = new HashSet<Vector3Int>();
        m_meshGenBuffer = new NativeQueue<Vector3Int>(Allocator.Persistent);
        m_genChunks = new NativeList<Vector3Int>(Allocator.Persistent);
        m_meshGenBufferCount = 0;
        m_center = transform.position;
        m_colliderSpawner.Init(m_voxelLayer);
        m_meshGenState = 0;
        m_meshDataArray = new Mesh.MeshDataArray();
    }


    private void Start()
    {
        UpdateVoxelRange();
        LoadInitialChunkData();
        //string path = "F:/Eifle.txt";
        //ReadVoxelData(path);
    }


    private void Update()
    {
        //Update mesh generation range
        if (Vector3.Distance(transform.position, m_prevDrawPos) > m_updateDist)
        {
            UpdateVoxelRange();
            UpdateChunks();
            m_prevDrawPos = transform.position;
        }

        //Mesh calculation
        if (m_meshGenState == 0 && m_meshGenBufferCount > 0)
        {
            int batchCount = m_meshGenBufferCount < m_meshGenBatchSize ? m_meshGenBufferCount : m_meshGenBatchSize;
            Vector3Int[] min = new Vector3Int[batchCount];
            Vector3Int[] max = new Vector3Int[batchCount];
            for (int i = 0; i < batchCount; i++)
            {
                Vector3Int genChunk = m_meshGenBuffer.Dequeue();
                min[i] = m_chunks[genChunk].drawRangeMin;
                max[i] = m_chunks[genChunk].drawRangeMax;
                m_genChunks.Add(genChunk);
                m_meshGenBufferCount--;
            }
            m_meshGenState = 1;
            GenerateMeshes(m_genChunks, min, max);
        }
        //Apply mesh
        else if(m_meshGenState == 1 && m_MeshGenHandle.IsCompleted)
        {
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
            m_meshGenState = 0;
            m_genChunks.Clear();
        }
    }

    #endregion

    public Voxel[] GetVoxelData(Vector3Int chunk_coord)
    {
        if (m_chunkData.ContainsKey(chunk_coord))
            return m_chunkData[chunk_coord];
        return new Voxel[VoxelManager.chunkSize* VoxelManager.chunkSize* VoxelManager.chunkSize];
    }



    private void LoadInitialChunkData()
    {
        Vector3Int min = GetChunk(new Vector3Int(m_rangeX.min, m_rangeY.min, m_rangeZ.min));
        Vector3Int max = GetChunk(new Vector3Int(m_rangeX.max, m_rangeY.max, m_rangeZ.max));

        UpdateMeshRange(min, max);
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
                }
            }
        }
    }

    private void UpdateVoxelRange()
    {
        m_prevDrawPos = transform.position;
        m_center = transform.position;
        float vSize = VoxelManager.voxelSize;
        m_voxelExtent = Mathf.FloorToInt(renderExtent / VoxelManager.voxelSize);
        m_centerCoord = new Vector3Int(Mathf.FloorToInt(m_center.x / vSize), Mathf.FloorToInt(m_center.y / vSize), Mathf.FloorToInt(m_center.z / vSize));
        m_rangeX = new MinMaxInt(m_centerCoord.x - m_voxelExtent, m_centerCoord.x + m_voxelExtent - 1);
        m_rangeY = new MinMaxInt(m_centerCoord.y - m_voxelExtent, m_centerCoord.y + m_voxelExtent - 1);
        m_rangeZ = new MinMaxInt(m_centerCoord.z - m_voxelExtent, m_centerCoord.z + m_voxelExtent - 1);
    }

    private void UpdateMeshRange(Vector3Int min, Vector3Int max)
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
                        VoxelChunk chunk = Instantiate(m_chunkPrefab).Init(new Vector3Int(x, y, z), VoxelManager.chunkSize, this);
                        chunk.transform.position = new Vector3(x, y, z) * VoxelManager.chunkSize;
                        m_chunks[chunkCoord] = chunk;
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

    private void UpdateChunks()
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

                    //Serialize
                    if (x == min.x - 2 || x == max.x + 2
                        || y == min.y - 2 || y == max.y + 2
                        || z == min.z - 2 || z == max.z + 2)
                    {
                        Vector3Int coord = new Vector3Int(x, y, z);

                        if(m_chunkData.ContainsKey(coord)&&
                            !m_exportingChunks.Contains(coord))
                        {
                            if (m_dataChangeFlag.Contains(coord))
                            {
                                print("Export:" + coord);
                                m_exportingChunks.Add(coord);
                                VoxelManager.ExportData(m_chunkData[coord], coord,
                                    () =>
                                    {
                                        m_chunkData.Remove(coord);
                                        m_exportingChunks.Remove(coord);
                                        m_dataChangeFlag.Remove(coord);
                                    });
                            }
                            else
                            {
                                m_chunkData.Remove(coord);
                            }
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
                            m_chunks[coord].SetDrawRange(Vector3Int.zero, Vector3Int.zero);
                            m_chunks[coord].gameObject.SetActive(false);
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
                    //    print("Set: "+ chunkCoord+ " " + drawRangeMin + " " + drawRangeMax + " " + m_rangeX.max + "  " + max);


                    if (!m_chunks.ContainsKey(chunkCoord))
                    {
                        VoxelChunk chunk = Instantiate(m_chunkPrefab).Init(new Vector3Int(x, y, z), VoxelManager.chunkSize, this);
                        chunk.transform.position = new Vector3(x, y, z) * VoxelManager.chunkSize;
                        m_chunks[chunkCoord] = chunk;
                    }

                    m_chunks[chunkCoord].gameObject.SetActive(true);
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

    private Voxel[] RequestChuckData(Vector3Int chunk_coord, bool updateMesh)
    {
        int chunkSize = VoxelManager.chunkSize;
        Voxel[] v = new Voxel[chunkSize * chunkSize * chunkSize];

        string path = VoxelManager.VoxelDataDir + "/" + chunk_coord.ToString() + ".txt";
        if (!File.Exists(path))
        {
            m_dataChangeFlag.Add(chunk_coord);
        }
            //print("read:" + chunk_coord);
            VoxelManager.LoadData(chunk_coord, (data) =>
             {
                 m_chunkData[chunk_coord] = data;
                 if (m_chunks.ContainsKey(chunk_coord) && updateMesh)
                 {
                     BufferMeshGen(chunk_coord);
                     //print("Read Done: " + chunk_coord);
                 }
             });
        return v;
    }


    private Vector3Int GetChunk(Vector3Int coord)
    {
        int[] xyz = new int[] { coord.x, coord.y, coord.z };

        for (int i = 0; i < xyz.Length; i++)
        {
            xyz[i] = ((xyz[i] - step(xyz[i])) / VoxelManager.chunkSize) + step(xyz[i]);
        }
        return new Vector3Int(xyz[0], xyz[1], xyz[2]);
    }

    private int step(int num)
    {
        int step = Mathf.Sign(num) == -1 ? -1 : 0;
        return step;
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
                    m_dataChangeFlag.Add(chunkCoord);
                    VoxelManager.ExportData(m_chunkData[chunkCoord], chunkCoord,
                        () =>
                        {
                            m_exportingChunks.Remove(chunkCoord);
                            m_dataChangeFlag.Remove(chunkCoord);
                        });
                }
            }
        }
    }


    #region Mesh Generation

    private void BufferMeshGen(Vector3Int coord)
    {
        m_meshGenBuffer.Enqueue(coord);
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

            float voxelSize = _voxelSize[0];
            int size = _chunkSize[0];
            int startIndex = index * _chunkSize[0] * _chunkSize[0] * _chunkSize[0];
            Vector3Int chunkCoord = chunkCoords[index];
            for (int x = drawRangeMin[index].x; x <= drawRangeMax[index].x; x++)
            {
                for (int y = drawRangeMin[index].y; y <= drawRangeMax[index].y; y++)
                {
                    for (int z = drawRangeMin[index].z; z <= drawRangeMax[index].z; z++)
                    {
                        Vector3Int localCoord = new Vector3Int(x, y, z);
                        int localIndex = Coord2Index(localCoord, size);
                        //print(localCoord + "  " + localIndex);
                        Voxel voxel = voxelData[startIndex + localIndex];
                        if (voxel.render == 0)
                            continue;

                        //back-----------------------------------------------------
                        Voxel back;
                        if (localCoord.z == 0)
                        {
                            Vector3Int otherLocalCoord = new Vector3Int(localCoord.x, localCoord.y, size - 1);
                            int i = chunkCoords.IndexOf(chunkCoord + Vector3Int.back);
                            if (i!= -1)
                            {
                                int otherStartIndex = i * size * size * size;
                                back = voxelData[otherStartIndex + Coord2Index(otherLocalCoord, size)];                          
                            }
                            else
                                back = new Voxel();
                        }
                        else
                            back = voxelData[startIndex + Coord2Index(localCoord + Vector3Int.back, size)];
                        if (back.render == 0)
                        {
                            //print(chunkCoord + " " + localCoord);
                            NativeArray<Vector3> vertices = new NativeArray<Vector3>(4, Allocator.Temp);
                            vertices[0] = new Vector3(localCoord.x * voxelSize, localCoord.y * voxelSize, localCoord.z * voxelSize);
                            vertices[1] = new Vector3(localCoord.x * voxelSize, (localCoord.y + 1) * voxelSize, localCoord.z * voxelSize);
                            vertices[2] = new Vector3((localCoord.x + 1) * voxelSize, (localCoord.y + 1) * voxelSize, localCoord.z * voxelSize);
                            vertices[3] = new Vector3((localCoord.x + 1) * voxelSize, localCoord.y * voxelSize, localCoord.z * voxelSize);
                            DrawQuad(vertices, VoxelDirection.Back, voxel.color);
                            vertices.Dispose();
                        }


                        //forward---------------------------------------------------
                        Voxel forward;
                        if (localCoord.z == size - 1)
                        {
                            Vector3Int otherLocalCoord = new Vector3Int(localCoord.x, localCoord.y, 0);
                            int i = chunkCoords.IndexOf(chunkCoord + Vector3Int.forward);
                            if (i != -1)
                            {
                                int otherStartIndex = i * size * size * size;
                                forward = voxelData[otherStartIndex + Coord2Index(otherLocalCoord, size)];
                            }
                            else
                                forward = new Voxel();
                        }
                        else
                            forward = voxelData[startIndex + Coord2Index(localCoord + Vector3Int.forward, size)];
                        if (forward.render == 0)
                        {
                            NativeArray<Vector3> vertices = new NativeArray<Vector3>(4, Allocator.Temp);
                            vertices[0] = new Vector3((localCoord.x + 1) * voxelSize, localCoord.y * voxelSize, (localCoord.z + 1) * voxelSize);
                            vertices[1] = new Vector3((localCoord.x + 1) * voxelSize, (localCoord.y + 1) * voxelSize, (localCoord.z + 1) * voxelSize);
                            vertices[2] = new Vector3(localCoord.x * voxelSize, (localCoord.y + 1) * voxelSize, (localCoord.z + 1) * voxelSize);
                            vertices[3] = new Vector3(localCoord.x * voxelSize, localCoord.y * voxelSize, (localCoord.z + 1) * voxelSize);
                            DrawQuad(vertices, VoxelDirection.Forward, voxel.color);
                            vertices.Dispose();
                        }

                        //left------------------------------------------------------------
                        Voxel left;
                        if (localCoord.x == 0)
                        {
                            Vector3Int otherLocalCoord = new Vector3Int(size - 1, localCoord.y, localCoord.z);
                            int i = chunkCoords.IndexOf(chunkCoord + Vector3Int.left);
                            if (i != -1)
                            {
                                int otherStartIndex = i * size * size * size;
                                left = voxelData[otherStartIndex + Coord2Index(otherLocalCoord, size)];
                            }
                            else
                                left = new Voxel();
                        }
                        else
                            left = voxelData[startIndex + Coord2Index(localCoord + Vector3Int.left, size)];
                        if (left.render == 0)
                        {
                            NativeArray<Vector3> vertices = new NativeArray<Vector3>(4, Allocator.Temp);
                            vertices[0] = new Vector3(localCoord.x * voxelSize, localCoord.y * voxelSize, (localCoord.z + 1) * voxelSize);
                            vertices[1] = new Vector3(localCoord.x * voxelSize, (localCoord.y + 1) * voxelSize, (localCoord.z + 1) * voxelSize);
                            vertices[2] = new Vector3(localCoord.x * voxelSize, (localCoord.y + 1) * voxelSize, localCoord.z * voxelSize);
                            vertices[3] = new Vector3(localCoord.x * voxelSize, localCoord.y * voxelSize, localCoord.z * voxelSize);
                            DrawQuad(vertices, VoxelDirection.Left, voxel.color);
                            vertices.Dispose();
                        }

                        //right-------------------------------------------------------------
                        Voxel right;
                        if (localCoord.x == size - 1)
                        {
                            Vector3Int otherLocalCoord = new Vector3Int(0, localCoord.y, localCoord.z);
                            int i = chunkCoords.IndexOf(chunkCoord + Vector3Int.right);
                            if (i != -1)
                            {
                                int otherStartIndex = i * size * size * size;
                                right = voxelData[otherStartIndex + Coord2Index(otherLocalCoord, size)];
                            }
                            else
                                right = new Voxel();
                        }
                        else
                            right = voxelData[startIndex + Coord2Index(localCoord + Vector3Int.right, size)];
                        if (right.render == 0)
                        {
                            NativeArray<Vector3> vertices = new NativeArray<Vector3>(4, Allocator.Temp);
                            vertices[0] = new Vector3((localCoord.x + 1) * voxelSize, localCoord.y * voxelSize, localCoord.z * voxelSize);
                            vertices[1] = new Vector3((localCoord.x + 1) * voxelSize, (localCoord.y + 1) * voxelSize, localCoord.z * voxelSize);
                            vertices[2] = new Vector3((localCoord.x + 1) * voxelSize, (localCoord.y + 1) * voxelSize, (localCoord.z + 1) * voxelSize);
                            vertices[3] = new Vector3((localCoord.x + 1) * voxelSize, localCoord.y * voxelSize, (localCoord.z + 1) * voxelSize);
                            DrawQuad(vertices, VoxelDirection.Right, voxel.color);
                            vertices.Dispose();
                        }

                        //down---------------------------------------------------------------
                        Voxel down;
                        if (localCoord.y == 0)
                        {
                            Vector3Int otherLocalCoord = new Vector3Int(localCoord.x, size - 1, localCoord.z);
                            int i = chunkCoords.IndexOf(chunkCoord + Vector3Int.down);
                            if (i != -1)
                            {
                                int otherStartIndex = i * size * size * size;
                                down = voxelData[otherStartIndex + Coord2Index(otherLocalCoord, size)];
                            }
                            else
                                down = new Voxel();
                        }
                        else
                            down = voxelData[startIndex + Coord2Index(localCoord + Vector3Int.down, size)];
                        if (down.render == 0)
                        {
                            NativeArray<Vector3> vertices = new NativeArray<Vector3>(4, Allocator.Temp);
                            vertices[0] = new Vector3(localCoord.x * voxelSize, localCoord.y * voxelSize, (localCoord.z + 1) * voxelSize);
                            vertices[1] = new Vector3(localCoord.x * voxelSize, localCoord.y * voxelSize, localCoord.z * voxelSize);
                            vertices[2] = new Vector3((localCoord.x + 1) * voxelSize, localCoord.y * voxelSize, localCoord.z * voxelSize);
                            vertices[3] = new Vector3((localCoord.x + 1) * voxelSize, localCoord.y * voxelSize, (localCoord.z + 1) * voxelSize);
                            DrawQuad(vertices, VoxelDirection.Down, voxel.color);
                            vertices.Dispose();
                        }

                        //up--------------------------------------------------------------
                        Voxel up;
                        if (localCoord.y == size - 1)
                        {
                            Vector3Int otherLocalCoord = new Vector3Int(localCoord.x, 0, localCoord.z);
                            int i = chunkCoords.IndexOf(chunkCoord + Vector3Int.up);
                            if (i != -1)
                            {
                                int otherStartIndex = i * size * size * size;
                                up = voxelData[otherStartIndex + Coord2Index(otherLocalCoord, size)];
                            }
                            else
                                up = new Voxel();
                        }
                        else
                            up = voxelData[startIndex + Coord2Index(localCoord + Vector3Int.up, size)];
                        if (up.render == 0)
                        {
                            NativeArray<Vector3> vertices = new NativeArray<Vector3>(4, Allocator.Temp);
                            vertices[0] = new Vector3(localCoord.x * voxelSize, (localCoord.y + 1) * voxelSize, localCoord.z * voxelSize);
                            vertices[1] = new Vector3(localCoord.x * voxelSize, (localCoord.y + 1) * voxelSize, (localCoord.z + 1) * voxelSize);
                            vertices[2] = new Vector3((localCoord.x + 1) * voxelSize, (localCoord.y + 1) * voxelSize, (localCoord.z + 1) * voxelSize);
                            vertices[3] = new Vector3((localCoord.x + 1) * voxelSize, (localCoord.y + 1) * voxelSize, localCoord.z * voxelSize);
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
