using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        abstract class Line {

            public abstract bool IsValid();

            // protected abstract string ValidationErrorMessage { get; }

            public virtual void Validate() {
                if (!IsValid()) {
                    var msg = string.Format("Invalid '{0}'!", ToString());
                    /* if (!string.IsNullOrEmpty(ValidationErrorMessage))
                        msg += '\n' + ValidationErrorMessage;*/
                    throw new Exception(msg);
                }
            }

        }

    }

}