using System;
using System.Reflection;

namespace VGPrompter {

    public abstract class Mirror {

        static readonly object[] EMPTY_ARRAY = { };

        Type _type;

        public Mirror() {
            _type = GetType();
        }

        // protected string ClassName => _type.Name;

        bool TryGetMethod(string name, out MethodInfo method) {
            method = _type.GetMethod(name);
            return method != null;
        }

        protected void Invoke(string name) {
            if (!TryGetMethod(name, out MethodInfo m)) throw new Exception(string.Format("Unknown method '{0}'!", name));
            m.Invoke(this, EMPTY_ARRAY);
        }

        protected bool InvokeBoolean(string name) {
            if (!TryGetMethod(name, out MethodInfo m)) throw new Exception(string.Format("Unknown method '{0}'!", name));
            return (bool)m.Invoke(this, EMPTY_ARRAY);
        }

    }

}
