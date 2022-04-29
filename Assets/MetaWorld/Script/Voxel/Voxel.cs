using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Voxel
{
    public int userId;
    public int render;
    public Color color;
}


public struct VoxelCoordinate
{
    public Vector3Int chunkCoord;
    public Vector3Int localCoord;
}