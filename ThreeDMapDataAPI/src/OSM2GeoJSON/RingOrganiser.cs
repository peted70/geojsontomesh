using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace OsmToGeoJSON
{
    public class RingOrganiser : IRingOrganiser
    {
        public List<Ring> AssignToRings(List<Way> ways)
        {
            var copyWays = new List<Way>(ways);
            var joined = new Stack<Ring>();
            int i = 0;
            while (copyWays.Count != 0)
            {
                Action<List<Node>> how = null;
                var what = new List<Node>();
                var currentWay = copyWays.Pop();
                var currentNodes = currentWay.Nodes.Select(n => currentWay.ResolvedNodes[n]).ToList();
                var newRing = new Ring();
                newRing.AddRange(currentNodes);
                joined.Push(newRing);
                while (copyWays.Count > 0 && currentNodes.First().Id != currentNodes.Last().Id)
                {
                    currentNodes = newRing;
                    var firstNode = currentNodes.First();
                    var lastNode = currentNodes.Last();
                    for (i = 0; i < copyWays.Count; ++i)
                    {
                        what = copyWays[i].ResolvedNodes.Values.ToList();
                        if (lastNode.Equals(what.First()))
                        {
                            how = newRing.AddRange;
                            what = what.Skip(1).ToList();
                            break;
                        }
                        if (lastNode.Equals(what.Last()))
                        {
                            how = newRing.AddRange;
                            what = what.TakeAllButLast().Reverse().ToList();
                            break;
                        }
                        if (firstNode.Equals(what.Last()))
                        {
                            how = (nodes) =>newRing.InsertRange(0, nodes);
                            what = what.TakeAllButLast().ToList();
                            break;
                        }
                        if (firstNode.Equals(what.First()))
                        {
                            how = (nodes) => newRing.InsertRange(0, nodes);
                            what = what.Skip(1).Reverse().ToList();
                            break;
                        }
                        else
                        {
                            what = null;
                            how = null;
                        }
                    }
                    if (what == null)
                    {
                        Debug.WriteLine("Multipolygon contains unclosed ring geometry");
                        break;
                    }
                    var wayToRemove = copyWays[i];
                    copyWays.Remove(wayToRemove);
                    how(what);
                }

            }
            return joined.ToList();
        }
    }
}