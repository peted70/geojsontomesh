using System.Collections.Generic;

namespace OsmToGeoJSON
{
    public interface IClusterCleanser
    {
        void SanitzeClusters(IEnumerable<Cluster> clusters);
    }
}