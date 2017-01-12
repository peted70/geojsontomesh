using System.Diagnostics;

namespace OsmToGeoJSON
{
    [DebuggerDisplay("Type = {Type} Id = {Id}")]
    public class Area : Element
    {
        public override string Type
        {
            get { return "area"; }
        }
    }
}