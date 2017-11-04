using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;

namespace VGPrompter {

    public partial class Script {

        public static partial class Parser {

            struct ParserRule {
                public string Keyword { get; private set; }
                public Func<string[], VGPBlock, Line> Constructor { get; private set; }
                public Func<string[], bool> Validator { get; private set; }
                public int? Count { get; private set; }

                public ParserRule(string keyword, Func<string[], VGPBlock, Line> constructor, int? count = null, Func<string[], bool> validator = null) : this() {
                    Keyword = keyword;
                    Constructor = constructor;
                    Validator = validator;
                    Count = count;
                }
            }

            public static Logger Logger = new Logger("Parser");

            const char
                WHITESPACE = ' ',
                FLOAT_SUFFIX = 'f',
                TAB = '\t',
                QUOTE = '"',
                SINGLE_QUOTE = '\'',
                COLON = ':',
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
                EQUAL = "=",
                SCRIPT = "script";

            const NumberStyles NUMBER_STYLE = NumberStyles.Any;

            public enum IndentChar {
                Auto,
                Whitespace,
                Tab
            }

            static readonly CultureInfo CULTURE_INFO = CultureInfo.InvariantCulture;

            static readonly char[] COMMA_SPLIT = { ',' };

            static readonly string[]
                UNSUPPORTED_RENPY_KEYWORDS = {
                    WITH, SHOW, HIDE, PLAY, STOP, SCENE, IMAGE, PAUSE
                },

                UNSUPPORTED_RENPY_BLOCK_KEYWORDS = {
                    INIT, PYTHON, INIT_PYTHON
                };

            const string
                IDENTIFIER = @"[a-zA-Z_]\w*",
                NUMERIC = @"\d+(?:\.\d+(?:f)?)?",
                DOUBLE_QUOTED_STRING_LITERAL = @""".*""",
                DOUBLE_QUOTED_STRING_LITERAL_CAPTURING = @"""(.*)""";


            static readonly string

                LITERAL = string.Format(
                    @"(?:{0}|"".*""|'.*'|{1}|True|False)", IDENTIFIER, NUMERIC),

                FUNCTION_CALL = string.Format(
                    @"({0})\s*(?:\(({1}(?:,{1})*)?\))?", IDENTIFIER, LITERAL),

                LINE_RE = string.Format(
                    @"^(?:({0}) )?{1}$", IDENTIFIER, DOUBLE_QUOTED_STRING_LITERAL_CAPTURING),

                DEFINE_RE = string.Format(
                    @"^define\s+({0})\s+=\s+(?:{1}|({2}))\s*$", IDENTIFIER, DOUBLE_QUOTED_STRING_LITERAL_CAPTURING, NUMERIC),

                CHOICE_RE = string.Format(
                    @"^(?:({0})\s+)?{1}(?:\s+if\s+({2}))?$", IDENTIFIER, DOUBLE_QUOTED_STRING_LITERAL_CAPTURING, FUNCTION_CALL);


            static Regex line_re = new Regex(LINE_RE, RegexOptions.Compiled);
            static Regex define_re = new Regex(DEFINE_RE, RegexOptions.Compiled);
            static Regex choice_re = new Regex(CHOICE_RE, RegexOptions.Compiled);

            public static Regex string_interpolation_re = new Regex(@"(?<=(?<!\\)\[)\w+(?=\])", RegexOptions.Compiled);
            public static Regex nested_interpolation_re = new Regex(@"\[[^\]]*\[", RegexOptions.Compiled);

            static Regex function_call_re = new Regex(FUNCTION_CALL, RegexOptions.Compiled);
            static Regex function_call_line_re = new Regex(string.Format("^{0}$", FUNCTION_CALL), RegexOptions.Compiled);

            // The DEFINE rule is never used (due to the non-standard tokenization it requires)
            static ParserRule[] TopLevelRules = new ParserRule[] {
                new ParserRule( LABEL,        (tokens, parent) => new VGPBlock(tokens[1].Substring(0, tokens[1].Length - 1)), 2,
                                              (tokens)         => tokens[1][tokens[1].Length - 1] == COLON)
                /*new ParserRule( DEFINE,       (tokens, parent) => new VGPDefine(tokens[1], UnescapeTextString(UnquoteString(tokens[3])), false), 4,
                                              (tokens)         => tokens[2] == EQUAL && define_value_re.IsMatch(tokens[3]))*/
            };

            static ParserRule[] LeafRules = new ParserRule[] {
                new ParserRule( PASS,         (tokens, parent) => new VGPPass(), 1),
                new ParserRule( RETURN,       (tokens, parent) => new VGPReturn(), 1),
                new ParserRule( JUMP,         (tokens, parent) => new VGPGoTo(tokens[1], is_call: false), 2),
                new ParserRule( CALL,         (tokens, parent) => new VGPGoTo(tokens[1], is_call: true), 2)
                /*new ParserRule( string.Empty, (tokens, parent) => new VGPReference(tokens[0], argv: tokens.Length > 1 ? tokens.Skip(1).ToArray() : null), null,
                                              (tokens)         => true)*/
            };

