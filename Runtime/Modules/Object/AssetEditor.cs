#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

public static class AssetEditor
{
    public static T[] LoadAll<T>() where T : Object
    {
        return LoadAll<T>(null);
    }

    public static T[] LoadAll<T>(string folderPath) where T : Object
    {
        var _list = new List<T>();
        var _type = typeof(T);

        string[] _searchFolders = null;
        if (!string.IsNullOrEmpty(folderPath))
            _searchFolders = new[] { folderPath };

        if (typeof(Component).IsAssignableFrom(_type))
        {
            var _guids = AssetDatabase.FindAssets("t:GameObject", _searchFolders);
            foreach (var _guid in _guids)
            {
                var _path = AssetDatabase.GUIDToAssetPath(_guid);
                var _go = AssetDatabase.LoadAssetAtPath<GameObject>(_path);
                if (_go == null) continue;

                var _comps = _go.GetComponentsInChildren<T>(true);
                if (_comps != null && _comps.Length > 0)
                    _list.AddRange(_comps);
            }

            return _list.ToArray();
        }

        var _assetGuids = AssetDatabase.FindAssets($"t:{_type.FullName}", _searchFolders);
        if (_assetGuids.Length == 0)
            _assetGuids = AssetDatabase.FindAssets($"t:{_type.Name}", _searchFolders);

        foreach (var _guid in _assetGuids)
        {
            var _path = AssetDatabase.GUIDToAssetPath(_guid);
            var _asset = AssetDatabase.LoadAssetAtPath<T>(_path);
            if (_asset != null)
                _list.Add(_asset);
        }

        return _list.ToArray();
    }

    public static T Load<T>() where T : Object
    {
        return Load<T>(null);
    }

    public static T Load<T>(string assetPath) where T : Object
    {
        var _type = typeof(T);

        if (!string.IsNullOrEmpty(assetPath))
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                if (typeof(Component).IsAssignableFrom(_type))
                {
                    var _results = LoadAll<T>(assetPath);
                    return _results.Length > 0 ? _results[0] : null;
                }

                var _guids = AssetDatabase.FindAssets($"t:{_type.FullName}", new[] { assetPath });
                if (_guids.Length == 0)
                    _guids = AssetDatabase.FindAssets($"t:{_type.Name}", new[] { assetPath });
                if (_guids.Length > 0)
                    assetPath = AssetDatabase.GUIDToAssetPath(_guids[0]);
            }
            else
            {
                if (typeof(Component).IsAssignableFrom(_type))
                {
                    var _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (_prefab != null)
                    {
                        var _comp = _prefab.GetComponentInChildren<T>(true);
                        if (_comp != null) return _comp;
                    }
                    return null;
                }

                var _loaded = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (_loaded != null) return _loaded;
            }
        }

        if (string.IsNullOrEmpty(assetPath))
        {
            if (typeof(Component).IsAssignableFrom(_type))
            {
                var _results = LoadAll<T>();
                return _results.Length > 0 ? _results[0] : null;
            }

            var _guids = AssetDatabase.FindAssets($"t:{_type.FullName}");
            if (_guids.Length == 0)
                _guids = AssetDatabase.FindAssets($"t:{_type.Name}");
            if (_guids.Length == 0) return null;
            assetPath = AssetDatabase.GUIDToAssetPath(_guids[0]);
        }

        if (typeof(Component).IsAssignableFrom(_type))
        {
            var _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            return _prefab != null ? _prefab.GetComponentInChildren<T>(true) : null;
        }

        return AssetDatabase.LoadAssetAtPath<T>(assetPath);
    }

    public static string GetFolder(this ScriptableObject so)
    {
        var _assetPath = AssetDatabase.GetAssetPath(so);
        return Path.GetDirectoryName(_assetPath);
    }

    public static T[] LoadAllAddressableAssets<T>() where T : Component
    {
        return LoadAllAddressableAssets<T>(null);
    }

    public static T[] LoadAllAddressableAssets<T>(string folderPath) where T : Component
    {
        var _list = new List<T>();
        var _entries = GetAddressableEntries(typeof(GameObject), folderPath);

        foreach (var _entry in _entries)
        {
            var _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_entry.AssetPath);
            if (_prefab == null) continue;

            var _comps = _prefab.GetComponentsInChildren<T>(true);
            if (_comps != null && _comps.Length > 0)
                _list.AddRange(_comps);
        }

        return _list.ToArray();
    }

    public static AssetReferenceGameObject[] LoadAllAssetReferenceGameObject(string folderPath = null)
    {
        return LoadAllAssetReference<AssetReferenceGameObject>(typeof(GameObject), folderPath);
    }

    public static AssetReferenceGameObject[] LoadAllAssetReferenceGameObject<T>(string folderPath = null) where T : Component
    {
        var _result = new List<AssetReferenceGameObject>();
        var _entries = GetAddressableEntries(typeof(GameObject), folderPath);

        foreach (var _entry in _entries)
        {
            var _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_entry.AssetPath);
            if (_prefab == null) continue;

            if (_prefab.GetComponentInChildren<T>(true) == null)
                continue;

            _result.Add(new AssetReferenceGameObject(_entry.guid));
        }

        return _result.ToArray();
    }

    public static AssetReferenceTexture[] LoadAllAssetReferenceTexture(string folderPath = null)
    {
        return LoadAllAssetReference<AssetReferenceTexture>(typeof(Texture), folderPath);
    }

    public static AssetReferenceTexture2D[] LoadAllAssetReferenceTexture2D(string folderPath = null)
    {
        return LoadAllAssetReference<AssetReferenceTexture2D>(typeof(Texture2D), folderPath);
    }

    public static AssetReferenceSprite[] LoadAllAssetReferenceSprite(string folderPath = null)
    {
        return LoadAllAssetReference<AssetReferenceSprite>(typeof(Sprite), folderPath);
    }

    static T[] LoadAllAssetReference<T>(System.Type mainAssetType, string folderPath = null) where T : AssetReference
    {
        var _result = new List<T>();
        var _entries = GetAddressableEntries(mainAssetType, folderPath);

        foreach (var _entry in _entries)
        {
            var _assetRef = (T)System.Activator.CreateInstance(typeof(T), _entry.guid);
            _result.Add(_assetRef);
        }

        return _result.ToArray();
    }

    static List<AddressableAssetEntry> GetAddressableEntries(System.Type mainAssetType, string folderPath = null)
    {
        var _result = new List<AddressableAssetEntry>();
        var _settings = AddressableAssetSettingsDefaultObject.Settings;

        if (_settings == null)
        {
            Debug.LogError("AddressableAssetSettings tidak ditemukan!");
            return _result;
        }

        var _allEntries = new List<AddressableAssetEntry>();
        _settings.GetAllAssets(_allEntries, true);

        foreach (var _entry in _allEntries)
        {
            if (!string.IsNullOrEmpty(folderPath) && !_entry.AssetPath.StartsWith(folderPath))
                continue;

            if (_entry.MainAssetType != mainAssetType)
                continue;

            _result.Add(_entry);
        }

        return _result;
    }
}

#endif