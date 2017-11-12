using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class VGPCSharpMethod {

            public string Name { get; private set; }

            public VGPCSharpMethod(string name) {
                Name = name;
            }

        }

    }

}
