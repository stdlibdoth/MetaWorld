using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class VoxelSpawner : MonoBehaviour
{
    [SerializeField] private VoxelChunk m_chunkPrefab;
    private ObjectPool<VoxelChunk> m_chunkPool;

    public VoxelSpawner Init()
    {
        m_chunkPool = new ObjectPool<VoxelChunk>(CreatePooledItem, OnGetFromPool, OnRelease, OnDestroyPoolObject, false, 1000, 80000);
        return this;
    }



    public VoxelChunk Get()
    {
        return m_chunkPool.Get();
    }

    public void Release(VoxelChunk chunk)
    {
        m_chunkPool.Release(chunk);
    }

    private VoxelChunk CreatePooledItem()
    {
        VoxelChunk boxCollider = Instantiate(m_chunkPrefab);
        return boxCollider;
    }

    void OnRelease(VoxelChunk chunk)
    {
        chunk.SetDrawRange(Vector3Int.zero, Vector3Int.zero);
        chunk.meshFilter.mesh.Clear();
        chunk.gameObject.SetActive(false);
    }

    void OnGetFromPool(VoxelChunk chunk)
    {
        chunk.gameObject.SetActive(true);
    }

    void OnDestroyPoolObject(VoxelChunk chunk)
    {
        Destroy(chunk.gameObject);
    }
}
