using System.Collections.Generic;

namespace OsmToGeoJSON
{
    public interface IFeaturePropertyBuilder
    {
        Dictionary<string, object> GetProperties(Element elementDto);
    }
}