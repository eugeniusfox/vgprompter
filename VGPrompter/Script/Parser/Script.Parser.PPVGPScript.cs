using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VGPrompter {

    public partial class Script {

        public static partial class Parser {

            public class PPVGPScript {

                public Dictionary<string, List<RawLine>> Contents { get; private set; }

                public IEnumerable<string> Labels => Contents.Keys;

                public ResourceManager ResourceManager { get; private set; }

                public PPVGPScript() {
                    Contents = new Dictionary<string, List<RawLine>>();
                    ResourceManager = new ResourceManager();
                }

                public void ParseFile(string path, IndentChar indent_enum = IndentChar.Auto) {
                    var raw_lines = File.ReadAllLines(path);

                    // Get info on the indentation
                    var indent = GetIndentCharacter(indent_enum, raw_lines);
                    var min_indent = GetIndentationUnit(indent, raw_lines);

                    // Code snippet
                    int? multiline_snippet_i = null;
                    var in_multiline_snippet = false;
                    var current_level = 0;

                    var current_label = string.Empty;
                    var snippet = string.Empty;

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

                        // Register label
                        if (level == 0 && t.StartsWith(LABEL)) {
                            var label_raw = t.Split(WHITESPACE)[1].Trim();
                            current_label = label_raw.Substring(0, label_raw.Length - 1);

                            if (Contents.ContainsKey(current_label))
                                throw new Exception(string.Format(
                                    "Duplicate label '{0}' in '{1}' at line {2}!", path, i));

                            Contents.Add(current_label, new List<RawLine>());
                        }

                        // Add multiline snippet
                        if (in_multiline_snippet && level <= current_level) {
                            var id = ResourceManager.CodeManager.RegisterMethod(path, multiline_snippet_i.Value, snippet);

                            var c = GetCodeSnippetPlaceholderLine(id, indent, min_indent, current_level);

                            Contents[current_label].Add(new RawLine(path, c, multiline_snippet_i.Value));

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

                                    var id = ResourceManager.CodeManager.RegisterMethod(path, i, tt.Skip(1).ToString().Trim());
                                    c = GetCodeSnippetPlaceholderLine(id, indent, min_indent, current_level);

                                } else if (identifier_re.IsMatch(tt) && tt != PASS && tt != RETURN) {

                                    var code = string.Format("{0}();", tt);
                                    var id = ResourceManager.CodeManager.RegisterMethod(path, i, code);
                                    c = GetCodeSnippetPlaceholderLine(id, indent, min_indent, current_level);

                                } else if (t.StartsWith(IF) || t.StartsWith(ELIF)) {

                                    // Register if or elif condition snippet
                                    var m = conditional_re.Match(r);
                                    if (m != null) {
                                        snippet = m.Groups[2].Value;
                                        var id = ResourceManager.CodeManager.RegisterMethod(path, i, snippet);
                                        snippet = string.Empty;
                                        c = string.Format("{0} ${1}:", m.Groups[1].Value, id);
                                    }

                                } else if (tt.Contains('"')) {

                                    var m = text_re.Match(tt);
                                    if (m.Success) {
                                        var id = ResourceManager.TextManager.RegisterText(current_label, m.Groups[1].Value);
                                        c = text_re.Replace(c, "%" + id.ToString());
                                    }
                                    // Throw exception otherwise?

                                    m = trailing_if_re.Match(tt);
                                    if (m.Success) {
                                        var code = m.Groups[1].Value;
                                        var id = ResourceManager.CodeManager.RegisterMethod(path, i, code);
                                        c = trailing_if_re.Replace(c, string.Format("if ${0}:", id));
                                    }

                                }

                                // Create line object
                                Contents[current_label].Add(new RawLine(path, c, i));

                            }

                        } else {
                            snippet += r;
                        }

                    }

                    

                }

            }

        }

    }

}