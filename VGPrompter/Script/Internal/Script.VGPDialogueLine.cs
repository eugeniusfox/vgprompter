using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class VGPDialogueLine : Line, IWrappable {

            public string Tag { get; private set; }
            public string ParentLabel { get; private set; }
            public string TextHash { get; private set; }

            public VGPDialogueLine() { }

            public VGPDialogueLine(string label, string hash, string tag) {
                Tag = tag;
                ParentLabel = label;
                TextHash = hash;
            }

            public override bool IsValid() { return !string.IsNullOrEmpty(TextHash); }

            public new string ToString() {
                return (string.IsNullOrEmpty(Tag) ? "<Anonymous>" : Tag) + ": " + TextHash;
            }

            public IScriptLine ToWrapper(Script script) {
                var text = script._text_manager.GetText(ParentLabel, TextHash);
                return new DialogueLine(text, Tag);
            }

        }

    }

}