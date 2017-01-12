using System.Collections.Generic;

namespace OsmToGeoJSON.Processors
{
    public class WaysProcessingResult
    {
        public List<Way> Ways { get; set; }
        
        public List<Node> PseudoNodes { get; set; } 
    }
}