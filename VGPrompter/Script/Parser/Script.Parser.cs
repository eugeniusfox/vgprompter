using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;

namespace VGPrompter {

    public partial class Script {

        public static partial class Parser {

            public static Logger Logger = new Logger("Parser");

            const NumberStyles NUMBER_STYLE = NumberStyles.Any;

            public enum IndentChar {
                Auto,
                Whitespace,
                Tab
            }

            static readonly CultureInfo CULTURE_INFO = CultureInfo.InvariantCulture;

            static bool IsInteger(string s) =>
                s.All(c => char.IsDigit(c));

            static string UnquoteString(string value) =>
                value[0] == '"' && value[value.Length - 1] == '"' ? value.Substring(1, value.Length - 2) : value;

            internal static string CustomUnescapeString(string s) =>
                s.Replace(@"\[", @"[");

            public static Script ParseSource(string path, bool recursive = false, IndentChar indent = IndentChar.Auto, bool ignore_unsupported_renpy = false) {
                var lines = LoadRawLines(path, recursive, ignore_unsupported_renpy);
                return ParseLines(lines, indent, ignore_unsupported_renpy);
            }

            static RawLine[] LoadRawLines(string path, bool recursive = false, bool ignore_unsupported_renpy = false) {

                if (Directory.Exists(path)) {

                    var lines = new List<RawLine>();
                    var files = Utils.GetScriptFiles(path, recursive);
                    foreach (var f in files)
                        lines.AddRange(ReadLines(f, ignore_unsupported_renpy));

                    return lines.ToArray();

                } else if (File.Exists(path)) {

                    return ReadLines(path, ignore_unsupported_renpy).ToArray();

                } else {

                    throw new Exception("Missing source file or directory!");

                }
            }

            static char InferIndent(string[] lines) {
                foreach (var x in lines)
                    if (x[0] == WHITESPACE || x[0] == TAB)
                        return x[0];

                throw new Exception("No indentation found!");
            }

