using System.Collections.Generic;

namespace OsmToGeoJSON.Processors
{
    public class RelationsProcessingResult
    {
        public List<Relation> Relations { get; set; }

        public List<Way> PseudoWays { get; set; }
        public List<Node> PseudoNodes { get; set; }

    }
}