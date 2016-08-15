using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        abstract class Line {

            public abstract bool IsValid();

            public virtual void Validate() {
                if (!IsValid()) {
                    throw new Exception(string.Format("Invalid '{0}'!", this.ToString()));
                }
            }

        }

    }

}