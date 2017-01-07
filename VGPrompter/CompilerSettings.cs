namespace VGPrompter {

    public class Compiler {
        /*public struct CompilerSettings {

            public string CompilerPath { get; set; }
            public Script.Parser.IndentChar IndentChar { get; set; }

        }*/

        public string CompilerPath { get; set; }
        public Script.Parser.IndentChar IndentChar { get; set; }

        public bool Build() {
            return false;
        }

    }

}