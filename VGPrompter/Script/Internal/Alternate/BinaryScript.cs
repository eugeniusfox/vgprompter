using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace VGPrompter {

    public class BinaryScript {

        public enum LineType {
            While,
            If,
            ElseIf,
            Else,
            Action,
            Jump,
            Call,
            Menu,
            Choice,
            Dialogue,
            Return,
            Label
        }

        public const char
            COMMA = ';',
            WHITESPACE = ' ';

        public static readonly char[]
            SPLIT_TEXT = { COMMA },
            SPLIT_WHITESPACE = { WHITESPACE };

        static readonly int _span = 12;  // B

        readonly int _logic_length;
        readonly byte[] _logic;
        readonly IEnumerable<string> _text;
        readonly Func<bool>[] _conditions;
        readonly Action[] _actions;

        public int CurrentLine { get; private set; }

        public int? CurrentChoice { get; set; }

        T[] dict2array<T, K>(Dictionary<K, T> d) {
            return d
                .OrderBy(x => x.Key)
                .Select(x => x.Value)
                .ToArray();
        }

        public BinaryScript(byte[] logic, IEnumerable<string> text, Dictionary<string, Func<bool>> conditions, Dictionary<string, Action> actions) {
            _logic = logic;
            _text = text;
            _logic_length = _logic.Length / _span;
            _conditions = dict2array(conditions);
            _actions = dict2array(actions);

            _call_stack = new Stack<InstructionLine>();
        }

        public struct TextLine {
            public string[] Tags { get; private set; }
            public string Text { get; private set; }

            public TextLine(string text, string[] tags = null) : this() {
                Text = text;
                Tags = tags ?? new string[] { };
            }

            public string FirstTag {
                get { return Tags.Length > 0 ? Tags[0] : null; }
            }

            public static TextLine FromRaw(string raw_string) {
                var split = raw_string.Split(SPLIT_TEXT, 1);
                return new TextLine(split[1], split[0].Split(SPLIT_WHITESPACE));
            }
        }

        public struct InstructionLine {

            public LineType Type { get; private set; }

            public int Index { get; private set; }
            public int TargetIndex { get; private set; }
            public int TextIndex { get; private set; }

            public Func<bool> Condition { get; private set; }
            public Action Action { get; private set; }
            

            public InstructionLine(int index, LineType type, uint target, uint line_number, Func<bool> condition = null, Action action = null) : this() {
                Index = index;
                Type = type;
                TargetIndex = (int)target;
                TextIndex = (int)line_number;

                Condition = condition;
                Action = action;
            }

            public bool IsTrue {
                get { return Condition == null || Condition(); }
            }

            public int Count {
                get { return TargetIndex - Index + 1; }  //?
            }

            public int NextIndex() {
                if (Type == LineType.While || Type == LineType.If || Type == LineType.ElseIf) {
                    return IsTrue ? Index + 1 : TargetIndex;
                } else {
                    return TargetIndex;
                }
            }
        }

        Stack<InstructionLine> _call_stack;
        //Stack<int> _while_stack;

        int GetNextIndex(InstructionLine line) {
            uint i = 0;
            var type = line.Type;

            

            return (int)i;
        }

        TextLine GetTextLine(int i) {
            return TextLine.FromRaw(_text.ElementAt(i));
        }

        public IEnumerator GetEnumerator() {

            bool menu_mode = false;
            var choices = new List<int>();
            Script.Choice current_choice = null;

            int i = 0;

            using (var stream = new MemoryStream(_logic)) {
                using (var br = new BinaryReader(stream)) {

                    while (i < _logic_length) {

                        var type = (LineType)br.ReadUInt16();
                        var method = br.ReadUInt16();
                        var target = br.ReadUInt32();
                        var line_number = br.ReadUInt32();

                        var condition =
                            type == LineType.While  ||
                            type == LineType.If     ||
                            type == LineType.ElseIf ||
                            type == LineType.Choice ?
                            _conditions[method] : null;

                        var action =
                            type == LineType.Action ?
                            _actions[method] : null;

                        var line = new InstructionLine(i, type, target, line_number, condition, action);

                        if (line.Type == LineType.Return || line.Type == LineType.Label) {

                            if (_call_stack.Count > 0) {
                                i = _call_stack.Pop().Index + 1;
                            } else {
                                break;
                            }

                        } else {

                            if (menu_mode) {

                                if (line.Type == LineType.Choice) {

                                    choices.Add(i);
                                    i = line.NextIndex();

                                } else {

                                    if (choices.Count == 0)
                                        throw new Exception("Not a choice!");

                                    var choice_objects = new List<Script.Choice>();
                                    for (int j = 0; j < choices.Count; j++) {
                                        var tline = GetTextLine(line.TextIndex);
                                        choice_objects.Add(
                                            new Script.Choice(j, tline.Text, line.IsTrue, tline.FirstTag));
                                    }

                                    // RETURN MENU
                                    yield return new Script.Menu(choice_objects);

                                    if (!CurrentChoice.HasValue)
                                        throw new Exception("No choice selected!");

                                    menu_mode = false;
                                    current_choice = choice_objects[CurrentChoice.Value];

                                    i = choices[CurrentChoice.Value];

                                }


                            } else {

                                switch (line.Type) {

                                    case LineType.Action:

                                        line.Action();
                                        break;

                                    case LineType.Choice:

                                        yield return current_choice;
                                        current_choice = null;
                                        break;

                                    case LineType.Dialogue:

                                        var tline = GetTextLine(line.TextIndex);
                                        yield return new Script.DialogueLine(tline.Text, tline.FirstTag);
                                        break;

                                    case LineType.Call:

                                        _call_stack.Push(line);
                                        break;

                                }

                                i = line.NextIndex();

                            }
                        }

                    }
                }

            }
        }

    }

    /*

    public partial class Script {


        public abstract class SILine {

            public static readonly int ByteSpan = 8;

            public LineType Type;
            public int Method;
            public int Target;

            protected byte[] ToBinary(LineType type, ushort method, uint target) {
                using (var stream = new MemoryStream()) {
                    using (var bw = new BinaryWriter(stream)) {
                        bw.Write((ushort)type);
                        bw.Write(method);
                        bw.Write(target);
                    }
                    return stream.ToArray();
                }
            }

            protected static SILine FromBinary(byte[] s) {
                using (var stream = new MemoryStream(s)) {
                    using (var br = new BinaryReader(stream)) {
                        var type = (LineType)br.ReadUInt16();
                        var method = br.ReadInt32();
                        var target = br.ReadInt32();

                        switch (type) {
                            case LineType.While:
                                return new While(method, target);
                            case LineType.If:
                                return new SConditional.If(method, target);
                            case LineType.ElseIf:
                                return new SConditional.ElseIf(method, target);
                            case LineType.Else:
                                return new SConditional.Else(target);
                            default:
                                throw new Exception("Unrecognized line type!");
                        }
                    }
                }
            }

        }

        public enum LineType {
            While,
            If,
            ElseIf,
            Else,
            Action,
            Jump,
            Call
        }

        public abstract class SConditionalLine : SILine {
            public int Condition { get; private set; }
            public int Target { get; private set; }

            public SConditionalLine(int condition, int target) {
                Condition = condition;
                Target = target;
            }
        }

        public interface SText {
            string Text { get; }
        }

        public struct SBinLine {
            public LineType Type { get; }

            public void GetObjectData(SerializationInfo info, StreamingContext context) {
                throw new NotImplementedException();
            }
        }


        public class While : SConditionalLine {

            public readonly LineType Type = LineType.While;

            public While(int condition, int target)
                : base(condition, target) { }

            public byte[] AsBinary {
                get {
                    return ToBinary(Type, Condition, 0, 0);
                }
            }

        }

        public abstract class SConditional {

            public class If : SConditionalLine {
                public If(int condition, int target)
                    : base(condition, target) { }
            }

            public class ElseIf : SConditionalLine {
                public ElseIf(int condition, int target)
                    : base(condition, target) { }
            }

            public class Else : SConditionalLine {
                public Else(int target)
                    : base(0, target) { }
            }

        }


        public class IfElse : SILine {

        }
    }
    */

}