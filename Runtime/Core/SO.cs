using System.Collections.Generic;
using Moimo.Modules;
using Moimo.Utility;
using UnityEngine;

namespace Moimo.Core
{
    public static class SO
    {
        private static Dictionary<string, List<ScriptableObjectAuto>> resources;

        static SO()
        {
            var _libraries = Resources.LoadAll<PackLibrary>("");
            if (_libraries.Length == 0) return;

            resources = _libraries[0].scriptableResources;
        }

        public static T Get<T>() where T : ScriptableObjectAuto
        {
            TryGet<T>(out var _obj);
            return _obj;
        }

        public static List<T> GetAll<T>() where T : ScriptableObjectAuto
        {
            TryGetAll<T>(out var _list);
            return _list;
        }

        public static bool TryGet<T>(out T obj) where T : ScriptableObjectAuto
        {
            if (TryGetAll<T>(out var _list) && _list.Count > 0)
            {
                obj = _list[0];
                return true;
            }

            obj = null;
            return false;
        }

        public static bool TryGetAll<T>(out List<T> result) where T : ScriptableObjectAuto
        {
            result = new List<T>();

            if (resources == null || !resources.TryGetValue(typeof(T).AssemblyQualifiedName, out var _list) || _list == null)
            {
                return false;
            }

            result = new List<T>(_list.Count);

            foreach (var _item in _list)
            {
                if (_item is T _obj)
                {
                    result.Add(_obj);
                }
            }

            return result.Count > 0;
        }
    }
}