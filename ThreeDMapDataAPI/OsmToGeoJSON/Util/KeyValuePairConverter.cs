using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OsmToGeoJSON.Util
{
    public class KeyValuePairConverterCustom : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var listArrary = value as List<Dictionary<string, object>>;

                writer.WriteStartArray();
                if (listArrary != null)
                {
                    foreach (var dictionary in listArrary)
                    {
                        if (dictionary == null || dictionary.Count == 0) continue;
                        writer.WriteStartObject();
                        foreach (var item in dictionary)
                        {
                            writer.WritePropertyName(item.Key);

                            if (item.Value is JObject)
                            {
                                writer.WriteStartObject();

                                foreach (var prop in ((JObject) item.Value).Properties())
                                {
                                    writer.WritePropertyName(prop.Name);
                                    writer.WriteValue(prop.Value);
                                }
                                writer.WriteEndObject();


                            }
                            else
                            {
                                writer.WriteValue(item.Value);
                            }


                        }
                        writer.WriteEndObject();
                    }
                }
                writer.WriteEndArray();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return null;
            }

            public override bool CanConvert(Type objectType)
            {
                return true;
            }
        } 
    
}