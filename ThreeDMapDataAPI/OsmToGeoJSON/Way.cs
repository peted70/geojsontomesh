using System.Collections.Generic;
using System.Diagnostics;

namespace OsmToGeoJSON
{
    [DebuggerDisplay("Type = {Type} Id = {Id}, Nodes = {Nodes}")]
    public class Way : Element
    {
        public Way()
        {
            Nodes = new List<string>();
            ResolvedNodes = new Dictionary<string, Node>();
        }
        public Coordinates Center { get; set; }

        public List<Coordinates> Geometry { get; set; }
        public Bounds Bounds { get; set; }
        public override string Type { get { return "way"; } }
        public List<string> Nodes { get; set; }
        public Dictionary<string,Node> ResolvedNodes { get; set; }

        public string RoleInRelation { get; set; }
        public bool IsBoundsPlaceHolder { get; set; }

        public bool IsAnInnerWithoutAnOuter { get; set; }
        public bool IsMultipolygonOutline { get; set; }
    }
}