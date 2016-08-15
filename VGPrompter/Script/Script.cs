using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections;

namespace VGPrompter {

    [Serializable]
    public partial class Script {

        public const string
            TRUE = "True",
            FALSE = "False",
            DEFAULT_STARTING_LABEL = "start",
            COMMA = ", ";

        Dictionary<string, Block> Blocks { get; set; }
        public Dictionary<string, Func<bool>> Conditions { get; set; }
        public Dictionary<string, Action> Actions { get; set; }
        public string StartingLabel { get; set; }
        public uint? CurrentChoiceIndex { get; set; }

        public bool RepeatLastLineOnRecover { get; set; }
        int CurrentIterableLineOffset { get { return RepeatLastLineOnRecover ? 1 : 0; } }

        Block Start { get { return Blocks[StartingLabel]; } }

        /* Recover */
        IterableContainer CurrentIterable { get; set; }
        int CurrentIterableLine { get; set; }
        //bool IsPaused { get { return CurrentIterable != null; } }

        public bool IsPrimed { get; private set; }
        public int NumberOfMenus { get; private set; }
        public int CurrentBlockID;
        public bool HasReturned { get; private set; }

        /* Get enumerator */

        IEnumerator GetAnyEnumerator(
            IterableContainer container,
            Func<LineWrapper, IEnumerator> OnLine,
            Func<MenuWrapper, IEnumerator> OnMenu,
            Func<IEnumerator> OnReturn = null,
            Func<ReferenceWrapper, IEnumerator> OnReference = null) {

            return container.GetEnumerator(OnLine, OnMenu, OnReturn, OnReference);
        }

        public IEnumerator GetStartingEnumerator(
            Func<LineWrapper, IEnumerator> OnLine,
            Func<MenuWrapper, IEnumerator> OnMenu,
            Func<IEnumerator> OnReturn = null,
            Func<ReferenceWrapper, IEnumerator> OnReference = null) {

            return GetAnyEnumerator(Start, OnLine, OnMenu, OnReturn, OnReference);
        }

        public IEnumerator GetCurrentEnumerator(
            Func<LineWrapper, IEnumerator> OnLine,
            Func<MenuWrapper, IEnumerator> OnMenu,
            Func<IEnumerator> OnReturn = null,
            Func<ReferenceWrapper, IEnumerator> OnReference = null) {

            return GetAnyEnumerator(CurrentIterable ?? Start, OnLine, OnMenu, OnReturn, OnReference);
        }

        public IEnumerator GetLabelEnumerator(
            string label,
            Func<LineWrapper, IEnumerator> OnLine,
            Func<MenuWrapper, IEnumerator> OnMenu,
            Func<IEnumerator> OnReturn = null,
            Func<ReferenceWrapper, IEnumerator> OnReference = null) {

            return GetAnyEnumerator(GetLabel(label), OnLine, OnMenu, OnReturn, OnReference);
        }

        /* Enumerators with split SelectChoice and OnChoiceSelected coroutines */

        IEnumerator GetAnyEnumerator(
            IterableContainer container,
            Func<LineWrapper, IEnumerator> OnLine,
            Func<MenuWrapper, IEnumerator<int?>> SelectChoice,
            Func<MenuWrapper, ChoiceWrapper, IEnumerator> OnChoiceSelected,
            Func<IEnumerator> OnReturn = null,
            Func<ReferenceWrapper, IEnumerator> OnReference = null) {

            return container.GetEnumerator(OnLine, SelectChoice, OnChoiceSelected, OnReturn, OnReference);
        }

        public IEnumerator GetCurrentEnumerator(
            Func<LineWrapper, IEnumerator> OnLine,
            Func<MenuWrapper, IEnumerator<int?>> SelectChoice,
            Func<MenuWrapper, ChoiceWrapper, IEnumerator> OnChoiceSelected,
            Func<IEnumerator> OnReturn = null,
            Func<ReferenceWrapper, IEnumerator> OnReference = null) {

            return GetAnyEnumerator(CurrentIterable ?? Start, OnLine, SelectChoice, OnChoiceSelected, OnReturn, OnReference);
        }

        public IEnumerator GetLabelEnumerator(
            string label,
            Func<LineWrapper, IEnumerator> OnLine,
            Func<MenuWrapper, IEnumerator<int?>> SelectChoice,
            Func<MenuWrapper, ChoiceWrapper, IEnumerator> OnChoiceSelected,
            Func<IEnumerator> OnReturn = null,
            Func<ReferenceWrapper, IEnumerator> OnReference = null) {

            return GetAnyEnumerator(GetLabel(label), OnLine, SelectChoice, OnChoiceSelected, OnReturn, OnReference);
        }

