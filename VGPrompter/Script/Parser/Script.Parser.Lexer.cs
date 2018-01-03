using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace VGPrompter {

    public partial class Script {

        public static partial class Parser {

            const string
                ARGS_PH = "%a",
                STR_PH = "%s";

            static readonly string
                ARGS_PH_W = WHITESPACE + ARGS_PH + WHITESPACE,
                STR_PH_W = WHITESPACE + STR_PH + WHITESPACE;

            static readonly Regex
                ARGS_RE = new Regex(@"\((.*)\)", RegexOptions.Compiled),
                STRING_RE = new Regex(@"""(.*)""", RegexOptions.Compiled);

            enum TokenType {
                Text,
                ArgumentList,
                Identifier,
                StringLiteral,
                IntegerLiteral,
                FloatLiteral,
                DoubleLiteral,
                BooleanLiteral,
                Equal,
                Colon,
                If,
                ElIf,
                Else,
                While,
                Jump,
                Call,
                Label,
                Return,
                Define,
                Menu
            }

            struct Token {
                public TokenType Type { get; private set; }
                public object Value { get; private set; }

                public Token(TokenType type, object value = null) : this() {
                    Type = type;
                    Value = value;
                }
            }

            static TokenType GetTokenType(string s) {
                switch (s) {
                    case ARGS_PH:
                        return TokenType.ArgumentList;
                    case STR_PH:
                        return TokenType.Text;
                    case IF:
                        return TokenType.If;
                    case ELIF:
                        return TokenType.ElIf;
                    case ELSE:
                        return TokenType.Else;
                    case WHILE:
                        return TokenType.While;
                    case JUMP:
                        return TokenType.Jump;
                    case CALL:
                        return TokenType.Call;
                    case LABEL:
                        return TokenType.Label;
                    case RETURN:
                        return TokenType.Return;
                    case DEFINE:
                        return TokenType.Define;
                    case MENU:
                        return TokenType.Menu;
                    case EQUAL:
                        return TokenType.Equal;
                    default:
                        if (identifier_re.Match(s).Success) {
                            return TokenType.Identifier;
                        } else if (literal_string_re.Match(s).Success) {
                            return TokenType.StringLiteral;
                        } else if (literal_int_re.Match(s).Success) {
                            return TokenType.IntegerLiteral;
                        } else if (literal_boolean_re.Match(s).Success) {
                            return TokenType.BooleanLiteral;
                        } else if (literal_float_re.Match(s).Success) {
                            return TokenType.FloatLiteral;
                        } else if (literal_double_re.Match(s).Success) {
                            return TokenType.DoubleLiteral;
                        } else {
                            throw new Exception(string.Format("Invalid token '{0}'", s));
                        }
                }
            }

            static Token[] Tokenize(string line) {

                var s = line.Trim();

                var trailing_comma = s[s.Length - 1] == COLON;
                if (trailing_comma) {
                    s = s.Substring(0, s.Length - 1);
                }

                var m = ARGS_RE.Match(s);
                Arguments? args = null;
                if (m.Success) {
                    args = Arguments.Parse(m.Groups[1].Value);
                    s = ARGS_RE.Replace(s, ARGS_PH_W);
                }

                m = STRING_RE.Match(s);
                var text = string.Empty;
                if (m.Success) {
                    text = m.Groups[1].Value;
                    s = STRING_RE.Replace(s, STR_PH_W);
                }

                var tokens = s.Split(WHITESPACE)
                    .Where(x => !string.IsNullOrEmpty(x));

                var ptokens = new List<Token>();

                foreach (var t in tokens) {
                    var type = GetTokenType(t);
                    switch (type) {
                        case TokenType.Text:
                            ptokens.Add(new Token(type, text));
                            break;
                        case TokenType.ArgumentList:
                            ptokens.Add(new Token(type, args));
                            break;
                        case TokenType.Identifier:
                            ptokens.Add(new Token(type, t));
                            break;
                        default:
                            ptokens.Add(new Token(type));
                            break;
                    }
                }

                if (trailing_comma) {
                    ptokens.Add(new Token(TokenType.Colon));
                }

                return ptokens.ToArray();
            }

        }

    }

}
