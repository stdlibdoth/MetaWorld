using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GlobalSettingsData
{
    public string voxelDataDirectory;
    public string settingsDirectory;
    public int serializationChunkSize;


    public GlobalSettingsData(GlobalSettingsData data)
    {
        voxelDataDirectory = data.voxelDataDirectory;
        settingsDirectory = data.settingsDirectory;
        serializationChunkSize = data.serializationChunkSize;
    }
}
