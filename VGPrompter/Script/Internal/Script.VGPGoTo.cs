using System;
using System.Runtime.Serialization;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class VGPGoTo : Line {

            string _target;
            public bool IsCall { get; private set; }
            public VGPBlock Target { get; private set; }

            public VGPGoTo() { }

            public VGPGoTo(string target_label, bool is_call) {
                _target = target_label;
                IsCall = is_call;
            }

            public void SetTarget(Script script) {
                Target = script.Blocks[_target];
            }

            protected VGPGoTo(SerializationInfo info, StreamingContext context) {
                _target = info.GetString("target");
                IsCall = info.GetBoolean("is_call");
            }

            public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
                info.AddValue("target", _target);
                info.AddValue("is_call", IsCall);
            }

            public override bool IsValid() {
                return Target != null;
            }

        }

    }

}