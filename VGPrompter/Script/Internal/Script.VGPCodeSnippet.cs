using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class VGPCodeSnippet : Line {
            
            public string CodeSnippet { get; private set; }

            public VGPCodeSnippet() { }

            public VGPCodeSnippet(string code) {
                CodeSnippet = code;
            }

            public override bool IsValid() {
                return true;
            }
        }

    }

}
