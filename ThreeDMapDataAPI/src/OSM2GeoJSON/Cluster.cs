using System.Collections.Generic;
using System.Linq;

namespace OsmToGeoJSON
{
    public class Cluster : List<Ring>
    {
        public Cluster(IEnumerable<Ring> rings)
        {
            rings.ToList().ForEach(Add);
        }
    }
}