using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownCam
{
    public static class Extention
    {
        public static Vector2 XY(this Vector3 v3)
        {
            return new Vector2(v3.x, v3.y);
        }

        public static Vector2 XZ(this Vector3 v3)
        {
            return new Vector2(v3.x, v3.z);
        }

        public static Vector3 SetX(this Vector3 v3, float x)
        {
            return new Vector3(x, v3.y, v3.z);
        }

        public static Vector3 SetY(this Vector3 v3, float y)
        {
            return new Vector3(v3.x, y, v3.z);
        }

        public static Vector3 SetZ(this Vector3 v3, float z)
        {
            return new Vector3(v3.x, v3.y, z);
        }
    }
}