using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VGPrompter {

    public partial class Script {

        public static partial class Parser {

            class ParsedVGPScriptFile {

                public string SourceFileName { get; private set; }
                public RawLine[] RawLines { get; private set; }
                public string[] Labels { get; private set; }
                public VGPDefine[] Definitions { get; private set; }

                public ParsedVGPScriptFile() {

                }

                public ParsedVGPScriptFile(string src, RawLine[] raw_lines, string[] labels, VGPDefine[] definitions) {
                    SourceFileName = src;
                    RawLines = raw_lines;
                    Labels = labels;
                    Definitions = definitions;
                }

                public static ParsedVGPScriptFile Parse(string src, RawLine[] lines, ref TextManager tm, IndentChar indent_enum = IndentChar.Auto, bool ignore_unsupported_renpy = false) {

                    List<int> top_lines_indices;

                    // Validate the indentation and assign a value to RawLine.Level
                    ComputeLineDepths(src, lines, out top_lines_indices, indent_enum);

                    var labels = new List<string>();
                    var label_lines_indices = new List<int>();
                    var definitions = new List<VGPDefine>();
                    var line = string.Empty;
                    Token[] tokens;

                    foreach (var i in top_lines_indices) {
                        line = lines[i].Text;

                        // Unsupported Ren'Py check [DEPRECATED]
                        if (unsupported_renpy_block_re.IsMatch(line)) {
                            Logger.Log(string.Format("Ignoring top-level Ren'Py block '{0}'", line));
                            continue;
                        }

                        tokens = Tokenize(line);

                        switch (tokens[0].Type) {

                            case TokenType.Define:
                                var definition = DefineParserRule.Parse(tokens, null) as VGPDefine;

                                // Interpolation check?
                                // ToDo

                                // Register definition
                                if (!tm.TryAddDefinition(definition.Key, definition.Value))
                                    throw new Exception(string.Format(
                                        "Variable '{0}' already initialized in {1}!", definition.Key, lines[i].ExceptionString));
                                break;

                            case TokenType.Label:
                                var block = LabelParserRule.Parse(tokens, null) as VGPBlock;
                                if (labels.Contains(block.Label))
                                    throw new Exception(string.Format(
                                        "Duplicate label in {0}!", lines[i].ExceptionString));
                                labels.Add(block.Label);
                                label_lines_indices.Add(i);
                                break;

                            default:
                                throw new Exception(string.Format(
                                    "Invalid top-level statement in {0}!", lines[i].ExceptionString));
                        }

                    }


                    return new ParsedVGPScriptFile(src, lines, labels.ToArray(), definitions.ToArray());
                }

                static char InferIndent2(string src, string[] lines) {
                    var i = 0;
                    while (!(lines[i][0] == WHITESPACE || lines[i][0] == TAB)) i++;
                    if (i == lines.Length)
                        throw new Exception(string.Format(
                            "No indentation found in '{0}'!", src));
                    return lines[i - 1][0];
                }

                static void ComputeLineDepths(string src, RawLine[] lines, out List<int> top_level_lines, IndentChar indent_enum = IndentChar.Auto) {

                    var lines_text = lines.Select(x => x.Text).ToArray();

                    top_level_lines = new List<int>();

                    char indent;

                    if (indent_enum == IndentChar.Auto) {
                        indent = InferIndent2(src, lines_text);
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
                    if (indent_values.Length < 2)
                        throw new Exception("No indentation!");

                    var min_indent = 1;

                    if (indent == WHITESPACE) {
                        min_indent = indent_values.FirstOrDefault(x => x > 0);

                        if (min_indent < 1) min_indent = 1;

                        // 2. Are all indents multiples of the indentation unit?
                        if (indent_values.Any(x => x % min_indent != 0))
                            throw new Exception("Irregular indentation!");
                    }

                    var diffs = Diff(depths);

                    for (int i = 0; i < lines_text.Length - 1; i++) {
                        lines[i].Level = depths[i] / min_indent;
                        if (diffs[i] > min_indent) {
                            throw new Exception(string.Format(
                                "Unexpected indentation in {0}!", lines[i].ExceptionString));
                        }
                        if (depths[i] == 0) {
                            top_level_lines.Add(i);
                        }
                    }

                }

            }

        }

    }

}
