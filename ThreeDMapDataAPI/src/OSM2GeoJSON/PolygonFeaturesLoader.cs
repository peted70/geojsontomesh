using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using OsmToGeoJSON.Util;

namespace OsmToGeoJSON
{
    public class PolygonFeaturesLoader : IPolygonFeaturesLoader
    {
        public Dictionary<string, object> Load()
        {
            return JsonConvert.DeserializeObject<Dictionary<string, object>>
                (File.ReadAllText(Converter.FilesRoot + "polygonFeatures.json"), new JsonConverter[] { new PolygonFeatureDictionaryConverter() });
        }
    }
}