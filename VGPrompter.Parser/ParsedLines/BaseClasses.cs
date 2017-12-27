using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VGPrompter.Parser.ParsedLines {

    interface ISnippet {
        string Snippet { get; }
    }

    interface IText {
        string Text { get; }
    }

    interface ICharacterTag {
        string CharacterTag { get; }
    }

    interface IContainer { }

    interface IChoice : IContainer { }

    abstract class ParsedLine {
        public string SourceFilePath { get; set; }
        public int LineNumber { get; set; }
        public string RawText { get; set; }

        protected ParsedLine(string src_file_path, int line_number, string raw_text) {
            SourceFilePath = src_file_path;
            LineNumber = line_number;
            RawText = raw_text;
        }
    }

    abstract class LineWithSnippet : ParsedLine, ISnippet {
        public string Snippet { get; private set; }

        protected LineWithSnippet(string src_file_path, int line_number, string raw_text, string snippet)
            : base(src_file_path, line_number, raw_text) {
            Snippet = snippet;
        }
    }

    abstract class LineWithText : ParsedLine, IText {

        public string Text { get; private set; }

        protected LineWithText(string src_file_path, int line_number, string raw_text, string text)
            : base(src_file_path, line_number, raw_text) {
            Text = text;
        }

    }

    abstract class LineWithCharacter : LineWithText, IText, ICharacterTag {

        public string CharacterTag { get; private set; }

        protected LineWithCharacter(string src_file_path, int line_number, string raw_text, string text, string character_tag)
            : base(src_file_path, line_number, raw_text, text) {
            CharacterTag = character_tag;
        }

    }

    abstract class GoTo : ParsedLine {

        public string Target { get; protected set; }

        protected GoTo(string src_file_path, int line_number, string raw_text, string target)
            : base(src_file_path, line_number, raw_text) {
            Target = target;
        }
    }

}
