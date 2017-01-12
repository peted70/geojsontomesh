using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OsmToGeoJSON.Dto
{
    public static class Extensions
    {
        public static Node ToDomain(this NodeDto node)
        {
            var newNode = new Node
            {
                Id = node.Id.ToString(CultureInfo.InvariantCulture),
                Lat = node.Lat,
                Lon = node.Lon,
                Tags =
                    node.Tags == null
                        ? new Dictionary<string, object>()
                        : node.Tags = new Dictionary<string, object>(node.Tags)
            };
            MapElementProperties(newNode, node);
            return newNode;
        }

        private static void MapElementProperties(Element newElement, ElementDto element)
        {
            newElement.TimeStamp = element.TimeStamp;
            newElement.ChangeSet = element.ChangeSet;
            newElement.User = element.User;
            newElement.Uid = element.Uid;
            newElement.Version = element.Version;
        }

        public static Way ToDomain(this WayDto way)
        {
            var newWay = new Way
            {
                Id = way.Id.ToString(CultureInfo.InvariantCulture),
                Bounds = way.Bounds == null ? null : way.Bounds.ToDomain(),
                Geometry = ConvertGeometryToDomain(way),
                Tags = way.Tags == null ? new Dictionary<string, object>() : new Dictionary<string, object>(way.Tags),
                Version = way.Version,
                Center = way.Center == null ? null : way.Center.ToDomain(0),
                Nodes =
                    way.Nodes == null ? null : way.Nodes.Select(n => n.ToString(CultureInfo.InvariantCulture)).ToList()
            };
            MapElementProperties(newWay, way);
            return newWay;
        }

       

        public static Relation ToDomain(this RelationDto relation)
        {
            var newRelation = new Relation
            {
                Id = relation.Id.ToString(CultureInfo.InvariantCulture),
                Bounds = relation.Bounds == null ? null : relation.Bounds.ToDomain(),
                Tags = relation.Tags == null ? new Dictionary<string, object>() : new Dictionary<string, object>(relation.Tags),
                Version = relation.Version,
                Center = relation.Center == null ? null : relation.Center.ToDomain(0),
                Members =
                    relation.Members == null
                        ? new List<RelationMember>()
                        : relation.Members.Select(m => m.ToDomain()).ToList()
            };
            MapElementProperties(newRelation, relation);
            return newRelation;
        }

        public static RelationMember ToDomain(this RelationMemberDto relationMember)
        {
            return new RelationMember
            {
                Role = relationMember.Role,
                Type = relationMember.Type,
                Ref = relationMember.Ref.ToString(CultureInfo.InvariantCulture),
                Geometry = 
                    ConvertGeometryToDomain(relationMember),
                Lat = relationMember.Lat,
                Lon = relationMember.Lon
            };
        }

        private static List<Coordinates> ConvertGeometryToDomain(RelationMemberDto relationMember)
        {
            var coordinates = new List<Coordinates>();
            if (relationMember.Geometry == null) return null;
            int indexCounter = 0;
            foreach (var coordinatesDto in relationMember.Geometry)
            {
                if (coordinatesDto == null) continue;
                coordinates.Add(ToDomain(coordinatesDto, indexCounter));
                indexCounter++;
            }
            return coordinates;
        }

        private static List<Coordinates> ConvertGeometryToDomain(WayDto way)
        {
            var coordinates = new List<Coordinates>();
            if (way.Geometry == null) return null;
            int indexCounter = 0;
            foreach (var coordinatesDto in way.Geometry)
            {
                if (coordinatesDto == null) continue;
                coordinates.Add(ToDomain(coordinatesDto, indexCounter));
                indexCounter++;
            }
            return coordinates;
        }

        public static Bounds ToDomain(this BoundsDto bounds)
        {
            return new Bounds
            {
                MaxLat = bounds.MaxLat,
                MaxLon = bounds.MaxLon,
                MinLat = bounds.MinLat,
                MinLon = bounds.MinLon
            };
        }

        public static Coordinates ToDomain(this CoordinatesDto coordinates, int index)
        {
            return new Coordinates { Index  = index, Lat = coordinates.Lat, Lon = coordinates.Lon };
        }
    }
}