using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Voxel
{
    public int userId;
    public int render;
    public Color color;
    public static bool operator ==(Voxel c1, Voxel c2)
    {
        return c1.userId == c2.userId
            && c1.render == c2.render
            && c1.color == c2.color;
    }

    public static bool operator !=(Voxel c1, Voxel c2)
    {
        return c1.userId != c2.userId
            || c1.render != c2.render
            || c1.color != c2.color;
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }
}


public struct VoxelCoordinate
{
    public Vector3Int chunkCoord;
    public Vector3Int localCoord;
}

