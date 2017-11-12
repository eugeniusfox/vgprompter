using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace VGPrompter {

    public partial class Script {

        public static partial class Parser {


            public static Script ParseSource2(string path, bool recursive = false, IndentChar indent = IndentChar.Auto) {
                var lines = LoadRawLines2(path, recursive);
                return ParseLines2(lines, indent);
            }

            static RawLine[] LoadRawLines2(string path, bool recursive = false) {

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

            const char COMMENT_CHAR = '#';

            const string CSHARP_LINE = CSHARP + ":";

            const string AUTO_METHOD_PREFIX = "__vgp_";

            static Regex conditional_re = new Regex(@"^(\s*(?:if|elif|while)) +(.+):");

            const string IDENTIFIER2 = @"[a-zA-Z_][a-zA-Z0-9_]*";
            static Regex identifier_re = new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*$"); // new Regex(string.Format("^{0}$", IDENTIFIER2));

            // Comment patterns
            static Regex comment_quotes_re = new Regex(@"(?<=(?:"".*?"").*?)\#.*?$", RegexOptions.Compiled);
            static Regex comment_no_quotes_re = new Regex(@"(\#.*?)$", RegexOptions.Compiled);
            static Regex text_re = new Regex(@"""(.*)""");

            static string StripTrailingComment(string s) {
                return (s.Contains('#') ?
                    (s.Contains('"') ?
                        comment_quotes_re.Replace(s, string.Empty) :
                        comment_no_quotes_re.Replace(s, string.Empty)) :
                    s).TrimEnd();
            }

            static int GetLevel(char indent, string line) {
                return line.Length - line.TrimStart(indent).Length;
            }

            static int GetIndentationUnit(char indent, string[] lines) {
                var len = 0;
                for (int i = 0; i < lines.Length; i++) {
                    len = GetLevel(indent, lines[i]);
                    if (len > 0) return len;
                }
                throw new Exception("No indentation!");
            }

            static char GetIndentCharacter(IndentChar indent_enum, string[] lines) {
                switch (indent_enum) {
                    case IndentChar.Auto:
                        return InferIndent(lines);
                    case IndentChar.Tab:
                        return TAB;
                    case IndentChar.Whitespace:
                        return WHITESPACE;
                    default:
                        throw new Exception("Invalid indent character!");
                }
            }

            static string GetCodeSnippetPlaceholderLine(int id, char indent, int min_indent, int current_level) {
                var indent_str = new string(indent, min_indent * current_level);
                return string.Format("{0}${1}", indent_str, id);
            }

            public static RawLine[] ParseVGPScriptFile(string path, ResourceManager rm, IndentChar indent_enum = IndentChar.Auto) {
                var raw_lines = File.ReadAllLines(path);

                // Get info on the indentation
                var indent = GetIndentCharacter(indent_enum, raw_lines);
                var min_indent = GetIndentationUnit(indent, raw_lines);

                // Top-level elements
                var labels = new List<string>();
                var label_lines_indices = new List<int>();

                // Code snippet
                int? multiline_snippet_i = null;
                var in_multiline_snippet = false;
                var current_level = 0;
                var snippet = string.Empty;

                // ???
                var tm = new TextManager();

                var lines = new List<RawLine>();

                for (int i = 0; i < raw_lines.Count(); i++) {
                    var r = raw_lines[i];
                    var t = r.TrimStart();

                    // Skip empty and comment lines
                    if (!in_multiline_snippet && (string.IsNullOrEmpty(t) || t[0] == COMMENT_CHAR)) continue;

                    // Check indentation
                    var indent_length = GetLevel(indent, r);
                    if (!in_multiline_snippet && indent_length % min_indent != 0)
                        throw new Exception(string.Format(
                            "Irregular indentation in '{0}' at line {1}!", path, i));

                    int level = indent_length / min_indent;
                    if (!in_multiline_snippet && level > current_level + 1)
                        throw new Exception(string.Format(
                            "Unexptected indentation in '{0}' at line {1}!", path, i));

                    if (!in_multiline_snippet)
                        current_level = level;

                    // Register label (?)
                    if (level == 0 && t.StartsWith(LABEL)) {
                        labels.Add(t.Split(WHITESPACE)[0].Trim());
                        label_lines_indices.Add(i);
                    }

                    // Add multiline snippet
                    if (in_multiline_snippet && level <= current_level) {
                        var id = rm.CodeManager.RegisterMethod(path, multiline_snippet_i.Value, snippet);

                        var c = GetCodeSnippetPlaceholderLine(id, indent, min_indent, current_level);

                        lines.Add(new RawLine(path, c, multiline_snippet_i.Value));

                        snippet = string.Empty;
                        in_multiline_snippet = false;
                        multiline_snippet_i = null;

                        continue;
                    }

                    // Check for indentation issues
                    if (!in_multiline_snippet) {

                        // Right now Python comments are preserved in the snippet
                        var tt = StripTrailingComment(t).TrimEnd();

                        var c = StripTrailingComment(r).TrimEnd();

                        var tmp = new RawLine(path, r, i);

                        if (tt == CSHARP_LINE) {

                            // Flag multiline code snippet started
                            in_multiline_snippet = true;
                            multiline_snippet_i = i;

                        } else {

                            if (t.StartsWith("$ ")) {

                                var id = rm.CodeManager.RegisterMethod(path, i, tt.Skip(1).ToString().Trim());
                                c = GetCodeSnippetPlaceholderLine(id, indent, min_indent, current_level);

                            } else if (identifier_re.IsMatch(tt) && tt != PASS && tt != RETURN) {

                                var code = string.Format("{0}();", tt);
                                var id = rm.CodeManager.RegisterMethod(path, i, code);
                                c = GetCodeSnippetPlaceholderLine(id, indent, min_indent, current_level);

                            } else if (t.StartsWith(IF) || t.StartsWith(ELIF)) {

                                // Register if or elif condition snippet
                                var m = conditional_re.Match(r);
                                if (m != null) {
                                    snippet = m.Groups[2].Value;
                                    var id = rm.CodeManager.RegisterMethod(path, i, snippet);
                                    snippet = string.Empty;
                                    c = string.Format("{0} ${1}:", m.Groups[1].Value, id);
                                }

                            } else if (tt.Contains('"')) {

                                var m = text_re.Match(tt);
                                if (m.Success) {
                                    var id = rm.TextManager.RegisterText(labels.Last(), m.Groups[1].Value);
                                    c = text_re.Replace(c, "%" + id.ToString());
                                }
                                // Throw exception otherwise?

                            }

                            // Create line object
                            lines.Add(new RawLine(path, c, i));

                        }
                        
                    } else {
                        snippet += r;
                    }
                }

                return lines.ToArray();
            }

            static Script ParseLines2(RawLine[] lines, IndentChar indent_enum = IndentChar.Auto) {
                return null;
            }

        }
    }

}