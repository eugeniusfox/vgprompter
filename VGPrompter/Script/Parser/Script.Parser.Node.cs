using System.Collections.Generic;
using System;
using System.Linq;

namespace VGPrompter {

    public partial class Script {

        public static partial class Parser {

            class Node {

                public static int _indent;
                public static int? IndentSpan {
                    get { return _indent; }
                    set {
                        if (value == null || value.Value < 0) throw new Exception("Invalid indentation span!");
                        _indent = value.Value;
                    }
                }

                public int Level { get; set; }
                public string Label { get; set; }
                public List<Node> Children { get; set; }

                public Node LastChild { get { return Children.LastOrDefault(); } }
                public bool IsEmpty { get { return Children.Count == 0; } }

                public Node(char indent) {
                    if (IndentSpan == null) throw new Exception("Indentation span not set!");
                    Children = new List<Node>();
                }

                public Node(string label, char indent, List<Node> children = null)
                    : this(indent) {
                    Label = label.Trim();
                    Level = GetIndent(label, indent) / _indent;
                    if (children != null)
                        Children = children;
                }

                public void Add(Node x) {
                    Children.Add(x);
                }

                public new string ToString() {
                    return IsEmpty ? Label : string.Format("{0} [{1}]: ({2})",
                        Label, Children.Count, string.Join(COMMA, Children.Select(x => x.ToString()).ToArray()));
                }

            }

        }

    }

}