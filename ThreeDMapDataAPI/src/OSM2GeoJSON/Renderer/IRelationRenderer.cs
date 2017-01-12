using System.Collections.Generic;
using GeoJSON.Net.Feature;

namespace OsmToGeoJSON.Renderer
{
    public interface IRelationRenderer
    {
        List<Feature> Render(List<Relation> relations);
    }
}