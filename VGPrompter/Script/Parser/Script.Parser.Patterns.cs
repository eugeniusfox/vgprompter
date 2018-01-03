using System.Text.RegularExpressions;

namespace VGPrompter {

    public partial class Script {

        public static partial class Parser {

            const char
                WHITESPACE = ' ',
                FLOAT_SUFFIX = 'f',
                TAB = '\t',
                QUOTE = '"',
                SINGLE_QUOTE = '\'',
                COLON = ':',
                COMMENT_CHAR = '#',
                UNDERSCORE = '_',
                RENPY_PYLINE_CHAR = '$',
                COMMA_C = ',',
                EQUAL_C = '=';

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

            static readonly char[] COMMA_SPLIT = { ',' };

            static readonly string[]
                UNSUPPORTED_RENPY_KEYWORDS = {
                    WITH, SHOW, HIDE, PLAY, STOP, SCENE, IMAGE, PAUSE
                },

                UNSUPPORTED_RENPY_BLOCK_KEYWORDS = {
                    INIT, PYTHON, INIT_PYTHON
                };

            const string
                // IDENTIFIER = @"[a-zA-Z_]\w*",
                IDENTIFIER = @"[A-z][A-z0-9_]*",
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

            static Regex inline_comment_re = new Regex(@"(.*"".*""|.*)\s+#.*$", RegexOptions.Compiled);
            static Regex comment_quotes_re = new Regex(@"(?<=(?:"".*?"").*?)\#.*?$", RegexOptions.Compiled);
            static Regex comment_no_quotes_re = new Regex(@"(\#.*?)$", RegexOptions.Compiled);

            static Regex function_call_re = new Regex(FUNCTION_CALL, RegexOptions.Compiled);
            static Regex function_call_line_re = new Regex(string.Format("^{0}$", FUNCTION_CALL), RegexOptions.Compiled);

            // Legacy regular expressions
            static Regex unsupported_renpy_re = new Regex(string.Format(@"^({0}) \w+", string.Join(PIPE, UNSUPPORTED_RENPY_KEYWORDS)), RegexOptions.Compiled);
            static Regex unsupported_renpy_block_re = new Regex(string.Format(@"^({0}) ?.*:$", string.Join(PIPE, UNSUPPORTED_RENPY_BLOCK_KEYWORDS)), RegexOptions.Compiled);

            static Regex
                identifier_re = new Regex(string.Format("^{0}$", IDENTIFIER), RegexOptions.Compiled),
                literal_boolean_re = new Regex(@"^(?:True|False)$", RegexOptions.Compiled),
                literal_float_re = new Regex(@"^\d+(?:\.\d+)?f$", RegexOptions.Compiled),
                literal_double_re = new Regex(@"^\d+\.\d+$", RegexOptions.Compiled),
                literal_int_re = new Regex(@"^\d+$", RegexOptions.Compiled),
                literal_string_re = new Regex(@"^(?:"".*""|'.*')$", RegexOptions.Compiled);

        }

    }

}