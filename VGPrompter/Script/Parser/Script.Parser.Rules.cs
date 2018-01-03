using System;
using System.Collections.Generic;

namespace VGPrompter {

    public partial class Script {

        public static partial class Parser {

            struct ParserRule2 {
                public TokenType FirstTokenType { get; private set; }
                public int MinTokenNumber { get; private set; }
                public int MaxTokenNumber { get; private set; }
                public bool TrailingComma { get; private set; }
                public Func<Token[], VGPBlock, Line> Constructor { get; private set; }
                public Action<Token[]> Validator { get; private set; }

                public ParserRule2(
                    TokenType first_token_type,
                    int min_tokens,
                    int max_tokens,
                    bool trailing_comma,
                    Func<Token[], VGPBlock, Line> constructor,
                    Action<Token[]> validator = null) : this() {
                    FirstTokenType = first_token_type;
                    MinTokenNumber = min_tokens;
                    MaxTokenNumber = max_tokens;
                    TrailingComma = trailing_comma;
                    Constructor = constructor;
                    Validator = validator;
                }

                public Line Parse(Token[] tokens, VGPBlock parent) {
                    if (tokens[0].Type != FirstTokenType)
                        throw new Exception("The first token is of the wrong type!");
                    if (TrailingComma && tokens[tokens.Length - 1].Type != TokenType.Comma)
                        throw new Exception("Missing trailing comma!");
                    if (tokens.Length < MinTokenNumber || tokens.Length > MaxTokenNumber)
                        throw new Exception("Unexpected number of tokens!");
                    Validator?.Invoke(tokens);
                    return Constructor(tokens, parent);
                }
            }

            static ParserRule2[] NodeRules2 = new ParserRule2[] {

                // Menu
                new ParserRule2(
                    TokenType.Menu, 2, 3, true,
                    (tokens, parent) => new VGPMenu(parent),
                    (tokens) => {
                        if (tokens.Length == 3 && tokens[1].Type != TokenType.ArgumentList)
                            throw new Exception("Argument list expected!");
                    }),

                // Else
                new ParserRule2(
                    TokenType.Else, 2, 2, true,
                    (tokens, parent) => new Conditional.Else(parent)),

                // If
                new ParserRule2(
                    TokenType.If, 3, 4, true,
                    (tokens, parent) => new Conditional.If(tokens[1].Value.ToString(), parent),
                    (tokens) => {
                        if (tokens.Length == 4 && tokens[1].Type != TokenType.ArgumentList)
                            throw new Exception("Argument list expected!");
                    }),

                // ElIf
                new ParserRule2(
                    TokenType.ElIf, 3, 4, true,
                    (tokens, parent) => new Conditional.ElseIf(tokens[1].Value.ToString(), parent),
                    (tokens) => {
                        if (tokens.Length == 4 && tokens[1].Type != TokenType.ArgumentList)
                            throw new Exception("Argument list expected!");
                    }),

                // While
                new ParserRule2(
                    TokenType.While, 3, 4, true,
                    (tokens, parent) => new VGPWhile(tokens[1].Value.ToString(), parent),
                    (tokens) => {
                        if (tokens.Length == 4 && tokens[1].Type != TokenType.ArgumentList)
                            throw new Exception("Argument list expected!");
                    })
            };

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

            static ParserRule[] TopLevelRules = new ParserRule[] {
                new ParserRule( LABEL,        (tokens, parent) => new VGPBlock(tokens[1].Substring(0, tokens[1].Length - 1)), 2,
                                              (tokens)         => tokens[1][tokens[1].Length - 1] == COLON)
            };

            static ParserRule[] LeafRules = new ParserRule[] {
                new ParserRule( PASS,         (tokens, parent) => new VGPPass(), 1),
                new ParserRule( RETURN,       (tokens, parent) => new VGPReturn(), 1),
                new ParserRule( JUMP,         (tokens, parent) => new VGPGoTo(tokens[1], is_call: false), 2),
                new ParserRule( CALL,         (tokens, parent) => new VGPGoTo(tokens[1], is_call: true), 2)
            };

            static ParserRule[] NodeRules = new ParserRule[] {
                new ParserRule( MENU,         (tokens, parent) => new VGPMenu(parent, tokens.Length == 2 ? (int?)int.Parse(tokens[1]) : null), null,
                                              (tokens)         => tokens.Length == 1 || (tokens.Length == 2 && IsInteger(tokens[1]))),

                new ParserRule( IF,           (tokens, parent) => new Conditional.If(tokens[1], parent), 2),
                new ParserRule( ELIF,         (tokens, parent) => new Conditional.ElseIf(tokens[1], parent), 2),
                new ParserRule( ELSE,         (tokens, parent) => new Conditional.Else(parent), 1),
                new ParserRule( WHILE,        (tokens, parent) => new VGPWhile(tokens[1], parent), 2)
            };
        }

    }

}