            static Script ParseLines(RawLine[] lines, IndentChar indent_enum = IndentChar.Auto, bool ignore_unsupported_renpy = false) {

                var lines_text = lines.Select(x => x.Text).ToArray();

                char indent;
                if (indent_enum == IndentChar.Auto) {
                    indent = InferIndent(lines_text);
                } else if (indent_enum == IndentChar.Tab) {
                    indent = TAB;
                } else if (indent_enum == IndentChar.Whitespace) {
                    indent = WHITESPACE;
                } else {
                    throw new Exception("Invalid indent character!");
                }

                var depths = GetLineDepths(lines_text, indent);

                var indent_values = depths.Distinct().OrderBy(x => x).ToArray();


                // 1. Is it a tree?
                if (indent_values.Length < 2) throw new Exception("No indentation!");

                var min_indent = 1;

                if (indent == WHITESPACE) {
                    min_indent = indent_values.FirstOrDefault(x => x > 0);  // indent_values[0] == 0 ? indent_values[1] : indent_values[0];

                    if (min_indent < 1) min_indent = 1;

                    // 2. Are all indents multiples of the indentation unit?
                    if (indent_values.Any(x => x % min_indent != 0)) throw new Exception("Irregular indentation!");
                }

                var diffs = Diff(depths);

                for (int i = 0; i < lines.Length - 1; i++) {
                    if (diffs[i] > min_indent) {
                        throw new Exception(string.Format("Unexpected indentation in {0}!", lines[i].ExceptionString));
                    }
                }

                //if (diffs.Any(x => x > min_indent)) throw new Exception("Unexpected indentation!");


                // 3. Get label blocks
                var line = string.Empty;

                var labels = new List<string>();

                var top_lines_indices = depths.FindAllIndexOf(0);
                int[] label_lines_indices;

                var label_lines_indices_tmp = new List<int>();
                var tm = new TextManager();

                foreach (var i in top_lines_indices) {

                    var raw_line = lines[i];

                    line = raw_line.Text.Trim();

                    //n = line.Length;

                    /*if (line[n - 1] != COLON)
                        throw new Exception(string.Format("Missing colon at line '{0}'!", line));*/

                    if (unsupported_renpy_block_re.IsMatch(line)) {
                        Logger.Log(string.Format("Ignoring top-level Ren'Py block '{0}'", line));
                        continue;
                    }

                    var ptokens = Tokenize(line);
                    var first_token_type = ptokens[0].Type;

                    if (first_token_type == TokenType.Define) {

                        var definition = GetDefinition(line, ref tm);

                        if (definition == null) throw new Exception(string.Format("Invalid definition in {0}!", raw_line.ExceptionString));

                        if (!tm.TryAddDefinition(definition.Key, definition.Value)) {
                            throw new Exception(string.Format("Variable '{0}' already initialized in {1}!", definition.Key, raw_line.ExceptionString));
                        }

                    } else if (first_token_type == TokenType.Label) {

                        foreach (var t in ptokens)
                            Console.WriteLine(string.Format("{0} -> {1}", t.Type, t.Value));

                        if (ptokens.Length != 3)
                            throw new Exception(string.Format("Invalid number of tokens for label in {0}!", raw_line.ExceptionString));

                        if (ptokens.Last().Type != TokenType.Colon)
                            throw new Exception(string.Format("Missing trailing comma for label in {0}!", raw_line.ExceptionString));


                        var block = new VGPBlock(ptokens[1].Value.ToString());

                        labels.Add(block.Label);
                        label_lines_indices_tmp.Add(i);

                    } else {

                        throw new Exception(string.Format("Invalid top-level statement in {0}!", raw_line.ExceptionString));

                    }

                }

                if (labels.GroupBy(x => x).Any(g => g.Count() > 1))
                    throw new Exception("Duplicate labels!");

                label_lines_indices = label_lines_indices_tmp.ToArray();

                // 4. Fill label blocks

                Utils.LogArray("Labels", labels.ToArray(), Logger);
                Utils.LogArray("Block line indices", label_lines_indices, Logger);

                Node.IndentSpan = min_indent;
                var tree = renpy2tree(lines, label_lines_indices, indent, print: true);

                // 5. Create script

                var script = tree2script(tree, ref tm, ignore_unsupported_renpy);
                return script;
            }

            static Script blocks2script(List<VGPBlock> blocks, ref TextManager tm) {
                var script = new Script(tm) {
                    Blocks = blocks
                        .Select(x => new KeyValuePair<string, VGPBlock>(x.Label, x))
                        .ToDictionary(x => x.Key, x => x.Value)
                };

                foreach (var block in script.Blocks)
                    block.Value.Script = script;

                return script;
            }

            static Node renpy2tree(RawLine[] lines, int[] label_lines_indices, char indent, bool print = false) {
                int nidx = 0;
                var root_node = Node.Root;

                Node label_node = null;
                foreach (var label_index in label_lines_indices) {
                    nidx = label_index;
                    label_node = new Node(lines[label_index], indent);
                    ParseContents(lines, label_node, indent, ref nidx);
                    root_node.Add(label_node);
                    nidx++;
                }

                if (print) Logger.Log(root_node.ToString());
                return root_node;
            }

            static Script tree2script(Node root_node, ref TextManager tm, bool ignore_unsupported_renpy = false) {
                VGPBlock block;
                var blocks = new List<VGPBlock>();

                foreach (var node in root_node.Children) {
                    // Format checks were performed in a previous step

                    block = node2ILine(node, null, null, ref tm, ignore_unsupported_renpy) as VGPBlock;
                    //Logger.Log(block.ToString());

                    blocks.Add(block);
                }

                return blocks2script(blocks, ref tm);
            }

