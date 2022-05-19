using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(IMeshControl))]
public class VoxelCommandModel : MonoBehaviour
{
    private IMeshControl m_meshControl;

    private Voxel[] m_voxelCopyBuffer;
    private Vector3Int m_voxelBufferSize;

    private void Awake()
    {
        m_meshControl = GetComponent<IMeshControl>();
    }

    public void ClearRectArea(Vector3Int min, Vector3Int max)
    {
        Vector3Int chunkMin = m_meshControl.GetChunkCoordinate(min);
        Vector3Int chunkMax = m_meshControl.GetChunkCoordinate(max);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                for (int z = min.z; z <= max.z; z++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    m_meshControl.SetVoxelData(pos, new Voxel { render = 0 });
                }
            }
        }
        m_meshControl.UpdateChunkMeshes(chunkMin, chunkMax);
    }

    public void SetRectArea(Vector3Int min, Vector3Int max, Color color)
    {
        Vector3Int chunkMin = m_meshControl.GetChunkCoordinate(min);
        Vector3Int chunkMax = m_meshControl.GetChunkCoordinate(max);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                for (int z = min.z; z <= max.z; z++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    m_meshControl.SetVoxelData(pos, new Voxel { render = 1, color = color });
                }
            }
        }
        m_meshControl.UpdateChunkMeshes(chunkMin, chunkMax);
    }

    public void CopyVoxelData(Vector3Int min_world, Vector3Int max_world)
    {
        m_voxelCopyBuffer = m_meshControl.GetRangedVoxelData(min_world, max_world);
        m_voxelBufferSize = max_world - min_world + Vector3Int.one;
    }

    public void PasteVoxelData(Vector3Int min_world)
    {
        if (m_voxelBufferSize == Vector3Int.zero)
            return;

        for (int x = 0; x < m_voxelBufferSize.x; x++)
        {
            for (int y = 0; y < m_voxelBufferSize.y; y++)
            {
                for (int z = 0; z < m_voxelBufferSize.z; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z);
                    m_meshControl.SetVoxelData(min_world + coord,
                        m_voxelCopyBuffer[coord.CoordToIndex(m_voxelBufferSize)]);
                }
            }
        }
    }

    public void SetVoxel(Vector3Int world_coord, Color color)
    {
        m_meshControl.SetVoxelData(world_coord, new Voxel { color = color, render = 1 });
        Vector3Int chunk = m_meshControl.GetChunkCoordinate(world_coord);
        m_meshControl.UpdateChunkMesh(chunk);
    }

    public void SetVoxelCheckEqual(Vector3Int world_coord, Color color)
    {
        if (m_meshControl.GetVoxelData(world_coord) != new Voxel { color = color, render = 1 })
        {
            m_meshControl.SetVoxelData(world_coord, new Voxel { color = color, render = 1 });
            Vector3Int chunk = m_meshControl.GetChunkCoordinate(world_coord);
            m_meshControl.UpdateChunkMesh(chunk);
        }
    }

    public void ClearVoxel(Vector3Int world_coord)
    {
        if (m_meshControl.GetVoxelData(world_coord).render == 0)
            return;
        m_meshControl.SetVoxelData(world_coord, new Voxel { render = 0 });
        Vector3Int chunk = m_meshControl.GetChunkCoordinate(world_coord);
        m_meshControl.UpdateChunkMesh(chunk);
    }

    public Vector3Int GetWorldCoord(Vector3 world_pos)
    {
        return m_meshControl.GetWorldCoordinate(world_pos);
    }

    public Voxel GetVoxelData(Vector3Int world_coord)
    {
        return m_meshControl.GetVoxelData(world_coord);
    }

    public void RefreshMesh(Vector3Int min, Vector3Int max)
    {
        m_meshControl.ClearAllChunk();
        Vector3Int chunkMin = m_meshControl.GetChunkCoordinate(min);
        Vector3Int chunkMax = m_meshControl.GetChunkCoordinate(max);
        m_meshControl.LoadMeshData(chunkMin, chunkMax, true, true, true);
    }
}
