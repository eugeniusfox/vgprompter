using System.Collections.Generic;

namespace VGPrompter {

    public abstract class VGPBaseMenu : VGPCallable {

        public IEnumerable<VGPBaseChoice> Choices { get; protected set; }

        public VGPBaseMenu(IEnumerable<VGPBaseChoice> choices) {
            Choices = choices;
        }

    }

}
