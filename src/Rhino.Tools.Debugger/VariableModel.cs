using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aga.Controls.Tree;

namespace Rhino.Tools.Debugger
{
    public class VariableModel : ITreeModel
    {
        private readonly VariableNode root;

        public VariableModel(Dim debugger, object scope)
        {
            root = new VariableNode(debugger, scope, "this");
        }

        public VariableModel()
        {
        }

        public IEnumerable GetChildren(object parent)
        {
            if (parent == null)
            {
                if (root != null)
                    return root.Children;
            }
            else
            {
                var node = parent as VariableNode;
                if (node != null)
                    return node.Children;
            }
            return Enumerable.Empty<VariableNode>();
        }

        public bool HasChildren(object parent)
        {
            var enumerable = GetChildren(parent);
            return enumerable.Cast<object>().Any();
        }

        private sealed class VariableNode
        {
            private readonly Dim debugger;

            /// <summary>The script object.</summary>
            private readonly object @object;

            /// <summary>The object name.</summary>
            /// <remarks>Either a String or an Integer.</remarks>
            private readonly object id;

            /// <summary>Creates a new VariableNode.</summary>
            public VariableNode(Dim debugger, object @object, object id)
            {
                this.debugger = debugger;
                this.@object = @object;
                this.id = id;
            }

            /// <summary>Returns a string representation of this node.</summary>
            public override string ToString()
            {
                return id is string ? (string)id : "[" + (int)id + "]";
            }

            public string Name
            {
                get { return ToString(); }
            }

            public string Value
            {
                get
                {
                    // Value
                    var result = GetValue();
                    return Sanitize(result);
                }
            }

            /// <summary>Array of child nodes.</summary>
            /// <remarks>This is filled with the properties of the object. </remarks>
            public IEnumerable<VariableNode> Children
            {
                get
                {
                    object value = GetValue(this);
                    object[] ids = debugger.GetObjectIds(value);
                    if (ids == null)
                        return Enumerable.Empty<VariableNode>();
                    
                    return ids.OrderBy(x => x, new IdComparer()).Select(x => new VariableNode(debugger, value, x));
                }
            }

            private string GetValue()
            {
                try
                {
                    return debugger.ObjectToString(GetValue(this));
                }
                catch (Exception exc)
                {
                    return exc.Message;
                }
            }

            private static string Sanitize(string result)
            {
                var buf = new StringBuilder();
                foreach (char ch in result)
                {
                    buf.Append(char.IsControl(ch) ? ' ' : ch);
                }
                return buf.ToString();
            }

            /// <summary>Returns the value of the given node.</summary>
            private object GetValue(VariableNode node)
            {
                try
                {
                    return debugger.GetObjectProperty(node.@object, node.id);
                }
                catch (Exception)
                {
                    return "undefined";
                }
            }
        }

        private sealed class IdComparer : IComparer<object>
        {
            public int Compare(object l, object r)
            {
                var s = l as string;
                if (s != null)
                {
                    if (r is int)
                    {
                        return -1;
                    }
                    return String.Compare(s, (string)r, StringComparison.OrdinalIgnoreCase);
                }
                if (r is string)
                {
                    return 1;
                }
                return (int)l - (int)r;
            }
        }
    }
}
