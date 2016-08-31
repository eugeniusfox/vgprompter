using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class VGPWhile : IterableContainer, IConditional {

            [NonSerialized]
            Func<bool> _condition;

            public string Tag { get; private set; }
            public static uint MaxIterations = 100;
            public bool IsTrue { get { return Condition(); } }

            public uint Iterations { get; private set; }
            public Func<bool> Condition { get { return _condition; } set { _condition = value; } }

            public VGPWhile() {
                InitializeContainer();
            }

            public VGPWhile(string label, VGPBlock parent)
                : this() {
                Tag = label;
                Parent = parent;
            }

            public bool Evaluate() {

                Validate();

                /* So that MaxIterations is reset when the loop exits
                 * on its own (non pathological state).
                 */

                if (IsTrue) {
                    if (++Iterations < MaxIterations) {
                        return true;
                    } else {
                        throw new Exception("While loop exceed the maximum of iterations allowed!");
                    }

                } else {
                    Iterations = 0;
                }

                return false;
            }

            public override bool IsValid() { return Condition != null; }

            public new string ToString() {
                return "WHILE: " + Tag;
            }

        }

    }

}