            static Line node2ILine(Node node, Type parent_type, VGPBlock block, ref TextManager tm, bool ignore_unsupported_renpy = false) {
                Line iline = null;
                var line = node.Label;
                var n = line.Length;
                string[] tokens;
                VGPBlock current_block = block;

                Token[] ptokens = Tokenize(line);

                if (node.IsEmpty) {

                    // Leaf

                    tokens = line.Split(WHITESPACE);

                    iline = tokens2Leaf(tokens);

                    if (iline == null) iline = GetLineLeaf(line, current_block.Label, ref tm);

                    if (iline == null) throw new Exception(string.Format("Null leaf from line '{0}'", node.Line.ExceptionString));

                    /*var definition = iline as VGPDefine;
                    if (definition != null) {
                        current_block.Variables.Add(definition.Key, definition.Value);
                    }*/

                } else {

                    // Node

                    if (ignore_unsupported_renpy && unsupported_renpy_block_re.IsMatch(line)) {
                        Logger.Log(string.Format("Ignoring Ren'Py block '{0}'", line));
                        return new VGPPass();
                    }

                    var contents = new List<Line>();
                    var ifelse = new VGPIfElse(current_block);

                    if (line[n - 1] != COLON) throw new Exception(string.Format("Expected ending colon in {0}!", node.Line.ExceptionString));

                    var trimmed_line = line.Substring(0, n - 1);

                    if (parent_type == null) {

                        // Block

                        tokens = line.Substring(0, n - 1).Split(WHITESPACE);
                        iline = new VGPBlock(tokens[1]);

                        current_block = iline as VGPBlock;

                    } else if (parent_type == typeof(VGPMenu)) {
                        iline = GetChoiceNode(trimmed_line, current_block, ref tm);
                    } else {
                        // iline = tokens2Node(trimmed_line.Split(WHITESPACE), current_block);
                        iline = tokens2Node2(ptokens, current_block);
                    }

                    if (iline == null) throw new Exception(string.Format("Null node from {0}!", node.Line.ExceptionString));
                    if (iline is VGPChoice && parent_type != typeof(VGPMenu)) throw new Exception(string.Format("Choice out of menu in {0}!", node.Line.ExceptionString));

                    foreach (var child in node.Children) {
                        var tmp = node2ILine(child, iline.GetType(), current_block, ref tm, ignore_unsupported_renpy);
                        if (tmp == null) throw new Exception(string.Format("Null child ILine in {0}!", node.Line.ExceptionString));

                        if (tmp is Conditional) {

                            ifelse.AddCondition(tmp as Conditional);

                        } else {

                            // Add previous IfElse block
                            if (!ifelse.IsEmpty)
                                AddIfElse(ref ifelse, ref contents);

                            // Skip VGPDefine objects
                            //if (!(tmp is VGPDefine)) {
                             contents.Add(tmp);
                            //}

                        }
                    }

                    if (!ifelse.IsEmpty)
                        AddIfElse(ref ifelse, ref contents);

                    if (iline is VGPMenu) {
                        (iline as VGPMenu).Contents = contents.Select(x => x as VGPChoice).ToList();
                    } else if (iline is VGPIfElse) {
                        (iline as VGPIfElse).Contents = contents.Select(x => x as Conditional).ToList();
                    } else if (iline is IterableContainer) {
                        (iline as IterableContainer).Contents = contents;
                    } else {
                        throw new Exception(string.Format("Unexpected ILine container in {0}!", node.Line.ExceptionString));
                    }

                }
                return iline;
            }

            static void AddIfElse(ref VGPIfElse ifelse, ref List<Line> output) {
                output.Add(ifelse);
                ifelse = new VGPIfElse(ifelse.Parent);
            }

            static int GetIndent(string s, char indent) {
                int k = -1;
                while (s[++k] == indent);
                return k;
            }

            static VGPChoice GetChoiceNode(string line, VGPBlock parent, ref TextManager tm) {
                var m = choice_re.Match(line);
                if (!m.Success) throw new Exception("Invalid Choice!");

                var tag = m.Groups[1].Value;
                var text = UnescapeTextString(m.Groups[2].Value);
                var condition = m.Groups[3].Value;

                var to_interpolate = IsToInterpolate(text, line, ref tm);

                var hash = tm.AddText(parent.Label, text);

                if (string.IsNullOrEmpty(m.Groups[1].Value)) {
                    return new VGPChoice.VGPAnonymousChoice(hash, parent, to_interpolate, condition);
                } else {
                    return new VGPChoice.VGPNamedChoice(tag, hash, parent, to_interpolate, condition);
                }
            }

