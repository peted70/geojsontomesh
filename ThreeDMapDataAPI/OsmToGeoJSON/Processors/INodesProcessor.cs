using System.Collections.Generic;

namespace OsmToGeoJSON.Processors
{
    public interface INodesProcessor
    {
        NodesProcessingResult BuildIndex(List<Node> nodes, List<Way> ways, List<Relation> relations);
    }
}