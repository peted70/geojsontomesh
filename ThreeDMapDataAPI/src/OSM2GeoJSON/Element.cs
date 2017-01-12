using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace OsmToGeoJSON
{
    [DebuggerDisplay("Type = {Type} Id = {Id}")]
    public abstract class Element
    {
        protected Element()
        {
            RelationProperties = new Dictionary<string, Dictionary<string, object>>();
            Tags = new Dictionary<string, object>();
        }

        public abstract string Type { get; }
        public string Id { get; set; }

        public int? Version { get; set; }
        public DateTime? TimeStamp { get; set; }
        public int? ChangeSet { get; set; }
        public string User { get; set; }
        public int? Uid { get; set; }

        public bool IsGeometryIncomplete { get; set; }
        public bool HasInterestingTags { get; set; }

        public bool IsReferencedByARelation { get; set; }
        public Element Parent { get; set; }
        public bool HasParent { get { return Parent != null; }} 
        public Dictionary<string,Dictionary<string, object>> RelationProperties { get; set; }
        public Dictionary<string, object> Tags { get; set; }
        
    }
}