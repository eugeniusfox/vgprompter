using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VGPrompter {

    public partial class Script {

        public static partial class Parser {

            public static Logger Logger = new Logger("Parser");

            const char
                WHITESPACE = ' ',
                TAB = '\t',
                QUOTE = '"',
                COLON = ':',
                COMMENT_CHAR = '#',
                UNDERSCORE = '_',
                RENPY_PYLINE_CHAR = '$';

            const string
                IF = "if",
                ELIF = "elif",
                ELSE = "else",
                PASS = "pass",
                JUMP = "jump",
                CALL = "call",
                MENU = "menu",
                LABEL = "label",
                WHILE = "while",
                RETURN = "return",

                INIT = "init",
                PYTHON = "python",
                WITH = "with",
                SHOW = "show",
                HIDE = "hide",
                PLAY = "play",
                STOP = "stop",
                SCENE = "scene",
                IMAGE = "image",
                DEFINE = "define",
                PAUSE = "pause",
                INIT_PYTHON = "init python",

                PIPE = "|",
                SCRIPT = "script";

            public enum IndentChar {
                Auto,
                Whitespace,
                Tab
            }

            static readonly string[] UNSUPPORTED_RENPY_KEYWORDS = {
                WITH, SHOW, HIDE, PLAY, STOP, SCENE, IMAGE, DEFINE, PAUSE
            };

            static readonly string[] UNSUPPORTED_RENPY_BLOCK_KEYWORDS = {
                INIT, PYTHON, INIT_PYTHON
            };

            static Regex line_re = new Regex(@"^(?:(\w+) )?""(.+)""$");
            static Regex choice_re = new Regex(@"^(?:(\w+) )?""(.+)""(?: if (\w+))?$");

            static Regex unsupported_renpy_re = new Regex(string.Format(@"^({0}) \w+", string.Join(PIPE, UNSUPPORTED_RENPY_KEYWORDS)));
            static Regex unsupported_renpy_block_re = new Regex(string.Format(@"^({0}) ?.*:$", string.Join(PIPE, UNSUPPORTED_RENPY_BLOCK_KEYWORDS)));

            static ParserRule[] LeafRules = new ParserRule[] {
                new ParserRule( PASS,         (tokens, parent) => new VGPPass(), 1),
                new ParserRule( RETURN,       (tokens, parent) => new VGPReturn(), 1),
                new ParserRule( JUMP,         (tokens, parent) => new VGPGoTo(tokens[1], is_call: false), 2),
                new ParserRule( CALL,         (tokens, parent) => new VGPGoTo(tokens[1], is_call: true), 2),
                new ParserRule( string.Empty, (tokens, parent) => new VGPReference(tokens[0]), 1)
            };

            static ParserRule[] NodeRules = new ParserRule[] {
                new ParserRule( MENU,         (tokens, parent) => new VGPMenu(parent), 1),
                new ParserRule( IF,           (tokens, parent) => new Conditional.If(tokens[1], parent), 2),
                new ParserRule( ELIF,         (tokens, parent) => new Conditional.ElseIf(tokens[1], parent), 2),
                new ParserRule( ELSE,         (tokens, parent) => new Conditional.Else(parent), 1),
                new ParserRule( WHILE,        (tokens, parent) => new VGPWhile(tokens[1], parent), 2)
            };

            public static Script ParseSource(string path, bool recursive = false, IndentChar indent = IndentChar.Auto, bool ignore_unsupported_renpy = false) {
                var lines = LoadRawLines(path, recursive, ignore_unsupported_renpy);
                return ParseLines(lines, indent, ignore_unsupported_renpy);
            }

            static string[] LoadRawLines(string path, bool recursive = false, bool ignore_unsupported_renpy = false) {

                if (Directory.Exists(path)) {

                    var lines = new List<string>();
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

            static Script ParseLines(string[] lines, IndentChar indent_enum = IndentChar.Auto, bool ignore_unsupported_renpy = false) {

                char indent;
                if (indent_enum == IndentChar.Auto) {
                    indent = InferIndent(lines);
                } else if (indent_enum == IndentChar.Tab) {
                    indent = TAB;
                } else if (indent_enum == IndentChar.Whitespace) {
                    indent = WHITESPACE;
                } else {
                    throw new Exception("Invalid indent character!");
                }

                var depths = GetLineDepths(lines, indent);

                var indent_values = depths.Distinct().OrderBy(x => x).ToArray();


                // 1. Is it a tree?
                if (indent_values.Length < 2) throw new Exception("No indentation!");

                var min_indent = 1;

                if (indent == WHITESPACE) {
                    min_indent = indent_values[0] == 0 ? indent_values[1] : indent_values[0];

                    // 2. Are all indents multiples of the indentation unit?
                    if (indent_values.Any(x => x % min_indent != 0)) throw new Exception("Irregular indentation!");
                }

                var diffs = Diff(depths);

                if (diffs.Any(x => x > min_indent)) throw new Exception("Unexpected indentation!");


                // 3. Get label blocks
                var line = string.Empty;
                var n = 0;

                var labels = new List<string>();

                var top_lines_indices = depths.FindAllIndexOf(0);
                int[] label_lines_indices;

                var label_lines_indices_tmp = new List<int>();

                foreach (var i in top_lines_indices) {

                    line = lines[i].Trim();
                    n = line.Length;

                    if (line[n - 1] != COLON)
                        throw new Exception(string.Format("Missing colon at line '{0}'!", line));

                    if (unsupported_renpy_block_re.IsMatch(line)) {
                        Logger.Log(string.Format("Ignoring top-level Ren'Py block '{0}'", line));
                        continue;
                    }

                    var label_tokens = line.Substring(0, n - 1).Split(WHITESPACE);

                    if (label_tokens.Length != 2 || label_tokens[0] != LABEL || string.IsNullOrEmpty(label_tokens[1]))
                        throw new Exception("Top-level node is not a valid label!");

                    label_lines_indices_tmp.Add(i);
                    labels.Add(label_tokens[1]);
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

                var script = tree2script(tree, ignore_unsupported_renpy);
                return script;
            }

            static Script blocks2script(List<VGPBlock> blocks) {
                var script = new Script() {
                    Blocks = blocks
                        .Select(x => new KeyValuePair<string, VGPBlock>(x.Label, x))
                        .ToDictionary(x => x.Key, x => x.Value)
                };

                foreach (var block in script.Blocks)
                    block.Value.Script = script;

                return script;
            }

            static Node renpy2tree(string[] lines, int[] label_lines_indices, char indent, bool print = false) {
                int nidx = 0;
                var root_node = new Node(SCRIPT, indent) { Level = -1 };

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

            static Script tree2script(Node root_node, bool ignore_unsupported_renpy = false) {
                VGPBlock block;
                var blocks = new List<VGPBlock>();

                foreach (var node in root_node.Children) {
                    // Format checks were performed in a previous step

                    block = node2ILine(node, null, null, ignore_unsupported_renpy) as VGPBlock;
                    //Logger.Log(block.ToString());

                    blocks.Add(block);
                }

                return blocks2script(blocks);
            }

            static Line node2ILine(Node node, Type parent_type, VGPBlock block, bool ignore_unsupported_renpy = false) {
                Line iline = null;
                var line = node.Label;
                var n = line.Length;
                string[] tokens;
                VGPBlock current_block = block;

                if (node.IsEmpty) {

                    // Leaf

                    if (line.Contains(QUOTE)) {
                        iline = GetLineLeaf(line);
                    } else {
                        tokens = line.Split(WHITESPACE);

                        if (!tokens.All(y => y.All(x => char.IsLetterOrDigit(x) || x == UNDERSCORE)))
                            throw new Exception(string.Format("Invalid characters in functional line '{0}'!", line));

                        var iline_test = GetFunctionalLeaf(tokens);
                        iline = tokens2Leaf(tokens);
                        Assert.ReferenceEquals(iline, iline_test);
                    }

                    if (iline == null) throw new Exception(string.Format("Null leaf from line '{0}'", line));

                } else {

                    // Node

                    if (ignore_unsupported_renpy && unsupported_renpy_block_re.IsMatch(line)) {
                        Logger.Log(string.Format("Ignoring Ren'Py block '{0}'", line));
                        return new VGPPass();
                    }

                    var contents = new List<Line>();
                    var ifelse = new VGPIfElse(current_block);

                    if (line[n - 1] != COLON) throw new Exception("Missing colon!");

                    var trimmed_line = line.Substring(0, n - 1);

                    if (parent_type == null) {

                        // Block

                        tokens = line.Substring(0, n - 1).Split(WHITESPACE);
                        iline = new VGPBlock(tokens[1]);

                        current_block = iline as VGPBlock;

                    } else if (parent_type == typeof(VGPMenu)) {
                        iline = GetChoiceNode(trimmed_line, current_block);
                    } else {
                        var iline_test = GetNode(trimmed_line.Split(WHITESPACE), current_block);
                        iline = tokens2Node(trimmed_line.Split(WHITESPACE), current_block);
                        Assert.ReferenceEquals(iline, iline_test);
                    }

                    if (iline == null) throw new Exception(string.Format("Null node from line '{0}'", line));
                    if (iline is VGPChoice && parent_type != typeof(VGPMenu)) throw new Exception("Choice out of menu!");

                    foreach (var child in node.Children) {
                        var tmp = node2ILine(child, iline.GetType(), current_block, ignore_unsupported_renpy);
                        if (tmp == null) throw new Exception("Null child ILine!");


                        if (tmp is Conditional) {
                            ifelse.AddCondition(tmp as Conditional);
                        } else {
                            if (!ifelse.IsEmpty)
                                AddIfElse(ref ifelse, ref contents);

                            contents.Add(tmp);
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
                        throw new Exception("Unexpected ILine container!");
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

            static VGPChoice GetChoiceNode(string line, VGPBlock parent) {
                var m = choice_re.Match(line);
                if (!m.Success) throw new Exception("Invalid Choice!");
                if (string.IsNullOrEmpty(m.Groups[1].Value)) {
                    return new VGPChoice.VGPAnonymousChoice(m.Groups[2].Value, parent, m.Groups[3].Value);
                } else {
                    return new VGPChoice.VGPNamedChoice(m.Groups[1].Value, m.Groups[2].Value, parent, m.Groups[3].Value);
                }
            }

            static void ParseContents(string[] lines, Node parent, char indent, ref int i) {
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

            static VGPDialogueLine GetLineLeaf(string line) {
                var m = line_re.Match(line);

                if (!m.Success) throw new Exception(string.Format("Invalid Line '{0}'!", line));

                return new VGPDialogueLine(m.Groups[1].Value, m.Groups[2].Value);
            }

            class Tuple2<T1, T2> {
                public T1 Item1 { get; protected set; }
                public T2 Item2 { get; protected set; }

                public Tuple2() { }
 
                public Tuple2(T1 item1, T2 item2) : this() {
                    Item1 = item1;
                    Item2 = item2;
                }
            }

            struct ParserRule {
                public string Keyword { get; private set; }
                public Func<string[], VGPBlock, Line> Constructor { get; private set; }
                public int Count { get; private set; }

                public ParserRule(string keyword, Func<string[], VGPBlock, Line> constructor, int count) : this() {
                    Keyword = keyword;
                    Constructor = constructor;
                    Count = count;
                }
            }

            static Line tokens2Leaf(string[] tokens) {
                return tokens2Line(LeafRules, tokens);
            }

            static Line tokens2Node(string[] tokens, VGPBlock parent) {
                return tokens2Line(NodeRules, tokens, parent);
            }

            static Line tokens2Line(ParserRule[] rules, string[] tokens, VGPBlock parent = null) {

                var first_token = tokens[0];

                foreach (var rule in rules)
                    if ((rule.Keyword == first_token || string.IsNullOrEmpty(rule.Keyword)) && tokens.Length == rule.Count)
                        return rule.Constructor(tokens, parent);

                Utils.LogArray("Invalid line", tokens, Logger);
                throw new Exception("Invalid line!");

            }

            static IEnumerable<string> ReadLines(string path, bool ignore_unsupported_renpy = false) {
                return
                    File.ReadAllLines(path)
                        .Where(x => {
                            var y = x.Trim();
                            var res = !string.IsNullOrEmpty(y) && y[0] != COMMENT_CHAR;
                            if (res && ignore_unsupported_renpy) {
                                res = !(y[0] == RENPY_PYLINE_CHAR || unsupported_renpy_re.Match(y).Success);
                                if (!res)
                                    Logger.Log(string.Format("Ignoring '{0}'", y));
                            }
                            return res;
                        });
            }

            /* Deprecated, for testing purposes onlny */

            [Obsolete]
            static Line GetFunctionalLeaf(string[] tokens) {
                var n = tokens.Length;

                switch (tokens[0]) {

                    case PASS:
                        if (n != 1) throw new Exception("Invalid Pass!");
                        return new VGPPass();
                    case RETURN:
                        if (n != 1) throw new Exception("Invalid Return!");
                        return new VGPReturn();
                    case JUMP:
                        if (n != 2) throw new Exception("Invalid Jump!");
                        return new VGPGoTo(tokens[1], is_call: false);
                    case CALL:
                        if (n != 2) throw new Exception("Invalid Call!");
                        return new VGPGoTo(tokens[1], is_call: true);
                    default:
                        if (n != 1) throw new Exception("Invalid Reference!");
                        return new VGPReference(tokens[0]);
                }

            }

            [Obsolete]
            static Line GetNode(string[] tokens, VGPBlock parent) {

                var n = tokens.Length;

                if (n == 0) throw new Exception("Empty line!");

                switch (tokens[0]) {

                    case MENU:
                        if (n != 1) throw new Exception("Invalid Menu!");
                        return new VGPMenu(parent);
                    case IF:
                        if (n != 2) throw new Exception("Invalid If!");
                        return new Conditional.If(tokens[1], parent);
                    case ELIF:
                        if (n != 2) throw new Exception("Invalid ElIf!");
                        return new Conditional.ElseIf(tokens[1], parent);

                    case ELSE:
                        if (n != 1) throw new Exception("Invalid Else!");
                        return new Conditional.Else(parent);

                    case WHILE:
                        if (n != 2) throw new Exception("Invalid While!");
                        return new VGPWhile(tokens[1], parent);

                    default:
                        Utils.LogArray("Invalid node", tokens, Logger);
                        throw new Exception("Invalid node!");
                }
            }

        }

    }

}