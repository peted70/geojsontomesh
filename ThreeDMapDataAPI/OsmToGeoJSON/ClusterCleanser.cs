using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace OsmToGeoJSON
{
    public class ClusterCleanser : IClusterCleanser
    {
        public void SanitzeClusters(IEnumerable<Cluster> clusters)
        {
            foreach (var cluster in clusters)
            {
                var sanitizedRings = new List<Ring>();
                foreach (var ring in cluster)
                {
                    if (ring.Count < 4)
                    {
                        Debug.WriteLine("Multipolygon contains a ring with less than four nodes");
                        continue;
                    }
                    if (!Equals(ring.First(), ring.Last()))
                    {
                        Debug.WriteLine("Multipolygon contains a ring that is not closed");
                        continue;
                    }
                    sanitizedRings.Add(ring);
                }
                cluster.Clear();
                sanitizedRings.ForEach(cluster.Add);
                if (cluster.Count == 0)
                    Debug.WriteLine("Multipolygon contains an empty ring cluster");
            }
        }
    }
}