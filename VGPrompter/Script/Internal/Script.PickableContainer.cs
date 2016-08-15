using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        abstract class PickableContainer<T> : ChildContainer<T> where T : Line {

            public abstract T GetContent();

        }

    }

}