            static ParserRule[] NodeRules = new ParserRule[] {
                new ParserRule( MENU,         (tokens, parent) => new VGPMenu(parent, tokens.Length == 2 ? (int?)int.Parse(tokens[1]) : null), null,
                                              (tokens)         => tokens.Length == 1 || (tokens.Length == 2 && IsInteger(tokens[1]))),

                new ParserRule( IF,           (tokens, parent) => new Conditional.If(tokens[1], parent), 2),
                new ParserRule( ELIF,         (tokens, parent) => new Conditional.ElseIf(tokens[1], parent), 2),
                new ParserRule( ELSE,         (tokens, parent) => new Conditional.Else(parent), 1),
                new ParserRule( WHILE,        (tokens, parent) => new VGPWhile(tokens[1], parent), 2)
            };

            static bool IsInteger(string s) =>
                s.All(c => char.IsDigit(c));

            static string UnquoteString(string value) =>
                value[0] == '"' && value[value.Length - 1] == '"' ? value.Substring(1, value.Length - 2) : value;

            internal static string CustomUnescapeString(string s) =>
                s.Replace(@"\[", @"[");

            public static Script ParseSource(string path, bool recursive = false, IndentChar indent = IndentChar.Auto) {
                var lines = LoadRawLines(path, recursive);
                return ParseLines(lines, indent);
            }

            static RawLine[] LoadRawLines(string path, bool recursive = false) {

                if (Directory.Exists(path)) {

                    var lines = new List<RawLine>();
                    var files = Utils.GetScriptFiles(path, recursive);
                    foreach (var f in files)
                        lines.AddRange(ReadLines(f));

                    return lines.ToArray();

                } else if (File.Exists(path)) {

                    return ReadLines(path).ToArray();

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

            static Script ParseLines(RawLine[] lines, IndentChar indent_enum = IndentChar.Auto) {

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

                    if (line.StartsWith(DEFINE)) {

                        var definition = GetDefinition(line, ref tm);

                        if (definition == null) throw new Exception(string.Format("Invalid definition in {0}!", raw_line.ExceptionString));

                        if (!tm.TryAddDefinition(definition.Key, definition.Value)) {
                            throw new Exception(string.Format("Variable '{0}' already initialized in {1}!", definition.Key, raw_line.ExceptionString));
                        }

                    } else if (line.StartsWith(LABEL)) {

                        var block = tokens2TopLevel(line.Split(WHITESPACE)) as VGPBlock;

                        if (block == null) throw new Exception(string.Format("Invalid label in {0}!", raw_line.ExceptionString));

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

                var script = tree2script(tree, ref tm);
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

            static Script tree2script(Node root_node, ref TextManager tm) {
                VGPBlock block;
                var blocks = new List<VGPBlock>();

                foreach (var node in root_node.Children) {
                    // Format checks were performed in a previous step

                    block = node2ILine(node, null, null, ref tm) as VGPBlock;
                    //Logger.Log(block.ToString());

                    blocks.Add(block);
                }

                return blocks2script(blocks, ref tm);
            }

            static Line node2ILine(Node node, Type parent_type, VGPBlock block, ref TextManager tm) {
                Line iline = null;
                var line = node.Label;
                var n = line.Length;
                string[] tokens;
                VGPBlock current_block = block;

                if (node.IsEmpty) {

                    // Leaf

                    tokens = line.Split(WHITESPACE);

                    iline = tokens2Leaf(tokens);

                    if (iline == null) iline = GetLineLeaf(line, current_block.Label, ref tm);

                    if (iline == null) throw new Exception(string.Format("Null leaf from line '{0}'", node.Line.ExceptionString));

                } else {

                    // Node

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
                        iline = tokens2Node(trimmed_line.Split(WHITESPACE), current_block);
                    }

                    if (iline == null) throw new Exception(string.Format("Null node from {0}!", node.Line.ExceptionString));
                    if (iline is VGPChoice && parent_type != typeof(VGPMenu)) throw new Exception(string.Format("Choice out of menu in {0}!", node.Line.ExceptionString));

                    foreach (var child in node.Children) {
                        var tmp = node2ILine(child, iline.GetType(), current_block, ref tm);
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


            /* Load and pre-filter rows */

            static IEnumerable<RawLine> ReadLines(string path) {
                return
                    File.ReadAllLines(path)
                        .Select((x, i) => new RawLine(path, x, i))
                        .Where(x => !x.IsEmptyOrComment);
            }

        }

    }

}