using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VGPrompter.Commons;

namespace VGPrompter.CodeGen.CSharp {
    public static partial class Generator {

        const char TAB = '\t';

        // .Replace("\n", new String(TAB, indent));

        static string GetUsingStatement(string ns) => string.Format("using {0};", ns);

        static string GetOpenClass(string name, string parent = null) => string.IsNullOrEmpty(parent) ? string.Format("class {0} {", name) : string.Format("class {0} : {1} {", name, parent);

        [Obsolete]
        static string GetFunctionOld(string name, Type rtype, Type[] arg_types = null, string[] arg_names = null) {
            if ((arg_types == null && arg_names != null) || (arg_types != null && arg_names == null))
                throw new Exception("Argument names and types must be either both valid or null!");
            if (arg_types.Length != arg_names.Length)
                throw new Exception("Inconsistent number of arguments!");

            return string.Format("{0} {1}({2}) {",
                rtype, name,
                arg_names == null ? string.Empty :
                    string.Join(", ", arg_names.Select((x, i) => string.Format("{0} {1}", arg_types[i].FullName, x)).ToArray()));
        }

        static string GetConstructor(string class_name, Type[] arg_types, string[] arg_names) {
            if (arg_types.Length != arg_names.Length) throw new Exception("Inconsistent number of arguments!");
            return string.Format("public {0}({1}) {",
                class_name,
                string.Join(", ", arg_names.Select((name, i) => string.Format("{0} {1}", arg_types[i].FullName, name)).ToArray()));
        }

        public static string GetOpenBlockStatement(string keyword, object value) => string.Format("{0} ({1}) {", keyword, value);
        public static string CloseBlock =  "}";

        public static string GetOpenCaseStatement(int value) => string.Format("case {0}:", value);
        public static string CloseCaseBlock = "break;";

        public static string GetSwitchStatement(string variable) => GetOpenBlockStatement("switch", variable);

        public static string ConcatenateStatements(params string[] statements) => string.Join("\n", statements);

        public static string GetTernaryOperator(string condition, string then_statement, string else_statement) => string.Format("{0} ? {1} : {2}", condition, then_statement, else_statement);

        static string GetTypeName(Type type) {
            if (type == typeof(void)) {
                return "void";
            } else if (type == typeof(bool)) {
                return "bool";
            } else if (type == typeof(string)) {
                return "string";
            } else if (type == typeof(double)) {
                return "double";
            } else if (type == typeof(float)) {
                return "float";
            } else if (type == typeof(int)) {
                return "int";
            } else {
                return type.FullName;
            }
        }

        static string GetFunction(string name, Type rtype, Type[] arg_types = null, string[] arg_names = null) {

            if ((arg_types == null && arg_names != null) || (arg_types != null && arg_names == null))
                throw new Exception("Argument names and types must be either both valid or null!");

            if (arg_types != null && arg_names != null) {
                if (arg_types.Length != arg_names.Length)
                    throw new Exception("Inconsistent number of arguments!");
            }

            return string.Format("{0} {1}({2})",
                GetTypeName(rtype), name,
                arg_names == null ? string.Empty :
                    string.Join(", ", arg_names.Select((x, i) => string.Format("{0} {1}", GetTypeName(arg_types[i]), x)).ToArray()));

        }

        static string WrapFunction(string name, string body, Type rtype, bool is_public) => string.Format("{2} {0} {{\n{1}\n}}", GetFunction(name, rtype), body, is_public ? "public" : string.Empty);

        public static string WrapAction(string name, string body, bool is_public = true) => WrapFunction(name, body, typeof(void), is_public);
        public static string WrapCondition(string name, string body, bool is_public = true) => WrapFunction(name, body, typeof(bool), is_public);

        /*public static void ast2enumerator(Script syntax_tree) {

            foreach (var b in syntax_tree.Blocks.Values) {
                
            }
        }*/

    }
}
