using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VGPrompter.Parser.ParsedLines;

namespace VGPrompter.Parser {
    public class VGPScriptParser {

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
            CSHARP = "cs",

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

            DOLLAR = "$",
            PIPE = "|",
            EQUAL = "=",
            SCRIPT = "script",

            STRING_PLACEHOLDER = "%s";

        enum ParseContext {
            TopLevel,
            InLabelBlock,
            InMenuBlock,
            InMultiLineSnippetBlock
        }

        enum SnippetType {
            SingleLineAction,
            MultiLineAction,
            Condition
        }

        string ParserErrorMsg(string msg, string src_file, int line_number, string raw_line) =>
            string.Format("[VGPParser] {0} in file '{1}' at line line {2}: '{3}'!", msg, src_file, line_number, raw_line);

        void CheckNoComma(string src_file, int line_number, string raw_text, bool has_trailing_colon) {
            if (!has_trailing_colon)
                throw new Exception(ParserErrorMsg(
                    "Missing trailing colon", src_file, line_number, raw_text));
        }

        ParsedLine ParseRawLine(string src_file, string s, int i, ref ParseContext context, ref int ContextIndentLevel) {
            // Remove trailing comment
            var s_tr = s.TrimEnd();

            // Exit from multi-line context on dedent...

            // Exit from menu context on dedent or indent to choice (?)

            // Check valid ID's as tokens... /[A-z][A-z0-9]*/
            // Keywords are always lowercase...

            var CurrentLevel = 0;

            if (context == ParseContext.InMultiLineSnippetBlock) {

                return new MultiLineSnippetLine(src_file, i, s_tr, s_tr);

            } else {

                var has_trailing_colon = s_tr[s_tr.Length - 1] == ':';

                if (has_trailing_colon) {
                    s_tr = s_tr.Substring(0, s_tr.Length - 1).TrimEnd();
                }

                var text = string.Empty;

                // Extract string literals
                if (s_tr.Contains('"')) {
                    text = "...";
                }

                var s_t = s_tr.TrimStart();
                var indent = s_tr.Length - s_t.Length;
                var tokens = s_t.Split(' ');
                var first_token = tokens[0];

                if (context == ParseContext.TopLevel && !(first_token == LABEL || first_token == DEFINE))
                    throw new Exception(ParserErrorMsg(
                        "Invalid top-level statement",
                            src_file, i, s_t));

                var snippet = string.Empty;
                var target = string.Empty;


                // Trailing colon checks

                /*if (!has_trailing_colon  && (
                    first_token == IF    || first_token == ELIF   || first_token == ELSE
                    first_token == MENU  || first_token == CSHARP || first_token == LABEL))
                    throw new Exception(ParserErrorMsg(
                        "Missing trailing colon",
                            src_file, i, s_t));
                
                else if (has_trailing_colon && (
                    first_token == JUMP || first_token == CALL))
                    throw new Exception(ParserErrorMsg(
                        "Unexpected trailing colon",
                            src_file, i, s_t));*/

                // Unknown first token with text literal ->
                // dialogue line if context is InLabel, choice if in InMenu -->
                // either wants or does not want the trailing colon!


                // First token check

                switch (first_token) {

                    case IF:
                        // Trailing colon check
                        if (!has_trailing_colon)
                            throw new Exception(ParserErrorMsg(
                                "Missing trailing colon", src_file, i, s_t));

                        snippet = s_t.Substring(first_token.Length, s_t.Length - first_token.Length).Trim();

                        // Snippet length check
                        if (string.IsNullOrEmpty(snippet))
                            throw new Exception(ParserErrorMsg(
                                "Empty conditional expression", src_file, i, s_t));

                        return new If(src_file, i, s_tr, snippet);

                    case ELIF:
                        // Trailing colon check
                        if (!has_trailing_colon)
                            throw new Exception(ParserErrorMsg(
                                "Missing trailing colon", src_file, i, s_t));

                        snippet = s_t.Substring(first_token.Length, s_t.Length - first_token.Length).Trim();

                        // Snippet length check
                        if (string.IsNullOrEmpty(snippet))
                            throw new Exception(ParserErrorMsg(
                                "Empty conditional expression", src_file, i, s_t));

                        return new ElIf(src_file, i, s_tr, snippet);

                    case WHILE:
                        // Trailing colon check
                        if (!has_trailing_colon)
                            throw new Exception(ParserErrorMsg(
                                "Missing trailing colon", src_file, i, s_t));

                        snippet = s_t.Substring(first_token.Length, s_t.Length - first_token.Length).Trim();

                        // Snippet length check
                        if (string.IsNullOrEmpty(snippet))
                            throw new Exception(ParserErrorMsg(
                                "Empty conditional expression", src_file, i, s_t));

                        return new While(src_file, i, s_tr, snippet);

                    case ELSE:
                        // Trailing colon check
                        if (!has_trailing_colon)
                            throw new Exception(ParserErrorMsg(
                                "Missing trailing colon", src_file, i, s_t));

                        // Token count check
                        if (tokens.Length > 1)
                            throw new Exception(ParserErrorMsg(string.Format(
                                "Unexpected characters after keyword '{0}'", first_token), src_file, i, s_t));

                        return new Else(src_file, i, s_tr);

                    case MENU:
                        // Trailing colon check
                        if (!has_trailing_colon)
                            throw new Exception(ParserErrorMsg(
                                "Missing trailing colon", src_file, i, s_t));

                        // Token count check
                        if (tokens.Length > 1)
                            throw new Exception(ParserErrorMsg(string.Format(
                                "Unexpected characters after keyword '{0}'", first_token), src_file, i, s_t));

                        var duration = 0;

                        return new Menu(src_file, i, s_tr, duration);

                    case JUMP:
                        // Trailing colon check
                        if (has_trailing_colon)
                            throw new Exception(ParserErrorMsg(
                                "Unexpected trailing colon", src_file, i, s_t));

                        target = tokens[1];

                        return new Jump(src_file, i, s_tr, target);

                    case CALL:
                        // Trailing colon check
                        if (has_trailing_colon)
                            throw new Exception(ParserErrorMsg(
                                "Unexpected trailing colon", src_file, i, s_t));

                        target = tokens[1];

                        return new Call(src_file, i, s_tr, target);

                    case CSHARP:
                        // Trailing colon check
                        if (!has_trailing_colon)
                            throw new Exception(ParserErrorMsg(
                                "Missing trailing colon", src_file, i, s_t));
                        context = ParseContext.InMultiLineSnippetBlock;
                        return new MultiLineSnippetLine(src_file, i, s_t, s_tr);

                    case DOLLAR:
                        snippet = s_t.Substring(1, s_t.Length - 1).Trim();
                        return new SingleLineSnippet(src_file, i, s_t, snippet);

                    default:
                        // Dialogue line or choice

                        if (first_token == STRING_PLACEHOLDER) {

                            // Anonymous dialogue line or choice
                            if (context == ParseContext.InMenuBlock && CurrentLevel == ContextIndentLevel + 1) {
                                return new AnonymousDialogueLine(src_file, i, s_tr, text);
                            } else {
                                return new AnonymousDialogueLine(src_file, i, s_tr, text);
                            }

                        }

                        if (context == ParseContext.InMenuBlock && CurrentLevel == ContextIndentLevel + 1) {

                            // Choice (named)

                            // Trailing colon check
                            if (!has_trailing_colon)
                                throw new Exception(ParserErrorMsg(
                                    "Missing trailing colon", src_file, i, s_t));

                            // Optional trailing condition check
                            // ...
                            // snippet = tokens[3-4].Trim();

                            return new Choice(src_file, i, s_t, text, first_token);


                        } else {

                            // Dialogue line

                            // Trailing colon check
                            if (has_trailing_colon)
                                throw new Exception(ParserErrorMsg(
                                    "Unexpected trailing colon", src_file, i, s_t));

                            return new DialogueLine(src_file, i, s_t, text, first_token);

                        }

                }

            }

        }

    }
}
