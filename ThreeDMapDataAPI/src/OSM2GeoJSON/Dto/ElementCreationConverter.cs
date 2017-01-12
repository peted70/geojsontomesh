using System;
using Newtonsoft.Json.Linq;

namespace OsmToGeoJSON.Dto
{
    public class ElementCreationConverter : JsonCreationConverter<ElementDto>
    {
        protected override ElementDto Create(Type objectType, JObject jsonObject)
        {
            string typeName = (jsonObject["type"]).ToString();
            switch (typeName)
            {
                case "node":
                    return new NodeDto();
                case "way":
                    return new WayDto();
                case "relation":
                    return new RelationDto();
                case "area":
                    return new AreaDto();
                default:
                    return null;
            }
        }
    }
}