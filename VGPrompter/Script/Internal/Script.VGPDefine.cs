using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class VGPDefine : Line, ITextual {

            public string Key { get; private set; }
            public string Value { get; private set; }
            public bool ToInterpolate { get; internal set; }

            public VGPDefine(string key, string value, bool to_interpolate) {
                Key = key;
                Value = value;
                ToInterpolate = to_interpolate;
            }

            public override bool IsValid() {
                return !string.IsNullOrEmpty(Value);
            }

            public string GetInterpolatedText(Script script) {
                return script._text_manager.GetGlobalText(Key, ToInterpolate);
            }

        }

    }

}
