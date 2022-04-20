using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using DG.Tweening;

public class MeshGenerator : MonoBehaviour
{
    [SerializeField] private VoxelChunk m_chunkPrefab;
    [SerializeField] private ColliderSpawner m_colliderSpawner;

    [SerializeField] private float renderExtent;
    [SerializeField] private string m_voxelLayer;
    [SerializeField] private float m_updateDist;

    private Dictionary<Vector3Int, Voxel[]> m_chunkData;
    private Dictionary<Vector3Int, VoxelChunk> m_chunks;
    private HashSet<Vector3Int> m_exportingChunks;
    private HashSet<Vector3Int> m_dataChangeFlag;
    //private HashSet<Vector3Int> m_updatingChunks;
    private bool initFlag;
    private Vector3 m_prevDrawPos;


    private MinMaxInt m_rangeX;
    private MinMaxInt m_rangeY;
    private MinMaxInt m_rangeZ;

    private int m_voxelExtent;
    private Vector3 m_center;
    private Vector3Int m_centerCoord;

    public ColliderSpawner ColliderSpawner { get { return m_colliderSpawner; } }


    private void Awake()
    {
        m_chunkData = new Dictionary<Vector3Int, Voxel[]>();
        m_chunks = new Dictionary<Vector3Int, VoxelChunk>();
        m_exportingChunks = new HashSet<Vector3Int>();
        m_dataChangeFlag = new HashSet<Vector3Int>();
        m_center = transform.position;
        m_colliderSpawner.Init(m_voxelLayer);
        //m_updatingChunks = new HashSet<Vector3Int>();
    }


    private void Start()
    {
        UpdateVoxelRange();
        LoadInitialChunkData();
        //string path = "F:/Eifle.txt";
        //ReadVoxelData(path);
        UpdateChunkDrawRange();
    }


    private void Update()
    {
        if (Vector3.Distance(transform.position, m_prevDrawPos) > m_updateDist)
        {
            UpdateVoxelRange();
            UpdateChunkDrawRange();
            m_prevDrawPos = transform.position;
            //float x = transform.position.x + Time.deltaTime * 5;
            //transform.position = new Vector3(x, transform.position.y, transform.position.z);
        }
        if (m_exportingChunks.Count == 0 && !initFlag)
        {
            initFlag = true;
            DOTween.Sequence()
                .Append(transform.DOMoveX(80, 7))
                .Append(transform.DOMoveX(0, 7))
                .SetLoops(-1);
        }
    }

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

        for (int x = min.x - 1; x <= max.x + 1; x++)
        {
            for (int y = min.y - 1; y <= max.y + 1; y++)
            {
                for (int z = min.z - 1; z <= max.z + 1; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z);
                    m_chunkData[coord] = RequestChuckData(coord);
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

    private void UpdateChunkDrawRange()
    {
        Vector3Int min = GetChunk(new Vector3Int(m_rangeX.min, m_rangeY.min, m_rangeZ.min));
        Vector3Int max = GetChunk(new Vector3Int(m_rangeX.max, m_rangeY.max, m_rangeZ.max));

        int size = VoxelManager.chunkSize;
        int xN = size - Mathf.Abs(m_rangeX.min - (min.x + 1) * size);
        int xP = Mathf.Abs(m_rangeX.max - max.x * size);
        int yN = size - Mathf.Abs(m_rangeY.min - (min.y + 1) * size);
        int yP = Mathf.Abs(m_rangeY.max - max.y * size);
        int zN = size - Mathf.Abs(m_rangeZ.min - (min.z + 1) * size);
        int zP = Mathf.Abs(m_rangeZ.max - max.z  * size);

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
                        if(!m_chunkData.ContainsKey(coord))
                            m_chunkData[coord] = RequestChuckData(coord);
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
                        m_chunks[chunkCoord].SetVoxelData(m_chunkData[chunkCoord]);
                    }

                    m_chunks[chunkCoord].gameObject.SetActive(true);
                    if (m_chunks[chunkCoord].drawRangeMin != drawRangeMin
                        || m_chunks[chunkCoord].drawRangeMax != drawRangeMax)
                    {
                        m_chunks[chunkCoord].SetDrawRange(drawRangeMin, drawRangeMax);
                        m_chunks[chunkCoord].SetDraw();
                    }
                }
            }
        }
    }

    private Voxel[] RequestChuckData(Vector3Int chunk_coord)
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
                 if (m_chunks.ContainsKey(chunk_coord))
                 {
                     m_chunks[chunk_coord].SetVoxelData(data);
                     m_chunks[chunk_coord].SetDraw();
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

    private Voxel[] GenerateRandomVoxelChunk(Vector3Int min, Vector3Int max)
    {
        int size = max.x - min.x + 1;
        Voxel[] voxels = new Voxel[size * size * size];
        Color color = Random.ColorHSV();
        color = new Color(color.r, color.g, color.b, 1);
        for (int x = 0 ; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    int i = z * (size * size) + y * size + x;
                    voxels[i].render = Random.Range(0, 10) == 1 ? 1 : 0;
                    voxels[i].color = color;
                }
            }
        }
        return voxels;
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
}
