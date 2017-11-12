using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        internal abstract class ChildContainer<T> : Container<T> where T : Line {

            Script _script;
            VGPBlock _parent;

            public VGPBlock Parent {
                get {
                    return _parent;
                }
                set {
                    if (_parent == null) {
                        _parent = value;
                    } else {
                        throw new Exception("The Parent of a SubBlock cannot be changed!");
                    }
                }
            }

            public override Script Script {
                get {
                    return _parent != null ? _parent.Script : _script;
                }
                set {
                    if (_parent == null) {
                        _script = value;
                    } else {
                        throw new Exception("Script is already derived from parent!");
                    }
                }
            }

        }

    }

}