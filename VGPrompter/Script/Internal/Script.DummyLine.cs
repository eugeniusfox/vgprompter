using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        abstract class DummyLine : Line {

            public override bool IsValid() { return true; }

        }

    }

}