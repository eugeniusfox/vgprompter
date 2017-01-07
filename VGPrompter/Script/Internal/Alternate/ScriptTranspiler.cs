using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.IO;
using System.CodeDom.Compiler;

namespace VGPrompter {

    public class ScriptTranspiler {
        CodeCompileUnit _unit;
        CodeTypeDeclaration _class;
        string _output_filename;

        static string[] _imports = {
            "System",
            "System.Collections.Generic"
        };

        const string
            YIELD_MENU = @"yield return new Script.Menu(choices);",
            NEW_CHOICE = @"new Script.Choice(""{0}"", {1}, {2});";

        const string
            MENU_BLOCK =
@"
if(!ScriptLogic.True.IsTrue)
	goto case {0};

var choices = new List<Script.Choice>() {
{1}
};

yield return new Script.Menu(choices);

if (CurrentChoice == null)
	throw new Exception(""Missing choice!"");

var j = CurrentChoice.Value;
        CurrentChoice = null;

yield return choices[j];  // To check...

switch(j) {
{2}
}

goto case {3};";

        static readonly CodeTypeReference
            STRING = new CodeTypeReference(typeof(string));

        public ScriptTranspiler(string input_path, string output_path) {
            _output_filename = output_path;

            var class_name = Path.GetFileNameWithoutExtension(_output_filename);

            InitializeTargetClass(class_name);

        }

        CodeMethodReturnStatement GetGetter(string field_name) {
            return
                new CodeMethodReturnStatement(
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(), field_name));
        }

        CodeAssignStatement GetSetter(string field_name) {
            return
                new CodeAssignStatement(
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(), field_name),
                        new CodePropertySetValueReferenceExpression());
        }

        string GetFieldName(string property_name) {
            return string.Format("_{0}", property_name.ToLowerInvariant());
        }

        void AddProperty<T>(CodeTypeDeclaration target_class, string name, bool is_readonly = false) {

            var field_name = GetFieldName(name);
            var type = new CodeTypeReference(typeof(T));

            var field = new CodeMemberField() {
                Name = field_name,
                Type = type
            };

            var p = new CodeMemberProperty() {
                Name = name,
                Attributes = MemberAttributes.Public,
                Type = type
            };

            p.GetStatements.Add(GetGetter(field_name));
            if (!is_readonly)
                p.SetStatements.Add(GetSetter(field_name));

            target_class.Members.Add(field);
            target_class.Members.Add(p);
        }

        CodeSnippetExpression GetSwitch(int i) {
            var exp = string.Format(@"switch ({0}) {\n{1}\n}", i, "");

            return new CodeSnippetExpression(exp);
        }

        void GetCase(int i) {
            var s = string.Format(@"case {0}:", i);
            //return CodeSnippetStatement("");
        }

        void InitializeTargetClass(string class_name) {
            _unit = new CodeCompileUnit();

            var ns = new CodeNamespace("VGPrompter");
            ns.Imports.AddRange(_imports.Select(x => new CodeNamespaceImport(x)).ToArray());

            _class = new CodeTypeDeclaration(class_name) {
                IsClass = true,
                TypeAttributes = TypeAttributes.Public | TypeAttributes.Serializable
            };

            // Add 'Serializable' attribute
            _class.CustomAttributes.Add(new CodeAttributeDeclaration("System.Serializable"));

            // Add public properties
            AddProperty<string>(_class, "Label");
            AddProperty<int>(_class, "CurrentLine");

            ns.Types.Add(_class);
            _unit.Namespaces.Add(ns);
        }

        public void GenerateCSharpCode(string fileName) {
            using (var provider = CodeDomProvider.CreateProvider("CSharp")) {
                var options = new CodeGeneratorOptions() {
                    BracingStyle = "Block"  // vs. "C"
                };

                using (var src_writer = new StreamWriter(fileName)) {
                    provider.GenerateCodeFromCompileUnit(
                        _unit, src_writer, options);
                }
            }
        }

    }
}
