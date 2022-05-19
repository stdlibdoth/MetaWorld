using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GlobalSettings m_globalSettings;
    [SerializeField] private DefaultSettings m_defaultSettings;


    [Header("Modules")]
    [SerializeField] private Transform m_managersHolder;
    [SerializeField] private VoxelManager m_voxelManagerPrefab;
    [SerializeField] private ResourceManager m_resourceManagerPrefab;
    [SerializeField] private InputManager m_inputManagerPrefab;

    [SerializeField] private EditingCamController m_editingCamControllerPrefab;


    private static GameManager m_singleton;

    private GameSettings m_gameSettings;

    private VoxelManager m_voxelManager;
    private ResourceManager m_resourceManager;
    private EditingCamController m_editingCamController;

    public static GlobalSettingsData GlobalSettings
    {
        get
        {
            return new GlobalSettingsData(m_singleton.m_globalSettings.globalSettingsData);
        }
    } 

    public static string RootDirectory
    {
        get { return Application.persistentDataPath; }
    }


    private void Awake()
    {
        if (m_singleton != null)
        {
            DestroyImmediate(this);
        }
        else
        {
            m_singleton = this;
            InitSettings();
        }
    }

    private void Start()
    {
        InitModules();
    }

    private void InitSettings()
    {
        m_gameSettings = m_defaultSettings.defaultGameSettings;
        GameSettings defaultSettings = m_defaultSettings.defaultGameSettings;

        string settingsDir = Application.persistentDataPath + "/" + m_globalSettings.globalSettingsData.settingsDirectory;
        if (!Directory.Exists(settingsDir))
            Directory.CreateDirectory(settingsDir);

        string settingsPath = settingsDir + "/GameSettings.ini";
        if (!File.Exists(settingsPath))
            File.Create(settingsPath).Close();

        print(settingsPath);
        using (StreamReader streamReader = new StreamReader(settingsPath))
        {
            Dictionary<string, string> settingsVal = new Dictionary<string, string>();
            while (!streamReader.EndOfStream)
            {
                string[] line = streamReader.ReadLine().Split("=");
                settingsVal[line[0]] = line[1];
            }

            if (!settingsVal.ContainsKey("targetFPS") || !int.TryParse(settingsVal["targetFPS"], out m_gameSettings.targetFPS))
                m_gameSettings.targetFPS = defaultSettings.targetFPS;
            if (!settingsVal.ContainsKey("vSync") || !int.TryParse(settingsVal["vSync"], out m_gameSettings.vSync))
                m_gameSettings.vSync = defaultSettings.vSync;
            if (!settingsVal.ContainsKey("resolution") || !Utility.TryParseIntxInt(settingsVal["resolution"], out m_gameSettings.resolution))
                m_gameSettings.resolution = defaultSettings.resolution;
        }

        QualitySettings.vSyncCount = m_gameSettings.vSync;
        //Application.targetFrameRate = m_gameSettings.targetFPS;
        Application.targetFrameRate = -1;
    }



    private void InitModules()
    {
        m_voxelManager = Instantiate(m_voxelManagerPrefab, m_managersHolder);
        m_resourceManager = Instantiate(m_resourceManagerPrefab, m_managersHolder);
        //m_editingCamController = Instantiate(m_editingCamControllerPrefab);
    }
}