            static void ParseContents(RawLine[] lines, Node parent, char indent, ref int i) {
                Node node;
                while (++i < lines.Length) {
                    node = new Node(lines[i], indent);
                    if (node.Level > parent.Level) {
                        ParseContents(lines, node, indent, ref i);
                        parent.Add(node);
                    } else {
                        i -= 1;
                        return;
                    }
                }
            }

            static int[] GetLineDepths(string[] x, char indent) {
                int[] result = new int[x.Length];
                for (var i = 0; i < x.Length; i++) {
                    result[i] = x[i].Length - x[i].TrimStart(indent).Length;
                }
                return result;
            }

            static int[] Diff(int[] x) {
                int[] result = new int[x.Length - 1];
                for (var i = 0; i < x.Length - 1; i++) {
                    result[i] = x[i + 1] - x[i];
                }
                return result;
            }

            internal static bool IsToInterpolate(string text, string line, ref TextManager tm) {

                string ikey;

                if (nested_interpolation_re.Match(text).Success) throw new Exception(string.Format("Nested interpolation in line '{0}'!", line));

                var m = string_interpolation_re.Matches(text);
                var to_interpolate = m.Count > 0;

                if (to_interpolate) {
                    foreach (Group g in m) {

                        ikey = g.Value;

                        if (string.IsNullOrEmpty(ikey))
                            throw new Exception(string.Format("Empty variable name in dialogue line '{0}'!", line));

                        if (!tm.Globals.ContainsKey(ikey))
                            throw new Exception(string.Format("Undefined variable '{0}' in dialogue line '{1}'!", ikey, line));

                    }
                }

                return to_interpolate;
            }

            static VGPDialogueLine GetLineLeaf(string line, string label, ref TextManager tm) {
                var m = line_re.Match(line);

                if (!m.Success) throw new Exception(string.Format("Invalid Line '{0}'!", line));

                var tag = m.Groups[1].Value;
                var text = UnescapeTextString(m.Groups[2].Value);

                // String interpolation validation (string aliases must be defined)
                var to_interpolate = IsToInterpolate(text, line, ref tm);

                // Extract text and get its hash
                var hash = tm.AddText(label, text);

                return new VGPDialogueLine(label, hash, tag, to_interpolate);
            }

            static VGPDefine GetDefinition(string line, ref TextManager tm) {

                var m = define_re.Match(line);

                if (!m.Success) throw new Exception(string.Format("Invalid definition '{0}'!", line));

                var tag = m.Groups[1].Value;
                var text = UnescapeTextString(m.Groups[2].Value);

                // String interpolation validation (string aliases must be defined)
                var to_interpolate = IsToInterpolate(text, line, ref tm);

                return new VGPDefine(tag, text, to_interpolate);
                // Tokenize ignoring spaces between double quotes
                // Run tokens through existing delegate (excise it from the list?)
            }

