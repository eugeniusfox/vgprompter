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
                var ppscript = LoadRawLines2(path, recursive);
                return ParseLines2(ppscript, indent);
            }

            public static PPVGPScript LoadRawLines2(string path, bool recursive = false) {

                var script = new PPVGPScript();

                if (Directory.Exists(path)) {

                    var files = Utils.GetScriptFiles(path, recursive);
                    foreach (var f in files) {
                        script.ParseFile(f);
                    }

                } else if (File.Exists(path)) {

                    script.ParseFile(path);

                } else {

                    throw new Exception("Missing source file or directory!");

                }

                return script;

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
            static Regex trailing_if_re = new Regex(@"if ( *.+):$");
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

            static Script ParseLines2(PPVGPScript raw_script, IndentChar indent_enum = IndentChar.Auto) {
                return null;
            }

        }
    }

}