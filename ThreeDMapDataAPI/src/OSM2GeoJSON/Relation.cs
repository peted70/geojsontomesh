using System.Collections.Generic;
using System.Diagnostics;

namespace OsmToGeoJSON
{
    [DebuggerDisplay("Type = {Type} Id = {Id}, Members = {Members.Count}")]
    public class Relation : Element
    {
        public Relation()
        {
            ResolvedNodes = new Dictionary<string, Node>();
            ResolvedWays = new Dictionary<string, Way>();
            ResolvedChildRelations =new Dictionary<string, Relation>();
            Members = new List<RelationMember>();
        }

        public Coordinates Center { get; set; }
        public Bounds Bounds { get; set; }

        public override string Type
        {
            get { return "relation"; }
        }

        public List<RelationMember> Members { get; set; }


        public bool IsMultiPolygon
        {
            get
            {
                return Tags != null &&
                       Tags.ContainsKey("type") &&
                       (Tags["type"].ToString() == "multipolygon" ||
                        Tags["type"].ToString() == "boundary");
            }
        }
        public bool IsSimpleMultiPolygon { get; set; }

        public bool HasBeenRendered { get; set; }
        public Dictionary<string, Node> ResolvedNodes { get; set; }

        public Dictionary<string, Way> ResolvedWays { get; set; }

        public Dictionary<string, Relation> ResolvedChildRelations { get; set; }

        public string RoleInParentRelation { get; set; }
    }
}