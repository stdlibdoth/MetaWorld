using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class Extensions
{
    public readonly static Vector3[] VoxelDir2V3 = new Vector3[]
    {
        Vector3.back,
        Vector3.forward,
        Vector3.left,
        Vector3.right,
        Vector3.down,
        Vector3.up,
    };

    //private static int SignToOne

    public static Vector3 ToVector3(this VoxelDirection dir)
    {
        return VoxelDir2V3[(int)dir];
    }

    //public static VoxelDirection ToVoxelDirection(this Vector3 v)
    //{
    //    int index = v.z + (v.x*2 + )+(v.y*)
    //}

    public static VoxelDirection Increment(this VoxelDirection dir, int steps)
    {
        int i = Mathf.Abs(((int)dir + VoxelDirections.directionCount + steps) % VoxelDirections.directionCount);
        return (VoxelDirection)i;
    }

    public static int CoordToIndex(this Vector3Int coord, Vector3Int dimension)
    {
        return coord.z * (dimension.x * dimension.y) + coord.y * dimension.x + coord.x;
    }
}


public static class Utility
{
    public static bool TryParseIntxInt(string str, out Vector2Int v2_int)
    {
        v2_int = Vector2Int.zero;
        string[] s = str.Split('x');
        if(s.Length==2)
        {
            bool xb = int.TryParse(s[0], out int x);
            bool yb = int.TryParse(s[1], out int y);
            v2_int = new Vector2Int(x, y);
            return xb && yb;
        }
        return false;
    }

    public static bool TryParseColor(string str,out Color color)
    {
        string[] c = str.Split(',');
        color = Color.clear;

        if(float.TryParse(c[0].Substring(1),out float r)&&
        float.TryParse(c[1],out float g)&&
        float.TryParse(c[2],out float b)&&
        float.TryParse(c[3].Remove(c[3].Length - 1),out float a))
        {
            color = new Color(r, g, b, a);
            return true;
        }
        return false;
    }
}
