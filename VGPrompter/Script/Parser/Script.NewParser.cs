using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace VGPrompter {

    public partial class Script {

        public static partial class Parser {

            class RawLineWrapper<T> {
                public string Key { get; private set; }
                public RawLine Line { get; private set; }
                public T Value { get; private set; }

                public RawLineWrapper(string key, T value, RawLine line) {
                    Key = key;
                    Value = value;
                    Line = line;
                }
            }

            class ParsedVGPScriptProject {

                public string SourceFileName { get; private set; }
                public RawLine[] RawLines { get; private set; }
                public Dictionary<string, RawLineWrapper<string>> Labels { get; private set; }
                public Dictionary<string, RawLineWrapper<KeyValuePair<string, string>>> Definitions { get; private set; }

                public List<VGPBlock> Blocks { get; private set; }

                public ParsedVGPScriptProject() {

                }

                /*public void AddTree(Node2 root) {
                    Blocks = ParseRoot(root).ToList();
                }*/

                public void AddFile(string path, IndentChar indent) {
                    var root = GetTree(ReadVGPSLines(path, indent));
                    // AddTree(root);
                    Blocks = ParseRoot(root).ToList();
                }

                /*public ParsedVGPScriptFile(string src, RawLine[] raw_lines, string[] labels, VGPDefine[] definitions) {
                    SourceFileName = src;
                    RawLines = raw_lines;
                    Labels = labels;
                    Definitions = definitions;
                }*/

                // public static ParsedVGPScriptFile Parse(string src, RawLine[] lines, ref TextManager tm, IndentChar indent_enum = IndentChar.Auto, bool ignore_unsupported_renpy = false)


                // 1. VGPScript File -> RawLines

                static IEnumerable<RawLine> ReadVGPSLines(string path, IndentChar indent_enum = IndentChar.Auto) {
                    var i = 0;
                    char? indent_char = null;
                    int? min_indent = null;
                    int level = 0;
                    int prev_level = 0;
                    string line;

                    switch (indent_enum) {
                        case IndentChar.Tab:
                            indent_char = TAB;
                            break;
                        case IndentChar.Whitespace:
                            indent_char = WHITESPACE;
                            break;
                    }

                    using (var fh = File.OpenText(path)) {
                        while (!fh.EndOfStream) {
                            i++;
                            prev_level = level;
                            line = fh.ReadLine().TrimEnd();

                            // Skip empty line
                            if (string.IsNullOrEmpty(line))
                                continue;

                            char first_whitespace;
                            int indent = 0;
                            if (line[0] == WHITESPACE || line[0] == TAB) {
                                first_whitespace = line[0];
                                if (indent_char.HasValue && indent_char.Value != first_whitespace)
                                    throw new Exception("Inconsistentent indentation character!");
                                int j = 0;
                                while (line[j++] == first_whitespace) { }

                                if (line[j] == WHITESPACE || line[j] == TAB)
                                    throw new Exception("Inconsistentent indentation character!");

                                line = line.Substring(j);

                                indent = j - 1;

                                if (min_indent.HasValue) {
                                    if (indent < min_indent) {
                                        throw new Exception("Unexpected indentation!");
                                    } else if (indent % min_indent != 0) {
                                        throw new Exception("Unexpected indentation: not a multiple!");
                                    }
                                } else {
                                    min_indent = indent;
                                }

                                level = indent / min_indent.Value;

                                if (level > prev_level && level - prev_level > 1)
                                    throw new Exception("Unexpected indentation!");

                            }

                            if (line[0] == COMMENT_CHAR) {
                                // Skip comment line
                                continue;
                            }

                            // Handle in-line comments
                            if (line.Contains('#')) {
                                if (line.Contains('"')) {
                                    line = comment_quotes_re.Replace(line, string.Empty);
                                } else {
                                    line = comment_no_quotes_re.Replace(line, string.Empty);
                                }
                                Console.WriteLine(line);
                            }

                            // Wrap line
                            yield return new RawLine(path, line, i, level);
                        }
                    }
                }

                // 2. RawLines -> Tree

                static Node2 GetTree(IEnumerable<RawLine> lines) {
                    var root = Node2.Root;
                    int delta;
                    Node2 child;

                    var stack = new Stack<Node2>();
                    stack.Push(root);

                    foreach (var line in lines) {
                        if (line.Level < stack.Count - 2) {
                            delta = stack.Count - line.Level - 1;
                            for (int i = 0; i < delta; i++) {
                                child = stack.Pop();
                                stack.Peek().Add(child);
                            }
                        } else if (stack.Count > 1 && line.Level == stack.Count - 2) {
                            child = stack.Pop();
                            stack.Peek().Add(child);
                        }
                        stack.Push(new Node2(line));
                    }
                    for (int i = 0; i < stack.Count; i++) {
                        child = stack.Pop();
                        stack.Peek().Add(child);
                    }

                    return root;
                }

                // 3. Tree -> VGPBlocks

                IEnumerable<VGPBlock> ParseRoot(Node2 root) {

                    foreach (var node in root.Children) {
                        switch (node.Line.Tokens[0].Type) {
                            case TokenType.Label:
                                var label = node.Line.Tokens[0].Value as string;
                                if (Labels.ContainsKey(label))
                                    throw new Exception("Duplicate label!");
                                Labels.Add(label, new RawLineWrapper<string>(label, null, node.Line));
                                yield return (VGPBlock)ParseNode(node, null);
                                break;
                            case TokenType.Define:
                                var kvp = DefineParserRule.Parse(node.Line.Tokens, null);
                                var definition = new RawLineWrapper<KeyValuePair<string, string>>(kvp.Key, kvp, node.Line);
                                if (Definitions.ContainsKey(kvp.Key))
                                    throw new Exception("Global variable assigned multiple times!");
                                Definitions.Add(definition.Key, definition);
                                break;
                            default:
                                throw new Exception("Invalid top level statement!");
                        }
                    }

                }

                Line ParseNode(Node2 node, VGPBlock block) {
                    var line = node.Line;
                    var tokens = line.Tokens;

                    var current_block = block ?? LabelParserRule.Parse(tokens, null);

                    Line current_line = null;
                    var contents = new List<Line>();
                    VGPIfElse ifelse = new VGPIfElse(current_block);

                    if (node.IsEmpty) {
                        // Leaf
                        current_line = tokens2line2(tokens, LeafRules2, current_block);
                    } else {
                        // Block or other node
                        current_line = block == null ? current_block : tokens2line2(tokens, NodeRules2, current_block);

                        foreach (var child in node.Children) {
                            var tmp = ParseNode(child, current_block);
                            if (tmp == null) throw new Exception(string.Format("Null child ILine in {0}!", node.Line.ExceptionString));

                            if (tmp is Conditional) {
                                ifelse.AddCondition(tmp as Conditional);
                            } else {
                                // Add previous IfElse block
                                if (!ifelse.IsEmpty)
                                    AddIfElse(ref ifelse, ref contents);
                                contents.Add(tmp);
                            }
                        }

                        // Add previous IfElse block
                        if (!ifelse.IsEmpty)
                            AddIfElse(ref ifelse, ref contents);

                        if (current_line is VGPMenu) {
                            (current_line as VGPMenu).Contents = contents.Select(x => x as VGPChoice).ToList();
                        } else if (current_line is VGPIfElse) {
                            (current_line as VGPIfElse).Contents = contents.Select(x => x as Conditional).ToList();
                        } else if (current_line is IterableContainer) {
                            (current_line as IterableContainer).Contents = contents;
                        } else {
                            throw new Exception(string.Format("Unexpected ILine container in {0}!", node.Line.ExceptionString));
                        }
                    }


                    // current_line = tokens2line2(tokens, node.IsEmpty ? NodeRules2 : NodeRules2, parent_block);

                    return current_line;
                }

                // 4. Path -> Script

                /*public static ParsedVGPScriptProject FromFile(string path, IndentChar indent = IndentChar.Auto) {
                    var pscript = new ParsedVGPScriptProject();
                    var root = GetTree(ReadVGPSLines(path, indent));
                    pscript.AddTree(root);
                    return pscript;
                }*/

                public static Script Parse(string path, bool recursive = false, IndentChar indent = IndentChar.Auto) {

                    var tm = new TextManager();
                    var pscript = new ParsedVGPScriptProject();

                    if (Directory.Exists(path)) {

                        var lines = new List<RawLine>();
                        var files = Utils.GetScriptFiles(path, recursive);
                        foreach (var f in files) {
                            pscript.AddFile(path, indent);
                        }

                    } else if (File.Exists(path)) {

                        pscript.AddFile(path, indent);

                    } else {

                        throw new Exception("Missing source file or directory!");

                    }

                    // Register definitions

                    foreach (var d in pscript.Definitions) {
                        if (!tm.TryAddDefinition(d.Key, d.Value.Value.Value)) {
                            throw new Exception("Variable redefined!");
                        }
                    }

                    // Create Script

                    return blocks2script(pscript.Blocks, ref tm);

                }

                static Line tokens2line2(Token[] tokens, ParserRule2<Line>[] rules, VGPBlock parent) {
                    var first_token_type = tokens[0].Type;
                    foreach (var rule in rules) {
                        if (rule.FirstTokenType == first_token_type)
                            return rule.Parse(tokens, parent);
                    }
                    return null;
                }

                static bool IsToInterpolate2(RawLine line, ref TextManager tm) {

                    string ikey;
                    var text = line.Text;

                    if (nested_interpolation_re.Match(text).Success)
                        throw new Exception(string.Format(
                            "Nested interpolation in line '{0}'!", line.Text));

                    var m = string_interpolation_re.Matches(text);
                    var to_interpolate = m.Count > 0;

                    if (to_interpolate) {
                        foreach (Group g in m) {

                            ikey = g.Value;

                            if (string.IsNullOrEmpty(ikey))
                                throw new Exception(string.Format(
                                    "Empty variable name in dialogue line '{0}'!", line.Text));

                            if (!tm.Globals.ContainsKey(ikey))
                                throw new Exception(string.Format(
                                    "Undefined variable '{0}' in dialogue line '{1}'!", ikey, line.Text));

                        }
                    }

                    return to_interpolate;
                }

            }

        }

    }

}
