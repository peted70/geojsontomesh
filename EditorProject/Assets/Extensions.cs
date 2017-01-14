using UnityEngine;

namespace Assets.Helpers
{
    public static class Extensions
    {
        public static Vector2 ToVector2xz(this Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }

        public static Vector3 ToVector3xz(this Vector2 v)
        {
            return new Vector3(v.x, 0, v.y);
        }

        public static Vector2[] ToPolygonFromBounds(this Bounds bounds)
        {
            var ret = new Vector2[4];
            var min = bounds.min.ToVector2xz();
            var max = bounds.max.ToVector2xz();

            ret[0] = min;
            ret[1] = new Vector2(min.x, max.y);
            ret[2] = max;
            ret[3] = new Vector2(max.x, min.y);
            return ret;
        }
    }
}
