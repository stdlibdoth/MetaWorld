using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;
using System;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VoxelChunk : MonoBehaviour
{
    private ColliderSpawner m_colliderSpawner;

    private Vector3Int m_coord;
    private int m_chunkSize;
    private MeshGenerator m_meshGenerator;
    private Mesh m_mesh;
    private MeshFilter m_meshFilter;

    private List<Vector3> m_verts;
    private List<Color> m_colors;
    private List<int> m_tris;
    private List<Vector3> m_normals;
    private List<BoxCollider> m_colliders;
    private Voxel[] m_voxels;
    private int m_quadCount;
    private bool m_drawFlag;
    //private VoxelFormatter m_voxelFormatter;
    //private int m_drawingState;
    //private Task m_drawingTask;
    //private CancellationTokenSource m_tokenSource;

    private Vector3Int m_drawRangeMin;
    private Vector3Int m_drawRangeMax;

    public Vector3Int drawRangeMin { get { return m_drawRangeMin; } }
    public Vector3Int drawRangeMax { get { return m_drawRangeMax; } }

    private void Awake()
    {
        m_mesh = new Mesh();
        m_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m_meshFilter = GetComponent<MeshFilter>();
        m_meshFilter.mesh = m_mesh;
        m_tris = new List<int>();
        m_verts = new List<Vector3>();
        m_colors = new List<Color>();
        m_normals = new List<Vector3>();
        m_colliders = new List<BoxCollider>();
        m_drawFlag = false;
        //m_voxelFormatter = new VoxelFormatter();
        //m_drawingTask = new Task(() => { });
        //m_drawingTask.Start();
        //m_drawingState = 0;
    }

    private void Start()
    {
    }

    private void LateUpdate()
    {
        if (m_drawFlag)
            Draw();
        //if (m_drawingState == 0 && m_drawFlag
        //    && (m_drawingTask.Status == TaskStatus.RanToCompletion || m_drawingTask.Status == TaskStatus.Canceled))
        //{
        //    m_mesh.Clear();
        //    m_tokenSource = new CancellationTokenSource();
        //    m_drawingTask = new Task(() => Draw(m_drawRangeMin, m_drawRangeMax), m_tokenSource.Token);
        //    m_drawingTask.Start();
        //    m_drawingState = 1;
        //}
        //else if (m_drawingState == 2 && m_drawFlag && m_drawingTask.Status == TaskStatus.RanToCompletion)
        //{
        //    m_mesh.SetVertices(m_verts);
        //    m_mesh.SetColors(m_colors);
        //    m_mesh.SetTriangles(m_tris, 0);
        //    m_mesh.SetNormals(m_normals);
        //    m_drawingState = 0;
        //    m_drawFlag = false;
        //    m_drawingTask.Dispose();
        //}
    }


    #region Public Methods

    public VoxelChunk Init(Vector3Int coord,int chunk_size, MeshGenerator mesh_gen)
    {
        m_meshGenerator = mesh_gen;
        m_chunkSize = chunk_size;
        m_coord = coord;
        m_colliderSpawner = mesh_gen.ColliderSpawner;
        m_voxels = new Voxel[chunk_size * chunk_size * chunk_size];
        name = coord.ToString();
        return this;
    }

    public void SetVoxelData(Voxel[] voxel_data)
    {
        //for (int i = 0; i < m_voxels.Length; i++)
        //{
        //    m_voxels[i] = voxel_data[i];
        //}
        m_voxels = voxel_data;
        int l = m_chunkSize * m_chunkSize * m_chunkSize;
        if (m_voxels.Length != l)
            m_voxels = new Voxel[l];
    }

    public void SetDraw()
    {
        m_drawFlag = true;
        //m_drawingState = 0;
        //if (m_drawingTask.Status == TaskStatus.Running && m_tokenSource!= null)
        //{
        //    m_tokenSource.Cancel();
        //    //m_tokenSource.Dispose();
        //    //m_drawingTask.Dispose();
        //}
        //if (m_drawingState == 0)
        //{
        //    m_mesh.Clear();
        //    m_tokenSource = new CancellationTokenSource();
        //    m_drawingTask = new Task(() => Draw(m_drawRangeMin, m_drawRangeMax), m_tokenSource.Token);
        //    m_drawingTask.Start();
        //}
    }


    public void SetDrawRange(Vector3Int min, Vector3Int max)
    {
        m_drawRangeMin = min;
        m_drawRangeMax = max;
    }

    //private void ExportData(Action onExport)
    //{
    //    m_voxelFormatter.Export(m_voxels, m_coord, VoxelManager.VoxelDataDir, onExport);
    //}

    //public void LoadData(Action<Voxel[]> onLoad)
    //{
    //    m_voxelFormatter.ReadData(VoxelManager.VoxelDataDir, m_coord, m_chunkSize* m_chunkSize* m_chunkSize, onLoad);
    //}

    #endregion

    private void Draw()
    {
        BoxCollider[] boxColliders = m_colliders.ToArray();
        for (int i = 0; i < boxColliders.Length; i++)
        {
            m_colliderSpawner.Release(boxColliders[i]);
        }
        m_colliders.Clear();

        m_mesh.Clear();
        m_verts.Clear();
        m_colors.Clear();
        m_tris.Clear();
        m_normals.Clear();
        m_quadCount = 0;

        //print(m_coord + "   " + m_drawRangeMin + "   " + m_drawRangeMax);

        for (int x = m_drawRangeMin.x; x <= m_drawRangeMax.x ; x++)
        {
            for (int y = m_drawRangeMin.y; y <= m_drawRangeMax.y ; y++)
            {
                for (int z = m_drawRangeMin.z; z <= m_drawRangeMax.z ; z++)
                {
                    int coordIndex = Coord2Index(new Vector3Int(x, y, z), m_chunkSize);
                    if (m_voxels[coordIndex].render == 1)
                    {
                        UpdateVoxel(new Vector3Int(x, y, z));
                    }
                }
            }
        }
        m_mesh.SetVertices(m_verts);
        m_mesh.SetColors(m_colors);
        m_mesh.SetTriangles(m_tris, 0);
        m_mesh.SetNormals(m_normals);
        m_drawFlag = false;
    }


    private void UpdateVoxel(Vector3Int coord)
    {
        int chunkSize = m_chunkSize;
        float voxelSize = VoxelManager.voxelSize;
        Voxel voxel = m_voxels[Coord2Index(coord, chunkSize)];

        //BoxCollider collider = m_colliderSpawner.Get();
        //collider.transform.position = (coord + new Vector3(0.5f, 0.5f, 0.5f)) * voxelSize + m_coord * chunkSize;
        //collider.size = new Vector3(voxelSize, voxelSize, voxelSize);
        //m_colliders.Add(collider);

        //back-----------------------------------------------------
        Voxel back;
        if (coord.z == 0)
        {
            Vector3Int vCoord = new Vector3Int(coord.x, coord.y, chunkSize - 1);
            back = m_meshGenerator.GetVoxelData(m_coord + Vector3Int.back)[Coord2Index(vCoord, chunkSize)];
        }
        else
            back = m_voxels[Coord2Index(coord + Vector3Int.back, chunkSize)];
        if (back.render == 0)
        {
            Vector3[] verts = new Vector3[]
            {
                new Vector3(coord.x*voxelSize,coord.y*voxelSize,coord.z*voxelSize),
                new Vector3(coord.x*voxelSize,(coord.y+1)*voxelSize,coord.z*voxelSize),
                new Vector3((coord.x + 1)*voxelSize,(coord.y+1)*voxelSize,coord.z *voxelSize),
                new Vector3((coord.x + 1)*voxelSize,coord.y*voxelSize,coord.z*voxelSize),
            };
            DrawQuad(verts, VoxelDirection.Back, voxel.color);
        }


        //forward---------------------------------------------------
        Voxel forward;
        if (coord.z == chunkSize - 1)
        {
            Vector3Int vCoord = new Vector3Int(coord.x, coord.y, 0);
            forward = m_meshGenerator.GetVoxelData(m_coord + Vector3Int.forward)[Coord2Index(vCoord, chunkSize)];
        }
        else
            forward = m_voxels[Coord2Index(coord + Vector3Int.forward, chunkSize)];
        if (forward.render == 0)
        {
            Vector3[] verts = new Vector3[]
            {
                new Vector3((coord.x+1)*voxelSize,coord.y*voxelSize,(coord.z +1)*voxelSize),
                new Vector3((coord.x + 1)*voxelSize,(coord.y+1)*voxelSize,(coord.z +1)*voxelSize),
                new Vector3(coord.x *voxelSize,(coord.y+1)*voxelSize,(coord.z +1) *voxelSize),
                new Vector3(coord.x *voxelSize,coord.y*voxelSize,(coord.z +1)*voxelSize),
            };
            DrawQuad(verts, VoxelDirection.Forward, voxel.color);
        }

        //left------------------------------------------------------------
        Voxel left;
        if (coord.x == 0)
        {
            Vector3Int vCoord = new Vector3Int(chunkSize - 1, coord.y, coord.z);
            left = m_meshGenerator.GetVoxelData(m_coord + Vector3Int.left)[Coord2Index(vCoord, chunkSize)];
        }
        else
            left = m_voxels[Coord2Index(coord + Vector3Int.left, chunkSize)];
        if (left.render == 0)
        {
            Vector3[] verts = new Vector3[]
            {
                new Vector3(coord.x*voxelSize,coord.y*voxelSize,(coord.z +1)*voxelSize),
                new Vector3(coord.x*voxelSize,(coord.y+1)*voxelSize,(coord.z +1)*voxelSize),
                new Vector3(coord.x*voxelSize,(coord.y+1)*voxelSize,coord.z *voxelSize),
                new Vector3(coord.x *voxelSize,coord.y*voxelSize,coord.z *voxelSize),
            };
            DrawQuad(verts, VoxelDirection.Left, voxel.color);
        }

        //right-------------------------------------------------------------
        Voxel right;
        if (coord.x == chunkSize - 1)
        {
            Vector3Int vCoord = new Vector3Int(0, coord.y, coord.z);
            right = m_meshGenerator.GetVoxelData(m_coord + Vector3Int.right)[Coord2Index(vCoord, chunkSize)];
        }
        else
            right = m_voxels[Coord2Index(coord + Vector3Int.right, chunkSize)];
        if (right.render == 0)
        {
            Vector3[] verts = new Vector3[]
            {
                new Vector3((coord.x + 1)*voxelSize,coord.y*voxelSize,coord.z*voxelSize),
                new Vector3((coord.x + 1)*voxelSize,(coord.y+1)*voxelSize,coord.z *voxelSize),
                new Vector3((coord.x + 1)*voxelSize,(coord.y+1)*voxelSize,(coord.z + 1) *voxelSize),
                new Vector3((coord.x + 1) *voxelSize,coord.y*voxelSize,(coord.z +1) *voxelSize),
            };
            DrawQuad(verts, VoxelDirection.Right, voxel.color);
        }

        //down---------------------------------------------------------------
        Voxel down;
        if (coord.y == 0)
        {
            Vector3Int vCoord = new Vector3Int(coord.x, chunkSize - 1, coord.z);
            down = m_meshGenerator.GetVoxelData(m_coord + Vector3Int.down)[Coord2Index(vCoord, chunkSize)];
        }
        else
            down = m_voxels[Coord2Index(coord + Vector3Int.down, chunkSize)];
        if (down.render == 0)
        {
            Vector3[] verts = new Vector3[]
            {
                new Vector3(coord.x*voxelSize,coord.y*voxelSize,(coord.z +1)*voxelSize),
                new Vector3(coord.x *voxelSize,coord.y*voxelSize,coord.z*voxelSize),
                new Vector3((coord.x + 1)*voxelSize,coord.y*voxelSize,coord.z *voxelSize),
                new Vector3((coord.x + 1) *voxelSize,coord.y*voxelSize,(coord.z+1) *voxelSize),
            };
            DrawQuad(verts, VoxelDirection.Down, voxel.color);
        }

        //up--------------------------------------------------------------
        Voxel up;
        if (coord.y == chunkSize - 1)
        {
            Vector3Int vCoord = new Vector3Int(coord.x, 0, coord.z);
            up = m_meshGenerator.GetVoxelData(m_coord + Vector3Int.up)[Coord2Index(vCoord, chunkSize)];
        }
        else
            up = m_voxels[Coord2Index(coord + Vector3Int.up, chunkSize)];
        if (up.render == 0)
        {
            Vector3[] verts = new Vector3[]
            {
                new Vector3(coord.x*voxelSize,(coord.y + 1)*voxelSize,coord.z*voxelSize),
                new Vector3(coord.x*voxelSize,(coord.y+1)*voxelSize,(coord.z +1)*voxelSize),
                new Vector3((coord.x + 1)*voxelSize,(coord.y+1)*voxelSize,(coord.z+1) *voxelSize),
                new Vector3((coord.x + 1) *voxelSize,(coord.y + 1)*voxelSize,coord.z *voxelSize),
            };
            DrawQuad(verts, VoxelDirection.Up, voxel.color);
        }
    }


    private void DrawQuad(Vector3[] verts, VoxelDirection dir, Color color)
    {
        int i = m_quadCount * 4;
        int[] topo = new int[] { i, i + 1, i + 3, i + 3, i + 1, i + 2 };
        Vector3 normal = dir.ToVector3();
        m_verts.AddRange(verts);
        m_colors.AddRange(new Color[] { color, color, color, color});
        m_normals.AddRange(new Vector3[] { normal, normal, normal, normal });
        m_tris.AddRange(topo);
        m_quadCount++;
    }


    private Vector3 Index2Coord(int vert_index, int dimension)
    {
        int z = vert_index / (dimension * dimension);
        int y = (vert_index % (dimension * dimension)) / dimension;
        int x = (vert_index % (dimension * dimension)) % dimension;
        return new Vector3(x, y, z);
    }

    private int Coord2Index(Vector3Int coord, int dimension)
    {
        return coord.z * (dimension * dimension) + coord.y * dimension + coord.x;
    }

}
