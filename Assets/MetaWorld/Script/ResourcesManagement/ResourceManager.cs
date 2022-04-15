using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    [SerializeField] private PrefabTable m_prefabTable;


    private Dictionary<string, GameObject> m_prefabMap;


    private static ResourceManager m_singleton = null;

    private void Awake()
    {
        if (m_singleton != null)
            DestroyImmediate(this);
        else
        {
            m_singleton = this;
            m_prefabMap = new Dictionary<string, GameObject>();
            for (int i = 0; i < m_prefabTable.prefabEntries.Length; i++)
            {
                m_prefabMap.Add(m_prefabTable.prefabEntries[i].name, m_prefabTable.prefabEntries[i].prefab);
            }
        }
    }
}
