using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        abstract class VGPBaseReference : Line, IWrappable {

            public string Tag { get; private set; }

            public VGPBaseReference(string label) {
                Tag = label;
            }

            public abstract override bool IsValid();

            public abstract IScriptLine ToWrapper(Script script = null);

        }

    }

}