using System.Collections.Generic;
using GeoJSON.Net.Feature;

namespace OsmToGeoJSON.Renderer
{
    public interface IWaysRenderer
    {
        List<Feature> Render(List<Way> nodes);
    }
}