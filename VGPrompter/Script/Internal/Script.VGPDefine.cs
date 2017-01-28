using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class VGPDefine : Line {

            public string Key { get; private set; }
            public string Value { get; private set; }

            public VGPDefine(string key, string value) {
                Key = key;
                Value = value;
            }

            public override bool IsValid() {
                return !string.IsNullOrEmpty(Value);
            }

        }

    }

}
