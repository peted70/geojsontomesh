using System.Collections.Generic;

namespace OsmToGeoJSON
{
    public interface IPolygonFeaturesLoader
    {
        Dictionary<string, object> Load();
    }
}