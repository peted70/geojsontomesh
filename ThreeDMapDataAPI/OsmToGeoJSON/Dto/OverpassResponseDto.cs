using System.Collections.Generic;
using Newtonsoft.Json;

namespace OsmToGeoJSON.Dto
{
    public class OverpassResponseDto
    {
        public string Version { get; set; }
        public string Generator { get; set; }
        [JsonProperty(TypeNameHandling = TypeNameHandling.Objects)]
        public List<ElementDto> Elements { get; set; }
    }
}