            static string UnescapeTextString(string s) {
                return s
                    .Replace(@"\\", @"\")
                    .Replace(@"\""", @"""");
            }

            internal static string InterpolateText(string text, ref TextManager tm) {
                var out_text = text;
                var m = string_interpolation_re.Matches(text);
                var to_interpolate = m.Count > 0;

                string ikey, itext;

                if (to_interpolate) {
                    foreach (Group g in m) {

                        ikey = g.Value;

                        if (tm.Globals.TryGetValue(ikey, out itext)) {

                            out_text = out_text.Replace(string.Format("[{0}]", ikey), itext);

                        } else {

                            throw new Exception(string.Format("Undefined variable '{0}'!", g.Value));

                        }

                    }
                }

                return out_text;
            }


            /* From tokens to Line objects */

            static object ParseLiteral(string s) {
                if (s == TRUE) {

                    // True
                    return true;

                } else if (s == FALSE) {

                    // False
                    return false;

                } else if (
                    (s[0] == QUOTE && s[s.Length - 1] == QUOTE) ||
                    (s[0] == SINGLE_QUOTE && s[s.Length - 1] == SINGLE_QUOTE)) {

                    // String
                    return s.Substring(1, s.Length - 2);

                } else if (s.All(c => char.IsDigit(c)) && int.TryParse(s, NUMBER_STYLE, CULTURE_INFO, out int i)) {

                    // Integer
                    return i;

                } else if (double.TryParse(s, NUMBER_STYLE, CULTURE_INFO, out double d)) {

                    // Double
                    return d;

                } else if (
                    s[s.Length - 1] == FLOAT_SUFFIX &&
                    float.TryParse(s.Substring(0, s.Length - 1), NUMBER_STYLE, CULTURE_INFO, out float f)) {

                    // Float
                    return f;

                } else {

                    // Null
                    return null;

                }
            }

            static VGPBaseReference GetFunctionCall(string[] tokens) {
                var s = string.Join(string.Empty, tokens);
                var m = function_call_line_re.Match(s);
                if (!m.Success) return null;

                var tag = m.Groups[1].Value.Trim();

                if (m.Groups[2].Value.Trim() == string.Empty) {

                    // Action
                    return new VGPReference(tag);

                } else {

                    // Func
                    object[] argv = m.Groups[2].Value
                        .Split(COMMA_SPLIT)
                        .Select(x => {
                            return ParseLiteral(x.Trim()) ?? throw new Exception(string.Format(
                                "Unsupported type for argument '{0}'! Only boolean, string, integer, float and double literals are allowed.", x.Trim()));
                        }).ToArray();

                    return new VGPFunction(tag, argv);

                }
            }

            static Line tokens2Line(ParserRule[] rules, string[] tokens, VGPBlock parent = null, Func<string[], Line> fallback = null) {

                var first_token = tokens[0];

                foreach (var rule in rules) {
                    if ((rule.Keyword == first_token || string.IsNullOrEmpty(rule.Keyword)) &&
                        (!rule.Count.HasValue || tokens.Length == rule.Count.Value) &&
                        (rule.Validator == null || rule.Validator(tokens))) {

                        return rule.Constructor(tokens, parent);

                    }
                }

                var line = fallback?.Invoke(tokens);

                return line;

                // if (line != null) return line;
                // Utils.LogArray("Invalid line", tokens, Logger);
                // throw new Exception(string.Format("Invalid line with tokens: {0}!", string.Join(", ", tokens)));

            }

            static Line tokens2TopLevel(string[] tokens) {
                return tokens2Line(TopLevelRules, tokens);
            }

            static Line tokens2Leaf(string[] tokens) {
                return tokens2Line(LeafRules, tokens, fallback: GetFunctionCall);
            }

            static Line tokens2Node(string[] tokens, VGPBlock parent) {
                return tokens2Line(NodeRules, tokens, parent);
            }

            static Line tokens2Node2(Token[] tokens, VGPBlock parent) {
                var first_token_type = tokens[0].Type;
                foreach (var rule in NodeRules2) {
                    if (rule.FirstTokenType == first_token_type)
                        return rule.Parse(tokens, parent);
                }
                return null;
            }

            /* Load and pre-filter rows */

            static IEnumerable<RawLine> ReadLines(string path, bool ignore_unsupported_renpy = false) {
                return
                    File.ReadAllLines(path)
                        .Select(y => {
                            var line = y;
                            
                            // Handle in-line comments
                            if (y.Contains('#')) {
                                if (y.Contains('"')) {
                                    line = comment_quotes_re.Replace(y, string.Empty);
                                } else {
                                    line = comment_no_quotes_re.Replace(y, string.Empty);
                                }
                                Console.WriteLine(line);
                            }

                            return line.TrimEnd();
                        }).Select((x, i) => new RawLine(path, x, i))
                        .Where(x => {
                            var y = x.Text.Trim();
                            var res = !string.IsNullOrEmpty(y) && y[0] != COMMENT_CHAR;
                            if (res && ignore_unsupported_renpy) {
                                res = !(y[0] == RENPY_PYLINE_CHAR || unsupported_renpy_re.Match(y).Success);
                                if (!res)
                                    Logger.Log(string.Format("Ignoring '{0}'", y));
                            }
                            return res;
                        });
            }

        }

    }

}