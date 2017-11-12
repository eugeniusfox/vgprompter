using System.Collections.Generic;
using System;
using System.Linq;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        internal class VGPBlock : IterableContainer {

            public string Label { get; private set; }
            public List<int> FromInstanceIDs { get; private set; }

            public VGPBlock() {
                FromInstanceIDs = new List<int>();
                Contents = new List<Line>();
            }

            public VGPBlock(string label)
                : this() {
                Label = label;
            }

            public VGPBlock(string label, List<Line> contents)
                : this(label) {
                Contents = contents ?? throw new Exception("Block contents can't be null!");
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