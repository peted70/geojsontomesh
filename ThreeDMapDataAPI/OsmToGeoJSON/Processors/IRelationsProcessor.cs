using System.Collections.Generic;

namespace OsmToGeoJSON.Processors
{
    public interface IRelationsProcessor
    {
        RelationsProcessingResult Process(List<Relation> relations, List<Way> ways, List<Node> nodes);
    }
}
