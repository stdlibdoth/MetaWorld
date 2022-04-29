using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class VoxelManager : MonoBehaviour
{
    [SerializeField] private MeshGenerator m_meshGenPrefab;

    [Header("Voxel")]
    [SerializeField] private float m_voxelSize;
    [SerializeField] private int m_chunkSize;

    [Header("Formatter")]
    [SerializeField] private int m_writeInterval;
    [SerializeField] private int m_writeBatchSize;
    [SerializeField] private int m_readInterval;


    private static VoxelManager m_singleton = null;

    public static float voxelSize { get { return m_singleton.m_voxelSize; } }
    public static int chunkSize { get { return m_singleton.m_chunkSize; } }

    public static int dataLength { get { return m_singleton.m_chunkSize * m_singleton.m_chunkSize * m_singleton.m_chunkSize; } }

    public static string VoxelDataDir { get { return m_singleton.m_voxelDataDir; } }

    public static bool isReadingData { get { return m_singleton.m_voxelDataFormatter.ReadingTaskCount != 0; } }
    public static bool isExportingData { get { return m_singleton.m_voxelDataFormatter.WritingTaskCount != 0; } }

    private string m_voxelDataDir;
    private MeshGenerator m_meshGen;
    private VoxelDataFormatter m_voxelDataFormatter;

    private void Awake()
    {
        if (m_singleton != null)
        {
            DestroyImmediate(this);
        }
        else
        {
            m_singleton = this;
            Init();
        }
    }


    private void Init()
    {
        m_voxelDataDir = GameManager.RootDirectory + "/" + GameManager.GlobalSettings.voxelDataDirectory;
        if (!Directory.Exists(m_voxelDataDir))
            Directory.CreateDirectory(m_voxelDataDir);
        m_voxelDataFormatter = new VoxelDataFormatter(m_writeInterval,m_readInterval,m_writeBatchSize);
        m_meshGen = Instantiate(m_meshGenPrefab);
    }


    public static void ExportData(Voxel[] data, Vector3Int chunk_coord, Action onExportData)
    {
        m_singleton.m_voxelDataFormatter.Export(data, chunk_coord, onExportData);
    }

    public static void LoadData(Vector3Int chunk_coord, Action<Voxel[]> onReadData)
    {
        m_singleton.m_voxelDataFormatter.ReadData(m_singleton.m_voxelDataDir, chunk_coord, chunkSize * chunkSize * chunkSize, onReadData);
    }


    private void OnDestroy()
    {
        m_voxelDataFormatter.Dispose();
    }
}
