using System.Collections.Generic;

namespace OsmToGeoJSON.Processors
{
    public interface IWaysProcessor
    {
        WaysProcessingResult Process(List<Way> ways, List<Node> nodes);
    }
}