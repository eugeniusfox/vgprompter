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

        Dictionary<string, VGPBlock> Blocks { get; set; }
        public string StartingLabel { get; set; }
        public uint? CurrentChoiceIndex { get; set; }

        [NonSerialized]
        Dictionary<string, Func<bool>> _conditions;
        [NonSerialized]
        Dictionary<string, Action> _actions;
        [NonSerialized]
        Dictionary<string, Delegate> _functions;
        [NonSerialized]
        TextManager _text_manager;
        [NonSerialized]
        bool _is_primed = false;

        public Dictionary<string, Func<bool>> Conditions { get { return _conditions; } set { _conditions = value; } }
        public Dictionary<string, Action> Actions { get { return _actions; } set { _actions = value; } }
        public Dictionary<string, Delegate> Functions { get { return _functions; } set { _functions = value; } }
        public Dictionary<string, Dictionary<string, string>> Lines { get { return _text_manager.Lines; } }
        public Dictionary<string, string> Globals { get { return _text_manager.Globals; } }

        public bool RepeatLastLineOnRecover { get; set; }
        int CurrentIterableLineOffset { get { return RepeatLastLineOnRecover ? 1 : 0; } }

        VGPBlock Start { get { return Blocks[StartingLabel]; } }

        /* Recover */
        IterableContainer CurrentIterable { get; set; }
        int CurrentIterableLine { get; set; }
        //bool IsPaused { get { return CurrentIterable != null; } }

        public bool IsPrimed {
            get { return _is_primed; }
            private set { _is_primed = value; }
        }
        public int NumberOfMenus { get; private set; }
        public int CurrentBlockID;
        public bool HasReturned { get; private set; }

        /* Get enumerator */

        IEnumerator GetAnyEnumerator(
            IterableContainer container,
            Func<DialogueLine, IEnumerator> OnLine,
            Func<Menu, IEnumerator> OnMenu,
            Func<IEnumerator> OnReturn = null,
            Func<Reference, IEnumerator> OnReference = null) {

            if (!IsPrimed) throw new Exception("Can't iterate if the script is not primed!");

            return container.GetEnumerator(OnLine, OnMenu, OnReturn, OnReference);
        }

        public IEnumerator GetStartingEnumerator(
            Func<DialogueLine, IEnumerator> OnLine,
            Func<Menu, IEnumerator> OnMenu,
            Func<IEnumerator> OnReturn = null,
            Func<Reference, IEnumerator> OnReference = null) {

            return GetAnyEnumerator(Start, OnLine, OnMenu, OnReturn, OnReference);
        }

        public IEnumerator GetCurrentEnumerator(
            Func<DialogueLine, IEnumerator> OnLine,
            Func<Menu, IEnumerator> OnMenu,
            Func<IEnumerator> OnReturn = null,
            Func<Reference, IEnumerator> OnReference = null) {

            return GetAnyEnumerator(CurrentIterable ?? Start, OnLine, OnMenu, OnReturn, OnReference);
        }

        public IEnumerator GetLabelEnumerator(
            string label,
            Func<DialogueLine, IEnumerator> OnLine,
            Func<Menu, IEnumerator> OnMenu,
            Func<IEnumerator> OnReturn = null,
            Func<Reference, IEnumerator> OnReference = null) {

            return GetAnyEnumerator(GetLabel(label), OnLine, OnMenu, OnReturn, OnReference);
        }

        /* Enumerators with split SelectChoice and OnChoiceSelected coroutines */

        IEnumerator GetAnyEnumerator(
            IterableContainer container,
            Func<DialogueLine, IEnumerator> OnLine,
            Func<Menu, IEnumerator<int?>> SelectChoice,
            Func<Menu, Choice, IEnumerator> OnChoiceSelected,
            Func<IEnumerator> OnReturn = null,
            Func<Reference, IEnumerator> OnReference = null) {

            if (!IsPrimed) throw new Exception("Can't iterate if the script is not primed!");

            return container.GetEnumerator(OnLine, SelectChoice, OnChoiceSelected, OnReturn, OnReference);
        }

        public IEnumerator GetCurrentEnumerator(
            Func<DialogueLine, IEnumerator> OnLine,
            Func<Menu, IEnumerator<int?>> SelectChoice,
            Func<Menu, Choice, IEnumerator> OnChoiceSelected,
            Func<IEnumerator> OnReturn = null,
            Func<Reference, IEnumerator> OnReference = null) {

            return GetAnyEnumerator(CurrentIterable ?? Start, OnLine, SelectChoice, OnChoiceSelected, OnReturn, OnReference);
        }

        public IEnumerator GetLabelEnumerator(
            string label,
            Func<DialogueLine, IEnumerator> OnLine,
            Func<Menu, IEnumerator<int?>> SelectChoice,
            Func<Menu, Choice, IEnumerator> OnChoiceSelected,
            Func<IEnumerator> OnReturn = null,
            Func<Reference, IEnumerator> OnReference = null) {

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

        VGPBlock GetLabel(string label) {
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

        public static Script FromBinary(byte[] script_bytes, byte[] strings_bytes)
        {
            var script = FromBinary(script_bytes);
            var rows = System.Text.Encoding.UTF8.GetString(strings_bytes);
            script.LoadStrings(rows);
            return script;
        }

        public static Script FromFiles(string script_path, string strings_path) {
            var script = FromBinary(script_path);
            script.LoadStrings(strings_path);
            return script;
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
            Blocks = new Dictionary<string, VGPBlock>();
            _text_manager = new TextManager();
        }

        internal Script(TextManager tm)
            : this() {
            _text_manager = tm;
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
            var success = true;
            foreach (var block in Blocks.Values) {
                try {
                    block.Prime();
                } catch (KeyNotFoundException ex) {
                    Logger.Log(ex.Message);
                    success = false;
                    break;
                }
            }
            IsPrimed = success;
        }

        public bool TryPrime() {
            Prime();
            return IsPrimed;
        }

        public void SetDelegates(Dictionary<string, Func<bool>> conditions, Dictionary<string, Action> actions) {
            Conditions = conditions;
            Actions = actions;
        }

        /* CONSUMPTION */

        public IEnumerator<IScriptLine> GetEnumerator() {

            HasReturned = false;

            if (!IsPrimed) throw new Exception("Can't iterate if the script is not primed!");

            foreach (IWrappable x in Start) {
                if (x == null) throw new Exception("Can't wrap this line!");
                yield return x.ToWrapper(this);
            }
        }

        void RunIterable(Func<Menu, int> OnMenu, Action<DialogueLine> OnLine, IterableContainer c, Action<Reference> OnReference = null, int? Take = null) {

            var i = Take.GetValueOrDefault();

            foreach (var x in this) {

                if (Take != null && --i < 0) break;

                if (x is Menu) {

                    var menu = x as Menu;
                    var ichoice = OnMenu(menu);

                    if (ichoice < 0 || ichoice >= menu.Count)
                        throw new Exception("Invalid choice index!");

                    CurrentChoiceIndex = (uint)ichoice;

                } else if (x is DialogueLine) {

                    OnLine(x as DialogueLine);

                } else if (x is Reference) {

                    var reference = x as Reference;
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

        public void RunFromCurrentLine(Func<Menu, int> OnMenu, Action<DialogueLine> OnLine, Action<Reference> OnReference = null, int? Take = null) {
            RunIterable(OnMenu, OnLine, CurrentIterable ?? Start, OnReference, Take);
        }

        public void RunFromBeginning(Func<Menu, int> OnMenu, Action<DialogueLine> OnLine, Action<Reference> OnReference = null, int? Take = null) {
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

        Delegate GetFunction(string key) {
            return Utils.GetFromDictionary(key, Functions, Logger);
        }

        public byte[] ToBinary() {
            return Utils.GetBinary(this);
        }

        public void SaveStrings(string path) {
            _text_manager.ToCSV(path);
        }

        public void LoadStrings(string path) {
            if (_text_manager == null)
                _text_manager = new TextManager();

            _text_manager.FromCSV(path);
        }

        public void LoadStrings(string[] rows) {
            if (_text_manager == null)
                _text_manager = new TextManager();

            _text_manager.FromCSV(rows);
        }

        public void ToFile(string path) {
            System.IO.File.WriteAllBytes(path, ToBinary());
        }

        public void ToFiles(string script_path, string strings_path) {
            ToFile(script_path);
            SaveStrings(strings_path);
        }

    }

}