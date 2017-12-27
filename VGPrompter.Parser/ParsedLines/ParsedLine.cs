namespace VGPrompter.Parser.ParsedLines {

    // Dummy lines

    class Pass : ParsedLine {
        public Pass(string src_file_path, int line_number, string raw_text)
            : base(src_file_path, line_number, raw_text) {
        }
    }

    class Else : ParsedLine, IContainer {
        public Else(string src_file_path, int line_number, string raw_text)
            : base(src_file_path, line_number, raw_text) {
        }
    }

    class CSharp : ParsedLine, IContainer {
        public CSharp(string src_file_path, int line_number, string raw_text)
            : base(src_file_path, line_number, raw_text) {
        }
    }

    class Menu : ParsedLine, IContainer {

        public int Duration { get; private set; }

        public Menu(string src_file_path, int line_number, string raw_text, int duration)
            : base(src_file_path, line_number, raw_text) {
            Duration = duration;
        }

    }

    // Go-to lines

    class Jump : GoTo {

        public Jump(string src_file_path, int line_number, string raw_text, string target)
            : base(src_file_path, line_number, raw_text, target) {
        }

    }

    class Call : GoTo {

        public Call(string src_file_path, int line_number, string raw_text, string target)
            : base(src_file_path, line_number, raw_text, target) {
        }

    }

    // Lines with logic (base)

    class If : LineWithSnippet, IContainer {

        public If(string src_file_path, int line_number, string raw_text, string snippet)
            : base(src_file_path, line_number, raw_text, snippet) {
        }

    }

    class ElIf : LineWithSnippet, IContainer {

        public ElIf(string src_file_path, int line_number, string raw_text, string snippet)
            : base(src_file_path, line_number, raw_text, snippet) {
        }

    }

    class While : LineWithSnippet, IContainer {

        public While(string src_file_path, int line_number, string raw_text, string snippet)
            : base(src_file_path, line_number, raw_text, snippet) {
        }

    }

    class SingleLineSnippet : LineWithSnippet {

        public SingleLineSnippet(string src_file_path, int line_number, string raw_text, string snippet)
            : base(src_file_path, line_number, raw_text, snippet) {
        }

    }

    class MultiLineSnippetLine : LineWithText {

        public MultiLineSnippetLine(string src_file_path, int line_number, string raw_text, string text)
            : base(src_file_path, line_number, raw_text, text) {
        }

    }

    // Dialogue lines

    class DialogueLine : LineWithCharacter {

        public DialogueLine(string src_file_path, int line_number, string raw_text, string text, string character_tag)
            : base(src_file_path, line_number, raw_text, text, character_tag) {
        }

    }

    class AnonymousDialogueLine : LineWithText {

        public AnonymousDialogueLine(string src_file_path, int line_number, string raw_text, string text)
            : base(src_file_path, line_number, raw_text, text) {
        }

    }

    // Choices

    class Choice : LineWithCharacter, IChoice {

        public Choice(string src_file_path, int line_number, string raw_text, string text, string character_tag)
            : base(src_file_path, line_number, raw_text, text, character_tag) {
        }

    }

    class ConditionalChoice : LineWithCharacter, ISnippet, IChoice {

        public string Snippet { get; private set; }

        public ConditionalChoice(string src_file_path, int line_number, string raw_text, string snippet, string text, string character_tag)
            : base(src_file_path, line_number, raw_text, text, character_tag) {
            Snippet = snippet;
        }

    }

    class AnonymousConditionalChoice : LineWithText, ISnippet, IChoice {

        public string Snippet { get; private set; }

        public AnonymousConditionalChoice(string src_file_path, int line_number, string raw_text, string snippet, string text)
            : base(src_file_path, line_number, raw_text, text) {
            Snippet = snippet;
        }

    }

}
