using Godot;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biosplit2.Scripts
{
    public static class GodotUtils
    {
        public static T[] ResolvePaths<T>(this Node node, IEnumerable<NodePath> paths) where T : Node
        {
            List<T> result = new List<T>();
            foreach(var path in paths)
            {
                if (path == null)
                    continue;
                if (path.IsEmpty())
                    continue;
                var nd = node.GetNodeOrNull<T>(path);
                if (nd != null)
                    result.Add(nd);
            }
            return result.ToArray();
        }
    }
}
