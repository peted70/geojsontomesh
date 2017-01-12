using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace OsmToGeoJSON.Processors
{
    public class NodesProcessor : INodesProcessor
    {
        private readonly ITagClassifier _tagClassifier;

        public NodesProcessor() : this(new TagClassifier())
        {
            
        }

        public NodesProcessor(ITagClassifier tagClassifier)
        {
            _tagClassifier = tagClassifier;
        }

        public NodesProcessingResult BuildIndex(List<Node> nodes, List<Way> ways, List<Relation> relations)
        {
            AddPsuedoNodesFromWays(ways, nodes);
            VerifyNodesFromRelationExist(relations, nodes);
            FilterNodes(nodes);
            SetInterestingTags(nodes);

            return new NodesProcessingResult { Nodes = nodes.ToList() };
        }

        private void VerifyNodesFromRelationExist(List<Relation> relations, List<Node> nodes)
        {
            foreach (var relation in relations)
            {
                if (relation.Members != null)
                {
                    foreach (var member in relation.Members)
                    {
                        if (member.Type == "node" && nodes.All(n => n.Id != member.Ref)) throw new Exception("Node reference in Relation can't be found"); 
                    }
                }
                else
                {
                    Debug.WriteLine("Relation{0}/{1} ignored because it has no members", relation.Type, relation.Id);
                }
            }
        }

        private void SetInterestingTags(IEnumerable<Node> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.Tags != null && _tagClassifier.AreInteresting(node.Tags))
                    node.HasInterestingTags = true;
            }
        }

        private void FilterNodes(List<Node> nodes)
        {
            var includedNodes = new Dictionary<string, Node>();
            foreach (var node in nodes)
            {
                if (node.Lat.HasValue && !includedNodes.ContainsKey(node.Id))
                    includedNodes.Add(node.Id, node);
                else
                {
                    Debug.WriteLine("Node{0}/{1} ignored because it has no coordinates", node.Type, node.Id);
                }
            }
            nodes.Clear();
            includedNodes.Values.ToList().ForEach(nodes.Add);
        }

        private void AddPsuedoNodesFromWays(IEnumerable<Way> ways, List<Node> nodes)
        {
            foreach (var way in ways)
            {
                if (way.Center != null) AddCentreGeometryAsPsudeoNode(way, nodes);
            }
        }


        private void AddCentreGeometryAsPsudeoNode(Way way, List<Node> nodes)
        {
            nodes.Push(new Node { Id = way.Id, IsCentrePlaceHolder = true, Lat = way.Center.Lat, Lon = way.Center.Lon, Tags = way.Tags, Version = way.Version });
        }
    }
}