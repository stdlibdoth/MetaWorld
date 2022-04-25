using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine.Rendering;
using Unity.Collections;

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
    //private Mesh.MeshDataArray meshDataArray;


    private List<BoxCollider> m_colliders;
    private Voxel[] m_voxels;

    private Vector3Int m_drawRangeMin;
    private Vector3Int m_drawRangeMax;

    public Vector3Int drawRangeMin { get { return m_drawRangeMin; } }
    public Vector3Int drawRangeMax { get { return m_drawRangeMax; } }

    public MeshFilter meshFilter { get { return m_meshFilter; } }


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
        m_voxels = voxel_data;
        int l = m_chunkSize * m_chunkSize * m_chunkSize;
        if (m_voxels.Length != l)
            m_voxels = new Voxel[l];
    }


    public void SetDrawRange(Vector3Int min, Vector3Int max)
    {
        m_drawRangeMin = min;
        m_drawRangeMax = max;
    }

    #endregion
}
