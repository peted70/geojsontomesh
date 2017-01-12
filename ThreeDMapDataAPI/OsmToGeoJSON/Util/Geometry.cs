using System.Collections.Generic;
using System.Linq;

namespace OsmToGeoJSON.Util
{
    public class Geometry
    {
        public static bool PolygonIntersectsPolygon(List<Node> outerNodes, List<Node> innerNodes)
        {
            return innerNodes.Any(node => PointInPolygon(node, outerNodes));
        }

        public static bool PointInPolygon(Node point, List<Node> polygon)
        {
            var x = point.Lat;
            var y = point.Lon;
            var inside = false;
            int k, j = polygon.Count - 1;
            for (k = 0; k < polygon.Count; k++)
            {
                var xk = polygon[k].Lat;
                var yk = polygon[k].Lon;
                var xj = polygon[j].Lat;
                var yj = polygon[j].Lon;
                var intersect = ((yk > y) != (yj > y)) &&
                  (x < (xj - xk) * (y - yk) / (yj - yk) + xk);
                if (intersect) inside = !inside;
                j = k;
            }
            return inside;
        }
    }
}