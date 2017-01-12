using System.Collections.Generic;
using System.Linq;

namespace OsmToGeoJSON.Processors
{
    class WaysProcessor : IWaysProcessor
    {
        private readonly ITagClassifier _tagClassifier;

        public WaysProcessor() : this(new TagClassifier())
        {
            
        }
        public WaysProcessor(ITagClassifier tagClassifier)
        {
            _tagClassifier = tagClassifier;
        }

        public WaysProcessingResult Process(List<Way> ways, List<Node> nodes)
        {
           var pseudoNodes = CreatePseudoNodesAndWays(ways);
           var geometryNodes = CorrectWayGeometry(ways);
           var newNodes = pseudoNodes.Concat(geometryNodes).ToList();
            var nodesRepo = nodes.Concat(newNodes).ToList();
            ResolveNodesForWays(ways, nodesRepo);
            SetInterestingTags(ways);

            return new WaysProcessingResult { PseudoNodes = newNodes, Ways = ways };
        }

        private void ResolveNodesForWays(List<Way> ways, List<Node> nodesRepo)
        {
            foreach (var way in ways)
            {
                if (way.Nodes == null) continue;
                foreach (var nodeId in way.Nodes)
                {
                    var node = nodesRepo.FirstOrDefault(x => x.Id == nodeId);
                    if (way.Geometry != null)
                    {
                        int n;
                        bool isNumeric = int.TryParse(nodeId, out n);
                        if (isNumeric)
                        {
                            var geometryCoordinates = way.Geometry.FirstOrDefault(g => g.Index == n -1);
                            if (geometryCoordinates != null)
                            {
                                node = nodesRepo.First(x => x.Lat == geometryCoordinates.Lat
                                    && x.Lon == geometryCoordinates.Lon);
                            }
                        }
                    }
                    
                    if (node == null)
                    {
                        way.IsGeometryIncomplete = true;
                        if (way.HasParent) way.Parent.IsGeometryIncomplete = true;
                    }
                    
                    if (node != null)
                    {
                        node.Parent = way;
                        if (!way.ResolvedNodes.ContainsKey(nodeId)) way.ResolvedNodes.Add(nodeId, node);
                    }
                   
                }
                
            }
        }

        private void SetInterestingTags(IEnumerable<Way> ways)
        {
            foreach (var way in ways)
            {
                if (way.Tags != null && _tagClassifier.AreInteresting(way.Tags, way.Parent != null ? way.Parent.Tags : new Dictionary<string, object>()))
                    way.HasInterestingTags = true;
            }
        }



        private List<Node> CorrectWayGeometry(List<Way> ways)
        {
            var newNodes = new List<Node>();
            foreach (var way in ways)
            {
                if (way.Nodes == null)
                {
                    if (way.Geometry != null)
                    {
                        way.Nodes =
                        way.Geometry.Select(g => string.Format("_anonymous@{0}/{1}", g.Lat, g.Lon)).ToList();
                    }
                }
                if (way.Geometry != null)
                {
                    foreach (var coordinate in way.Geometry)
                    {
                        var geometryNode = new Node
                        {
                            Id = string.Format("_anonymous@{0}/{1}", coordinate.Lat, coordinate.Lon),
                            Lat = coordinate.Lat,
                            Lon = coordinate.Lon,
                            IsAGeometryNode = true
                            
                        };
                        newNodes.Push(geometryNode);
                    }
                }
            }
            return newNodes;
        }

        private List<Node> CreatePseudoNodesAndWays(List<Way> ways)
        {
            var pseudoNodes = new List<Node>();
            var newWays = new List<Way>();
            foreach (var way in ways)
            {
                if (way.Center != null)
                {
                    var pseudoNode = CreatePsudeoNode(way);
                    if (way.Nodes == null) way.Nodes = new List<string>();
                    way.Nodes.Add(pseudoNode.Id);
                    pseudoNodes.Add(pseudoNode);
                    way.ResolvedNodes.Add(pseudoNode.Id, pseudoNode);
                }
                if (way.Bounds != null && way.Geometry == null)
                {
                    way.IsBoundsPlaceHolder = true;
                    AddBoundsAsPsuedoNodes(way, pseudoNodes);
                }
            }
            ways.AddRange(newWays);
            return pseudoNodes;
        }

        private static void AddBoundsAsPsuedoNodes(Way newWay, List<Node> nodeRepository)
        {
            nodeRepository.Add(CreateBoundsPseudoNode("node", newWay, newWay.Bounds.MinLat, newWay.Bounds.MinLon, 1));
            nodeRepository.Add(CreateBoundsPseudoNode("node", newWay, newWay.Bounds.MaxLat, newWay.Bounds.MinLon, 2));
            nodeRepository.Add(CreateBoundsPseudoNode("node", newWay, newWay.Bounds.MaxLat, newWay.Bounds.MaxLon, 3));
            nodeRepository.Add(CreateBoundsPseudoNode("node", newWay, newWay.Bounds.MinLat, newWay.Bounds.MaxLon, 4));
        }

        private static Node CreateBoundsPseudoNode(string type, Way way, double lat, double lon, int i)
        {
            var pseudoNode = new Node { Id = string.Format("_{0}/{1}bounds{2}", type, way.Id, i), Lat = lat, Lon = lon, Parent = way, IsBounds = true};
            if (way.Nodes == null) way.Nodes = new List<string>();
            way.Nodes.Add(pseudoNode.Id);
            return pseudoNode;
        }

        private static Node CreatePsudeoNode(Way way)
        {
            return new Node { Id = way.Id, IsCentrePlaceHolder = true, Lat = way.Center.Lat, Lon = way.Center.Lon, Tags = way.Tags, Version = way.Version, Parent = way};
        }
    }
}