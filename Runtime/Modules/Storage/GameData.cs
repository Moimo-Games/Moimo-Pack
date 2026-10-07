using System;
using System.Collections.Generic;
using System.Linq;
using Moimo.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Moimo.Modules
{
    public class GameData
    {
        private Dictionary<Type, BaseStorage> storages = new();
        public T Get<T>() where T : BaseStorage, new() => storages[typeof(T)] as T;
        private readonly JsonSerializerSettings jsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        public GameData()
        {
            storages = InstanceUtility.Create<BaseStorage>();
            foreach (var _storage in storages.Values)
            {
                _storage.OnCreate();
            }
        }

        public string GetJson()
        {
            return JsonConvert.SerializeObject(storages, jsonSettings);
        }

        public void SetJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                foreach (var _storage in storages.Values)
                {
                    _storage.OnDefault();
                    _storage.OnLoad();
                }
                return;
            }

            try
            {
                var _raw = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(json, jsonSettings);
                var _serializer = JsonSerializer.Create(jsonSettings);

                foreach (var _key in storages.Keys.ToArray())
                {
                    var _matched = false;
                    try
                    {
                        var _matchKey = FindKey(_key, _raw);
                        if (_matchKey != null && _raw.TryGetValue(_matchKey, out var _token) && _token != null && _token.Type != JTokenType.Null)
                        {
                            var _storage = _token.ToObject(_key, _serializer) as BaseStorage;
                            if (_storage != null)
                            {
                                storages[_key] = _storage;
                                _matched = true;
                            }
                        }
                    }
                    catch (Exception _exInner)
                    {
                        Debug.LogWarning($"Skip error for storage type {_key.Name}: {_exInner.Message}");
                    }

                    if (!_matched)
                    {
                        storages[_key].OnDefault();
                    }

                    storages[_key].OnLoad();
                }
            }
            catch (Exception _ex)
            {
                Debug.LogWarning($"Failed to load storage data: {_ex.Message}");
                foreach (var _storage in storages.Values)
                {
                    _storage.OnDefault();
                    _storage.OnLoad();
                }
            }
        }

        public void New()
        {
            foreach (var _storage in storages.Values)
            {
                _storage.OnDefault();
                _storage.OnLoad();
            }
        }

        public void Save()
        {
            foreach (var _storage in storages.Values)
            {
                _storage.OnSave();
            }
        }

        private static string FindKey(Type type, Dictionary<string, JToken> raw)
        {
            if (raw == null || type == null) return null;

            var _aqn = type.AssemblyQualifiedName;
            if (!string.IsNullOrEmpty(_aqn) && raw.ContainsKey(_aqn)) return _aqn;

            var _fullName = type.FullName;
            if (!string.IsNullOrEmpty(_fullName) && raw.ContainsKey(_fullName)) return _fullName;

            var _name = type.Name;
            if (!string.IsNullOrEmpty(_name) && raw.ContainsKey(_name)) return _name;

            foreach (var _key in raw.Keys)
            {
                if (string.IsNullOrEmpty(_key)) continue;
                if (_key == _name) return _key;
                if (_key.EndsWith("." + _name, StringComparison.Ordinal)) return _key;
                if (_key.StartsWith(_name + ",", StringComparison.Ordinal)) return _key;
                if (_key.Contains("." + _name + ",", StringComparison.Ordinal)) return _key;
            }

            return null;
        }
    }
}
