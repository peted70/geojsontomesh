using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OsmToGeoJSON.Dto
{
    [JsonConverter(typeof(ElementCreationConverter))]
    public abstract class ElementDto : ICloneable
    {
        public abstract string Type { get; }
        public long Id { get; set; }

        public int? Version { get; set; }

        public DateTime? TimeStamp { get; set; }

        public int? ChangeSet { get; set; }
        public string User { get; set; }
        public int? Uid { get; set; }

        public Dictionary<string, object> Tags { get; set; }

        public object Clone()
        {
            return CloneImplementation();

        }
        protected virtual ElementDto CloneImplementation()
        {
            var copy = (ElementDto)this.MemberwiseClone();
            if (Tags != null) copy.Tags = new Dictionary<string, object>(Tags);

            return copy;
        }
    }
}