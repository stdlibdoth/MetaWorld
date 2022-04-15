using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class VoxelManager : MonoBehaviour
{
    [SerializeField] private float m_voxelSize;
    [SerializeField] private int m_chunkSize;

    [SerializeField] private MeshGenerator m_meshGenPrefab;

    private static VoxelManager m_singleton = null;

    public static float voxelSize { get { return m_singleton.m_voxelSize; } }
    public static int chunkSize { get { return m_singleton.m_chunkSize; } }
    public static string VoxelDataDir { get { return m_singleton.m_voxelDataDir; } }

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


    private void OnGUI()
    {
        if (GUI.Button(new Rect(200, 200, 300, 100), "Click"))
            Debug.Log("Clicked the button with text");
    }

    private void Init()
    {
        m_voxelDataDir = GameManager.RootDirectory + "/" + GameManager.GlobalSettings.voxelDataDirectory;
        if (!Directory.Exists(m_voxelDataDir))
            Directory.CreateDirectory(m_voxelDataDir);
        m_voxelDataFormatter = new VoxelDataFormatter(2,1);
        m_meshGen = Instantiate(m_meshGenPrefab);
    }


    public static void ExportData(Voxel[] data, Vector3Int chunk_coord, Action onExportData)
    {
        m_singleton.m_voxelDataFormatter.Export(data, chunk_coord, m_singleton.m_voxelDataDir, onExportData);
    }

    public static void LoadData(Vector3Int chunk_coord, Action<Voxel[]> onReadData)
    {
        m_singleton.m_voxelDataFormatter.ReadData(m_singleton.m_voxelDataDir, chunk_coord, chunkSize * chunkSize * chunkSize, onReadData);
    }

}
