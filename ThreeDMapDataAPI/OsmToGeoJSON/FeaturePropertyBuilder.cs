using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using OsmToGeoJSON.Util;

namespace OsmToGeoJSON
{
    public class FeaturePropertyBuilder : IFeaturePropertyBuilder
    {
        public Dictionary<string, object> GetProperties(Element element)
        {
            var props = new Dictionary<string, object>();
            props.Add("type", element.Type);
            props.Add("id", element.Id);
            props.Add("tags", JsonConvert.SerializeObject(new Dictionary<string, object>(element.Tags.OrderBy(x => x.Key).ToDictionary(x => x.Key, x=> x.Value))));
            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Converters = new JsonConverter[] { new KeyValuePairConverterCustom() } };
            var relations = GetRelations(element);
            props.Add("relations", JsonConvert.SerializeObject(relations , settings));
            props.Add("meta", JsonConvert.SerializeObject(BuildMeta(element)));
            if (element.IsGeometryIncomplete) props.Add("tainted", true);
            if ((element is Way && ((Way)element).Bounds != null) ||
                ((element is Relation && ((Relation)element).Bounds != null)) || element.Id == "boundsway")
                props.Add("geometry", "bounds");
            if (element is Way && ((Way)element).Center != null) props.Add("geometry", "center");
            return props;
        }

        private List<Dictionary<string, object>> GetRelations(Element element)
        {
            if (element.RelationProperties.Count > 0)
            {
                return element.RelationProperties.Values.ToList();
            }
            return new List<Dictionary<string, object>>();

        }

        private static Dictionary<string, object> BuildMeta(Element elementDto)
        {
            var meta = new Dictionary<string, object>();
            if (elementDto.TimeStamp != DateTime.MinValue && elementDto.TimeStamp.HasValue) meta.Add("timestamp", elementDto.TimeStamp);
            if (elementDto.Version.HasValue) meta.Add("version", elementDto.Version);
            if (elementDto.ChangeSet.HasValue) meta.Add("changeset", elementDto.ChangeSet);
            if (!string.IsNullOrEmpty(elementDto.User)) meta.Add("user", elementDto.User);
            if (elementDto.Uid.HasValue) meta.Add("uid", elementDto.Uid);
            return meta.OrderBy(x => x.Key).ToDictionary(x => x.Key, x=> x.Value);
        }
    }
}