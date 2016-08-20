using System;
using System.Runtime.Serialization;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class VGPGoTo : Line {

            string _target;
            public bool IsCall { get; private set; }
            public VGPBlock Target { get; private set; }

            public VGPGoTo(string target_label, bool is_call) {
                _target = target_label;
                IsCall = is_call;
            }

            public void SetTarget(Script script) {
                Target = script.Blocks[_target];
            }

            public override bool IsValid() {
                return Target != null;
            }

        }

    }

}