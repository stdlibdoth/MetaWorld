//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class PoolManager : MonoBehaviour
//{
//    private static PoolManager m_singleton;

//    [SerializeField] private ObjectPool m_poolPrefab;

//    private Dictionary<string, ObjectPool> m_objectPools;


//    private void Awake()
//    {
//        if (m_singleton != null)
//            DestroyImmediate(this);
//        else
//        {
//            m_singleton = this;
//            m_objectPools = new Dictionary<string, ObjectPool>();
//        }
//    }


//    public static ObjectPool GetPool(string prefab_name)
//    {
//        ObjectPool objPool;
//        if (m_singleton.m_objectPools.ContainsKey(prefab_name))
//            objPool = m_singleton.m_objectPools[prefab_name];
//        else
//        {
//            objPool = Instantiate(m_singleton.m_poolPrefab);
//            if (!objPool.Init(prefab_name))
//            {
//                Destroy(objPool);
//                return null;
//            }
//            m_singleton.m_objectPools.Add(prefab_name, objPool);
//        }
//        return objPool;
//    }
//}
