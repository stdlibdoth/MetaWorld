using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface IMeshControl
{
    public void SetCenter(Vector3 center, bool continuous_mode);
    public Vector3Int GetCoordRangeMin();
    public Vector3Int GetCoordRangeMax();
    public void SetRenderExtent(float extent);
    public VoxelCoordinate GetVoxelCoordinate(Vector3Int world_coord);
    public Vector3Int GetWorldCoordinate(Vector3 world_pos);

    public Vector3Int GetChunkCoordinate(Vector3Int world_coord);
    public Voxel GetVoxelData(Vector3Int world_coord);
    public Voxel[] GetRangedVoxelData(Vector3Int min, Vector3Int max);
    public void SetVoxelData(Vector3Int world_coord, Voxel data);
    public Voxel[] GetChunkData(Vector3Int chunk_coord, bool copy_data);
    public void SetChunkData(Vector3Int chunk_coord, Voxel[] data);
    public void UpdateChunkMeshes(Vector3Int min, Vector3Int max, bool load_data = false, bool check_range = false);
    public void LoadMeshData(Vector3Int min, Vector3Int max, bool update_mesh = false, bool show_mesh = false, bool check_range = false);
    public void UpdateChunkMesh(Vector3Int chunk, bool load_data = false, bool check_range = false);
    public void LoadMeshData(Vector3Int chunk, bool update_mesh = false, bool show_mesh = false, bool check_range = false);
    public void SetUpdateMode(bool continuous_update);
    public void ClearChunk(Vector3Int chunk, bool clear_mesh = true, bool clear_data = true);
    public void ClearAllChunk(bool clear_mesh = true, bool clear_data = true);
}
