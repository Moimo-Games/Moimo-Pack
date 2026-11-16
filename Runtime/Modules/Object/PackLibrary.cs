using System.Collections.Generic;
using Moimo.Core;
using Moimo.Serialize;
using Moimo.Utility;
using Sirenix.OdinInspector;

namespace Moimo.Modules
{
    public class PackLibrary : ScriptableObjectAuto
    {
        public GameManager gameManager;
        public SerializedDictionary<string, ObjectBehaviour> objectResources = new();
        public SerializedDictionary<string, List<ScriptableObjectAuto>> scriptableResources = new();

#if UNITY_EDITOR
        [Button(ButtonSizes.Medium)]
        protected virtual void Refresh()
        {
            gameManager = AssetEditor.Load<GameManager>();
            
            objectResources.Clear();
            foreach (var _data in AssetEditor.LoadAll<ObjectBehaviour>())
            {
                objectResources.Add(_data.GetType().AssemblyQualifiedName, _data);
            }


            scriptableResources.Clear();
            var _allSO = AssetEditor.LoadAll<ScriptableObjectAuto>();
            foreach (var _item in _allSO)
            {
                var _type = _item.GetType();

                while (_type != null && typeof(ScriptableObjectAuto).IsAssignableFrom(_type))
                {
                    if (!scriptableResources.TryGetValue(_type.AssemblyQualifiedName, out var _list))
                    {
                        _list = new List<ScriptableObjectAuto>();
                        scriptableResources.Add(_type.AssemblyQualifiedName, _list);
                    }

                    _list.Add(_item);

                    _type = _type.BaseType;
                }
            }
        }
#endif
    }
}
