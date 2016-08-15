using System.Collections.Generic;
using System;
using System.Linq;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class Block : IterableContainer {

            public string Label { get; private set; }
            public List<int> FromInstanceIDs { get; private set; }

            public Block() {
                FromInstanceIDs = new List<int>();
                Contents = new List<Line>();
            }

            public Block(string label)
                : this() {
                Label = label;
            }

            public Block(string label, List<Line> contents)
                : this(label) {
                if (contents == null) throw new Exception("Block contents can't be null!");
                Contents = contents;
            }

            public void RegisterID(int id) {
                FromInstanceIDs.Add(id);
            }

            public override bool IsValid() {
                return !string.IsNullOrEmpty(Label);
            }

            public new string ToString() {
                return "BLOCK " + Label + ": " + string.Join(COMMA, Contents.Select(x => x.ToString()).ToArray());
            }

        }

    }

}