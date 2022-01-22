using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TundraExploration.Utils
{
    public static class Vector3Utils
    {
        public static Vector3 ToNewContext(this Vector3 direction, Transform oldSpace, Transform newSpace)
        {
            return newSpace.InverseTransformDirection(oldSpace.InverseTransformDirection(direction));
        }

    }
}
