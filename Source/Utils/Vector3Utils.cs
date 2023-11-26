using UnityEngine;

namespace StarshipExpansionProject.Utils
{
    public static class Vector3Utils
    {
        public static Vector3 ToNewContext(this Vector3 direction, Transform oldSpace, Transform newSpace)
        {
            return newSpace.InverseTransformDirection(oldSpace.InverseTransformDirection(direction));
        }

    }
}
