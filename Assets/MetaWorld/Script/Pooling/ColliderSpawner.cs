using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ColliderSpawner : MonoBehaviour
{
    [SerializeField] private BoxCollider m_colliderPrefab;
    [SerializeField] private string m_pooledLayer;
    private ObjectPool<BoxCollider> m_colliderPool;

    private int m_layer;

    public ColliderSpawner Init(string original_layer)
    {
        m_colliderPool = new ObjectPool<BoxCollider>(CreatePooledItem, OnGetFromPool, OnRelease, OnDestroyPoolObject, false, 1000, 80000);
        m_layer = LayerMask.NameToLayer(original_layer);
        return this;
    }

    public BoxCollider Get()
    {
       return m_colliderPool.Get();
    }

    public void Release(BoxCollider collider)
    {
        m_colliderPool.Release(collider);
    }

    private BoxCollider CreatePooledItem()
    {
        BoxCollider boxCollider = Instantiate(m_colliderPrefab);
        boxCollider.gameObject.layer = m_layer;
        return boxCollider;
    }

    void OnRelease(BoxCollider collider)
    {
        collider.gameObject.layer = LayerMask.NameToLayer(m_pooledLayer);
    }

    void OnGetFromPool(BoxCollider collider)
    {
        collider.gameObject.layer = m_layer;
    }

    void OnDestroyPoolObject(BoxCollider collider)
    {
        Destroy(collider.gameObject);
    }
}