        /* Wrapper enumerators */

        IEnumerator GetAnyWrapperEnumerator(IterableContainer container) {
            return container.GetWrapperEnumerator();
        }

        public IEnumerator GetCurrentWrapperEnumerator() {
            return GetAnyWrapperEnumerator(CurrentIterable ?? Start);
        }

        public IEnumerator GetLabelWrapperEnumerator(string label) {
            return GetAnyWrapperEnumerator(GetLabel(label));
        }

        Block GetLabel(string label) {
            if (!Blocks.ContainsKey(label))
                throw new Exception("Invalid label!");
            return Blocks[label];
        }


        /* STATIC */

        Logger _logger;
        public Logger Logger {
            get {
                if (_logger == null)
                    _logger = new Logger();
                return _logger;
            }
            set {
                _logger = value;
            }
        }

        public static Script FromBinary(string path) {
            return Utils.LoadSerialized<Script>(path);
        }

        public static Script FromBinary(byte[] bytes) {
            return Utils.FromBinary<Script>(bytes);
        }

        public static Script FromSource(
            string path,
            bool recursive = false,
            Parser.IndentChar indent = Parser.IndentChar.Auto,
            bool ignore_unsupported_renpy = false) {

            return Parser.ParseSource(path, recursive, indent, ignore_unsupported_renpy);
        }


        /* CONSTRUCTORS */

        public Script() {
            IsPrimed = false;
            CurrentBlockID = 0;
            StartingLabel = DEFAULT_STARTING_LABEL;
            Blocks = new Dictionary<string, Block>();
        }

        public Script(Dictionary<string, Func<bool>> conditions, Dictionary<string, Action> actions, string starting_label = DEFAULT_STARTING_LABEL)
            : this() {
            StartingLabel = starting_label;
            Conditions = conditions;
            Actions = actions;
        }

        /* */

        public void Prime() {
            NumberOfMenus = 0;
            foreach (var block in Blocks.Values) {
                block.Prime();
            }
            IsPrimed = true;
        }

        public void SetDelegates(Dictionary<string, Func<bool>> conditions, Dictionary<string, Action> actions) {
            Conditions = conditions;
            Actions = actions;
        }

        /* CONSUMPTION */

        public IEnumerator<IScriptLineWrapper> GetEnumerator() {

            HasReturned = false;

            if (!IsPrimed) throw new Exception("Can't iterate if the script is not primed!");

            foreach (IWrappable x in Start) {
                if (x == null) throw new Exception("Can't wrap this line!");
                yield return x.ToWrapper();
            }
        }

        void RunIterable(Func<MenuWrapper, int> OnMenu, Action<LineWrapper> OnLine, IterableContainer c, Action<ReferenceWrapper> OnReference = null, int? Take = null) {

            var i = Take.GetValueOrDefault();

            foreach (var x in this) {

                if (Take != null && --i < 0) break;

                if (x is MenuWrapper) {

                    var menu = x as MenuWrapper;
                    var ichoice = OnMenu(menu);

                    if (ichoice < 0 || ichoice >= menu.Count)
                        throw new Exception("Invalid choice index!");

                    CurrentChoiceIndex = (uint)ichoice;

                } else if (x is LineWrapper) {

                    OnLine(x as LineWrapper);

                } else if (x is ReferenceWrapper) {

                    var reference = x as ReferenceWrapper;
                    if (OnReference == null) {
                        reference.Action();
                    } else {
                        OnReference(reference);
                    }


                } else {

                    Logger.Log("Unhandled item type: " + x.ToString());

                }
            }
        }

        public void RunFromCurrentLine(Func<MenuWrapper, int> OnMenu, Action<LineWrapper> OnLine, Action<ReferenceWrapper> OnReference = null, int? Take = null) {
            RunIterable(OnMenu, OnLine, CurrentIterable ?? Start, OnReference, Take);
        }

        public void RunFromBeginning(Func<MenuWrapper, int> OnMenu, Action<LineWrapper> OnLine, Action<ReferenceWrapper> OnReference = null, int? Take = null) {
            RunIterable(OnMenu, OnLine, Start, OnReference, Take);
        }

        public bool IsValid() {
            return Blocks != null && Blocks.Count > 0 && Blocks.All(x => x.Value.IsValid());
        }

        public void Validate() {
            foreach (var block in Blocks) {
                block.Value.Validate();
            }
        }


        /* HELPERS */

        Func<bool> GetCondition(string key) {
            return Utils.GetFromDictionary(key, Conditions, Logger);
        }

        Action GetAction(string key) {
            return Utils.GetFromDictionary(key, Actions, Logger);
        }

        public byte[] ToBinary() {
            return Utils.GetBinary(this);
        }

    }

}