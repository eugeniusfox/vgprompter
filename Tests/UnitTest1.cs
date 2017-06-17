using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using VGPrompter;
using System.Collections.Generic;

namespace Tests {

    [TestClass]
    public class UnitTest1 {

        public static string GetResourcePath(string filename) {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Data\" + filename);
        }

        public static readonly string TEST_SCRIPT_1 = "insc_test1.rpy";
        public static readonly string TEST_SCRIPT_1_TAB = "insc_test1_tab.rpy";

        public Script LoadScript(string fp, Script.Parser.IndentChar indent = Script.Parser.IndentChar.Auto) {

            Assert.IsTrue(File.Exists(fp));

            var actions = new Dictionary<string, Action>() {
                { "Nothing", () => { } }
            };

            var conditions = new Dictionary<string, Func<bool>>() {
                { "True", () => true },
                { "False", () => false }
            };

            Script.Parser.Logger = new VGPrompter.Logger();

            var script = Script.FromSource(fp, indent: indent, ignore_unsupported_renpy: false);
            script.Conditions = conditions;

            script.Logger = new VGPrompter.Logger();

            return script;

        }


        [TestMethod]
        public void TestScriptPriming() {

            var script = LoadScript(GetResourcePath(TEST_SCRIPT_1));
            script.Prime();
            script.Validate();

        }

        [TestMethod]
        public void TestScriptEnumerator() {
            var script = LoadScript(GetResourcePath(TEST_SCRIPT_1_TAB));
            script.Prime();
            script.Validate();
            foreach (var x in script) {
                if (x is Script.Menu)
                    script.CurrentChoiceIndex = (uint)SelectChoice(x as Script.Menu);
                Console.WriteLine(x.ToString());
            }
        }

        [TestMethod]
        public void TestScriptEnumeratorWhile() {
            var script = LoadScript(GetResourcePath(TEST_SCRIPT_1_TAB));
            script.Prime();
            script.Validate();

            var i = 0;
            foreach (var line in script) {
                if (i++ > 2) {
                    break;
                }
                Console.WriteLine(line.ToString());
            }

        }

        [TestMethod]
        public void TestScriptSerialization() {

            var fn = GetResourcePath("serialized.bin");
            var script = LoadScript(GetResourcePath(TEST_SCRIPT_1_TAB));

            script.Prime();
            script.Validate();
            script.RepeatLastLineOnRecover = false;

            PlayTest(script, 2);

            var bytes = script.ToBinary();
            File.WriteAllBytes(fn, bytes);

            var dscript = Utils.LoadSerialized<Script>(fn);
            dscript.Validate();

            PlayTest(dscript);


            /*var enum1 = script.GetEnumeratorStrings();

            for (int i = 0; i < 2; i++)
                enum1.GetEnumerator().MoveNext();

            var enum2 = dscript.GetEnumeratorStrings();

            enum2.GetEnumerator().MoveNext();

            var a = enum1.GetEnumerator().Current;
            var b = enum2.GetEnumerator().Current;

            Console.WriteLine(a);
            Console.WriteLine(b);

            Assert.AreEqual(a, b);*/


            //PlayTest(dscript);

        }

        /*public void PlayTest(Script script, int? n = null) {
            Console.WriteLine("Playtest");
            var rnd = new Random();
            script.RunFromBeginning(
                (menu) => script.CurrentChoiceIndex = (uint)rnd.Next(menu.Count - 1),
                (line) => Console.WriteLine(line.ToString()),
                n: n
            );
        }*/

        public void PlayTest(Script script, int? n = null) {
            Console.WriteLine("Playtest");
            script.RunFromCurrentLine(
                OnMenu: menu => (new Random()).Next(menu.Count - 1),
                OnLine: line => Console.WriteLine(line.ToString()),
                Take: n
            );
        }

        int SelectChoice(Script.Menu menu) {
            return (new Random()).Next(menu.Count - 1);
        }

    }

}
