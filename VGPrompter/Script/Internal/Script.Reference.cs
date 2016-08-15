using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class Reference : Line, IWrappable {

            public string Tag { get; private set; }
            public Action Action { get; set; }

            public Reference(string label) {
                Tag = label;
            }

            public override bool IsValid() {
                return Action != null;
            }

            public IScriptLineWrapper ToWrapper() {
                return new ReferenceWrapper(Tag, Action);
            }
        }

    }

}