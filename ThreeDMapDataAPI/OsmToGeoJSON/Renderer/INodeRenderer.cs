using System.Collections.Generic;
using GeoJSON.Net.Feature;

namespace OsmToGeoJSON.Renderer
{
    public interface INodeRenderer
    {
        List<Feature> Render(List<Node> nodes);
    }
}