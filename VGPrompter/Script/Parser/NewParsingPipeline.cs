using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VGPrompter {

    public partial class Script {

        public static partial class Parser {

            // Folder -> Files

            // File -> Lines

            static IEnumerable<RawLine> ReadVGPSLines(string path, bool ignore_unsupported_renpy = false, IndentChar indent_enum = IndentChar.Auto) {
                var i = 0;
                char? indent_char = null;
                int? min_indent = null;
                int level = 0;
                int prev_level = 0;
                string line;

                switch (indent_enum) {
                    case IndentChar.Tab:
                        indent_char = TAB;
                        break;
                    case IndentChar.Whitespace:
                        indent_char = WHITESPACE;
                        break;
                }

                using (var fh = File.OpenText(path)) {
                    while (!fh.EndOfStream) {
                        i++;
                        prev_level = level;
                        line = fh.ReadLine().TrimEnd();

                        // Skip empty line
                        if (string.IsNullOrEmpty(line))
                            continue;

                        char first_whitespace;
                        int indent = 0;
                        if (line[0] == WHITESPACE || line[0] == TAB) {
                            first_whitespace = line[0];
                            if (indent_char.HasValue && indent_char.Value != first_whitespace)
                                throw new Exception("Inconsistentent indentation character!");
                            int j = 0;
                            while (line[j++] == first_whitespace) { }

                            if (line[j] == WHITESPACE || line[j] == TAB)
                                throw new Exception("Inconsistentent indentation character!");

                            line = line.Substring(j);

                            indent = j - 1;

                            if (min_indent.HasValue) {
                                if (indent < min_indent) {
                                    throw new Exception("Unexpected indentation!");
                                } else if (indent % min_indent != 0) {
                                    throw new Exception("Unexpected indentation: not a multiple!");
                                }
                            } else {
                                min_indent = indent;
                            }

                            level = indent / min_indent.Value;

                            if (level > prev_level && level - prev_level > 1)
                                throw new Exception("Unexpected indentation!");

                        }

                        if (line[0] == COMMENT_CHAR) {
                            // Skip comment line
                            continue;
                        }

                        // Handle in-line comments
                        if (line.Contains('#')) {
                            if (line.Contains('"')) {
                                line = comment_quotes_re.Replace(line, string.Empty);
                            } else {
                                line = comment_no_quotes_re.Replace(line, string.Empty);
                            }
                            Console.WriteLine(line);
                        }

                        // Wrap line
                        yield return new RawLine(path, line, i, level);
                    }
                }
            }

            // Lines -> Tokens

            /*static IEnumerable<Token[]> TokenizeRawLine(RawLine line) {
                yield return Tokenize(line.Text);
            }*/


            // Lines -> Tree

            static Node2 GetTree(IEnumerable<RawLine> lines) {
                var root = Node2.Root;
                int delta;
                Node2 child;

                var stack = new Stack<Node2>();
                stack.Push(root);

                foreach (var line in lines) {
                    if (line.Level < stack.Count - 2) {
                        delta = stack.Count - line.Level - 1;
                        for (int i = 0; i < delta; i++) {
                            child = stack.Pop();
                            stack.Peek().Add(child);
                        }
                    } else if (stack.Count > 1 && line.Level == stack.Count - 2) {
                        child = stack.Pop();
                        stack.Peek().Add(child);
                    }
                    stack.Push(new Node2(line));
                }
                for (int i = 0; i < stack.Count; i++) {
                    child = stack.Pop();
                    stack.Peek().Add(child);
                }

                return root;
            }

            // Tree -> VGPObjects

            static Script ParseTree(Node2 root) {
                var tm = new TextManager();
                var blocks = new List<VGPBlock>();
                var definitions = new List<VGPDefine>();

                // var blocks = root.Children.Select(ParseNode).Cast<VGPBlock>().ToList();
                foreach (var node in root.Children) {
                    switch (node.Line.Tokens[0].Type) {
                        case TokenType.Label:
                            blocks.Add((VGPBlock)ParseNode(node, null));
                            break;
                        case TokenType.Define:
                            definitions.Add((VGPDefine)DefineParserRule.Parse(node.Line.Tokens, null));
                            break;
                        default:
                            throw new Exception("Invalid top level statement!");
                    }
                }
                return blocks2script(blocks, ref tm);
            }

            static Line tokens2line2(Token[] tokens, ParserRule2[] rules, VGPBlock parent) {
                var first_token_type = tokens[0].Type;
                foreach (var rule in rules) {
                    if (rule.FirstTokenType == first_token_type)
                        return rule.Parse(tokens, parent);
                }
                return null;
            }

            static Line ParseNode(Node2 node, VGPBlock block) {
                var line = node.Line;
                var tokens = line.Tokens;

                var current_block = block ?? (VGPBlock)LabelParserRule.Parse(tokens, null);

                Line current_line = null;
                var contents = new List<Line>();
                VGPIfElse ifelse = new VGPIfElse(current_block);

                if (node.IsEmpty) {
                    // Leaf
                    current_line = tokens2line2(tokens, LeafRules2, current_block);
                } else {
                    // Block or other node
                    current_line = block == null ? current_block : tokens2line2(tokens, NodeRules2, current_block);

                    foreach (var child in node.Children) {
                        var tmp = ParseNode(child, current_block);
                        if (tmp == null) throw new Exception(string.Format("Null child ILine in {0}!", node.Line.ExceptionString));

                        if (tmp is Conditional) {
                            ifelse.AddCondition(tmp as Conditional);
                        } else {
                            // Add previous IfElse block
                            if (!ifelse.IsEmpty)
                                AddIfElse(ref ifelse, ref contents);
                            contents.Add(tmp);
                        }
                    }

                    // Add previous IfElse block
                    if (!ifelse.IsEmpty)
                        AddIfElse(ref ifelse, ref contents);

                    if (current_line is VGPMenu) {
                        (current_line as VGPMenu).Contents = contents.Select(x => x as VGPChoice).ToList();
                    } else if (current_line is VGPIfElse) {
                        (current_line as VGPIfElse).Contents = contents.Select(x => x as Conditional).ToList();
                    } else if (current_line is IterableContainer) {
                        (current_line as IterableContainer).Contents = contents;
                    } else {
                        throw new Exception(string.Format("Unexpected ILine container in {0}!", node.Line.ExceptionString));
                    }
                }
                

                // current_line = tokens2line2(tokens, node.IsEmpty ? NodeRules2 : NodeRules2, parent_block);
                
                return current_line;
            }

            /*static List<VGPBlock> ParseVGPFile(string path) {
                var blocks = new List<VGPBlock>();
                Token[] tokens;

                VGPBlock current_block = null;
                VGPIfElse current_ifelse = null;
                Line current_line = null;

                // Context flags
                int? current_menu_level = null;

                var definitions = new List<VGPDefine>();

                foreach (var line in ReadVGPSLines(path)) {
                    tokens = Tokenize(line.Text);

                    if (line.Level <= current_menu_level) {
                        current_menu_level = null;
                    }

                    switch (line.Level) {
                        case 0:
                            switch (tokens[0].Type) {
                                case TokenType.Label:
                                    if (current_block != null) {
                                        blocks.Add(current_block);
                                    }
                                    current_block = (VGPBlock)LabelParserRule.Parse(tokens, null);
                                    break;
                                case TokenType.Define:
                                    definitions.Add((VGPDefine)DefineParserRule.Parse(tokens, null));
                                    break;
                                default:
                                    throw new Exception("Invalid top level statement!");
                            }
                            break;
                        default:
                            switch (tokens[tokens.Length - 1].Type) {
                                case TokenType.Colon:
                                    current_line = tokens2line2(tokens, NodeRules2, current_block);
                                    if (current_line is VGPMenu) {
                                        current_menu_level = line.Level;
                                    }
                                    break;
                                default:
                                    // tokens2line2(tokens, LeafRules2, current_block);
                                    break;
                            }
                            break;
                    }
                }

                return blocks;
            }*/


        }

    }

}