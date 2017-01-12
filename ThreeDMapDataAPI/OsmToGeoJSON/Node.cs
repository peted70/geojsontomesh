using System.Diagnostics;

namespace OsmToGeoJSON
{
    [DebuggerDisplay("Type = {Type} Id = {Id}, Lat = {Lat} Lon = {Lon}")]
    public class Node : Element
    {
        protected bool Equals(Node other)
        {
            return Lat.Equals(other.Lat) && Lon.Equals(other.Lon);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Node) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Lat.GetHashCode()*397) ^ Lon.GetHashCode();
            }
        }

        public double? Lat { get; set; }
        public double? Lon { get; set; }

        public bool IsBounds { get; set; }

        public bool IsAGeometryNode { get; set; }

        public bool IsCentrePlaceHolder { get; set; }
        public override string Type { get { return "node"; } }

        public bool RequiresFeatureRendering { get; set; }

    }
}