using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OsmToGeoJSON.Processors
{
    class RelationsProcessor : IRelationsProcessor
    {
        private readonly ITagClassifier _tagClassifier;

        public RelationsProcessor() : this(new TagClassifier())
        {
            
        }

        public RelationsProcessor(ITagClassifier tagClassifier)
        {
            _tagClassifier = tagClassifier;
        }

        public RelationsProcessingResult Process(List<Relation> relations, List<Way> ways, List<Node> nodes)
        {
            var psuedoNodes = new List<Node>();
            var psuedoWays = new List<Way>();
            foreach (var relation in relations)
            {
                var hasFullGeometry = (relation.Members != null && relation.Members.Any(m => (m.Type == "node" && m.Lat.HasValue) || (m.Type == "way" && m.Geometry != null)));
                if (hasFullGeometry) ProcessRelationFullGeometry(relation, psuedoNodes, psuedoWays, ways);
                if (relation.Center != null) psuedoNodes.Add(AddCentreGeometryAsPsudeoNode(relation));
                if (relation.Bounds != null && relation.Members.All(x => x.Geometry == null)) AddBoundsAsPsuedoWay(relation, psuedoNodes, psuedoWays);

                var wayrepo = ways.Concat(psuedoWays).ToList();
                var nodesRepo = nodes.Concat(psuedoNodes).ToList();

                ResolveNodeAndWayReferences(relation, relations, nodesRepo, wayrepo);
                SetGeometryProperties(relation);
                AddRelationPropertiesToMembers(relation);

            }


            return new RelationsProcessingResult { PseudoNodes = psuedoNodes, PseudoWays = psuedoWays, Relations = relations };
        }

        private void SetGeometryProperties(Relation relation)
        {
            var relationHasInterestingTags = _tagClassifier.AreInteresting(relation.Tags, new Dictionary<string, object> { { "type", "anything" } }, false);
            relation.HasInterestingTags = relationHasInterestingTags;
            var outerCount = relation.Members.Count(m => m.Role == "outer");
            relation.IsSimpleMultiPolygon = outerCount == 1 && !relationHasInterestingTags;
           
            foreach (var member in relation.Members)
            {
                if (member.Type == "way")
                {
                    if (!relation.ResolvedWays.ContainsKey(member.Ref))
                    {
                        relation.IsGeometryIncomplete = true;
                        continue;
                    }
                    var way = relation.ResolvedWays[member.Ref];
                    way.IsReferencedByARelation = true;
                    var memberTags = way.Tags;
                    if (member.Role == "outer" && !_tagClassifier.AreInteresting(memberTags, relation.Tags))
                        way.IsMultipolygonOutline = true;
                    if (member.Role == "inner" && !_tagClassifier.AreInteresting(memberTags, relation.Tags))
                        way.IsMultipolygonOutline = true;
                    if (member.Role == "inner" && relation.ResolvedWays.Values.All(x => x.RoleInRelation != "outer"))
                        way.IsAnInnerWithoutAnOuter = true;
                    if (relation.IsSimpleMultiPolygon) way.IsMultipolygonOutline = true;
                }
                if (member.Type == "node")// && !relation.IsMultiPolygon)
                {
                    // This relation won't be rendered as it's not tagged as a MultiPolygon, however thit node may be important
                    // so set this flag to force the node to be rendered
                    var node = relation.ResolvedNodes[member.Ref];
                    node.IsReferencedByARelation = true;

                }
                
            }

            if (relation.ResolvedWays.Values.Any(x => x.IsGeometryIncomplete)
                || relation.ResolvedNodes.Values.Any(x => x.IsGeometryIncomplete))
                relation.IsGeometryIncomplete = true;

        }

        private void AddRelationPropertiesToMembers(Relation relation)
        {
            foreach (var member in relation.Members)
            {
                Element element = null;
                switch (member.Type)
                {
                    case "node":
                        if (relation.ResolvedNodes.ContainsKey(member.Ref))
                            element = relation.ResolvedNodes[member.Ref];
                        else
                            relation.IsGeometryIncomplete = true;
                        break;
                    case "way":
                        if (relation.ResolvedWays.ContainsKey(member.Ref))
                        {
                            element = relation.ResolvedWays[member.Ref];
                            relation.ResolvedWays[member.Ref].RoleInRelation = member.Role;
                        }
                        else
                            relation.IsGeometryIncomplete = true;
                        break;
                    case "relation":
                        if (relation.ResolvedChildRelations.ContainsKey(member.Ref))
                        {
                            element = relation.ResolvedChildRelations[member.Ref];
                            relation.ResolvedChildRelations[member.Ref].RoleInParentRelation = member.Role;
                        }
                        else
                            relation.IsGeometryIncomplete = true;
                        break;
                    default:
                        throw new Exception("Didn't expect this");
                }

                if (element != null)
                {
                    var props = new Dictionary<string, object>();
                    props.Add("rel", Convert.ToInt32(relation.Id));
                    props.Add("role", member.Role);
                    props.Add("relTags", TagsToObject(relation.Tags));
                    element.RelationProperties.Add(relation.Id, props);
                }
            }
        }

        private object TagsToObject(Dictionary<string, object> tags)
        {
            var wr = new StringWriter();
            var jsonWriter = new JsonTextWriter(wr);
            jsonWriter.StringEscapeHandling = StringEscapeHandling.EscapeNonAscii;
            return JObject.FromObject(tags);
        }

        private void ResolveNodeAndWayReferences(Relation relation, List<Relation> relations, List<Node> nodesRepo, List<Way> wayrepo)
        {
            foreach (var member in relation.Members)
            {
                if (member.Type == "node")
                {
                    var node = nodesRepo.FirstOrDefault(n => n.Id == member.Ref);
                    if (node == null)
                    {
                        relation.IsGeometryIncomplete = true;
                    }
                    else
                    {
                        node.Parent = relation;
                        relation.ResolvedNodes.Add(member.Ref, node);
                    }
                }
                if (member.Type == "way")
                {
                    var wayToAssociate = wayrepo.FirstOrDefault(n => n.Id == member.Ref);
                    if (wayToAssociate == null)
                    {
                        relation.IsGeometryIncomplete = true;
                    }
                    else
                    {
                        wayToAssociate.Parent = relation;
                        relation.ResolvedWays.Add(member.Ref, wayToAssociate);
                    }
                }
                if (member.Type == "relation")
                {
                    var relationToAssociate = relations.FirstOrDefault(n => n.Id == member.Ref);
                    if (relationToAssociate == null)
                    {
                        relation.IsGeometryIncomplete = true;
                    }
                    else
                    {
                        relationToAssociate.Parent = relation;
                        relation.ResolvedChildRelations.Add(member.Ref, relationToAssociate);
                    }
                }
            }
        }

        private static Node AddCentreGeometryAsPsudeoNode(Relation relation)
        {
            return new Node { Id = relation.Id, IsCentrePlaceHolder = true, Lat = relation.Center.Lat, Lon = relation.Center.Lon, Tags = relation.Tags, Version = relation.Version };
        }

        private static void ProcessRelationFullGeometry(Relation newRelation, List<Node> nodes, List<Way> psuedoWays, List<Way> ways)
        {
            int count = 0;
            foreach (var member in newRelation.Members)
            {
                if (member.Geometry != null) count += member.Geometry.Count;
                
                if (member.Type == "node")
                {
                    if (member.Lat.HasValue) nodes.Add(new Node { Id = member.Ref.ToString(), Lat = member.Lat.Value, Lon = member.Lon.Value, Parent = newRelation});
                }
                else if (member.Type == "way")
                {
                    if (member.Geometry != null) AddFullGeometryWay(newRelation, member, psuedoWays, ways, nodes);
                }
            }
            Debug.WriteLine(string.Format("Total Geometry: {0}", count));
        }

        private static void AddFullGeometryWay(Relation relation, RelationMember member, List<Way> psuedoWays, List<Way> ways, List<Node> nodes)
        {
            // shared multipolygon ways cannot be defined multiple times with the same id
            if (ways.Any(w => w.Id == member.Ref)) return;
            var geometryWay = new Way { Id = member.Ref, Nodes = new List<string>(), Parent = relation };
            foreach (var coordinate in member.Geometry)
            {
                var geometryPseudoNode = new Node { Id = string.Format("_anonymous@{0}/{1}", coordinate.Lat, coordinate.Lon), Lat = coordinate.Lat, Lon = coordinate.Lon, Parent = geometryWay};
                nodes.Add(geometryPseudoNode);
                geometryWay.Nodes.Add(geometryPseudoNode.Id);
                
            }
            psuedoWays.Add(geometryWay);
        }

        private static void AddBoundsAsPsuedoWay(Relation newRelation, List<Node> nodes, List<Way> ways)
        {
            var pseudoWay = new Way();
            pseudoWay.Id = "boundsway";
            newRelation.Members.Add(new RelationMember { Ref = "boundsway", Type = "way", Role = "outer"});
            newRelation.Tags["type"] = "boundary";
            pseudoWay.Nodes = new List<string>();
            pseudoWay.Parent = newRelation;
            nodes.Add(CreateBoundsPseudoNode("node", pseudoWay, newRelation.Bounds.MinLat, newRelation.Bounds.MinLon, 1));
            nodes.Add(CreateBoundsPseudoNode("node", pseudoWay, newRelation.Bounds.MaxLat, newRelation.Bounds.MinLon, 2));
            nodes.Add(CreateBoundsPseudoNode("node", pseudoWay, newRelation.Bounds.MaxLat, newRelation.Bounds.MaxLon, 3));
            nodes.Add(CreateBoundsPseudoNode("node", pseudoWay, newRelation.Bounds.MinLat, newRelation.Bounds.MaxLon, 4));
            pseudoWay.Nodes.Add(pseudoWay.Nodes[0]);
            pseudoWay.IsBoundsPlaceHolder = true;
            ways.Add(pseudoWay);
        }

        private static Node CreateBoundsPseudoNode(string type, Way pseudoWay, double lat, double lon, int i)
        {
            var pseudoNode = new Node { Id = string.Format("_{0}/{1}bounds{2}", type, pseudoWay.Id, i), Lat = lat, Lon = lon, Parent = pseudoWay };
            pseudoWay.Nodes.Add(pseudoNode.Id);
            return pseudoNode;
        }
    }
}