using System.Collections.Generic;
using System;
using System.Linq;

namespace VGPrompter {

    public partial class Script {

        public static partial class Parser {

            class Node2 {
                public RawLine Line { get; private set; }
                public int Level => Line.Level;

                public Node2() { }

                public Node2(RawLine line) {
                    Line = line;
                }

                public List<Node2> Children { get; set; }

                public Node2 LastChild { get { return Children.LastOrDefault(); } }
                public bool IsEmpty { get { return Children.Count == 0; } }

                public static Node2 Root => new Node2();

                public void Add(Node2 x) {
                    Children.Add(x);
                }
            }

            class Node {

                public static int _indent;
                public static int? IndentSpan {
                    get { return _indent; }
                    set {
                        if (value == null || value.Value < 0) throw new Exception("Invalid indentation span!");
                        _indent = value.Value;
                    }
                }

                RawLine _line;

                public int Level { get; set; }

                public string Label => _line.Text;

                public RawLine Line => _line;

                //public int Index => _line.Index;
                //public string Source => _line.Source;
                //public string ExceptionString => _line.ExceptionString;

                public List<Node> Children { get; set; }

                public Node LastChild { get { return Children.LastOrDefault(); } }
                public bool IsEmpty { get { return Children.Count == 0; } }

                public static Node Root {
                    get { return new Node(); }
                }

                Node() {
                    _line = new RawLine();
                    Children = new List<Node>();
                    Level = -1;
                }

                public Node(RawLine line, char indent, List<Node> children = null) {

                    if (IndentSpan == null) throw new Exception("Indentation span not set!");

                    Level = GetIndent(line.Text, indent) / _indent;
                    Children = children ?? new List<Node>();

                    _line = line.Trim();

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