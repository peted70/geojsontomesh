namespace OsmToGeoJSON.Dto
{
    public class NodeDto : ElementDto
    {
        public double? Lat { get; set; }
        public double? Lon { get; set; }
        public override string Type { get { return "node"; } }

        new public NodeDto Clone()
        {
            return (NodeDto)CloneImpl();
        }

        protected virtual ElementDto CloneImpl()
        {
            var copy = (NodeDto)base.CloneImplementation();
            copy.Lat = Lat;

            return copy;
        }
    }
}