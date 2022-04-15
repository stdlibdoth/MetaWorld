using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownCam
{
    [System.Serializable]
    public struct MinMax
    {
        public float min;
        public float max;

        public float Difference
        {
            get
            {
                return max - min;
            }
        }

        public MinMax(float min, float max)
        {
            this.min = min <= max ? min : max;
            this.max = max > min ? max : min;
        }

        public MinMax(MinMax min_max)
        {
            min = min_max.min;
            max = min_max.max;
        }

    }
}
