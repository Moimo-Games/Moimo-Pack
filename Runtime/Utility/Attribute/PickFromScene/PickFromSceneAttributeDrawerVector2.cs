#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Moimo.Attribute
{
    public class PickFromSceneAttributeDrawerVector2 : OdinAttributeDrawer<PickFromSceneAttribute, Vector2>
    {
        private static readonly List<PickFromSceneAttributeDrawerVector2> ActiveDrawers = new List<PickFromSceneAttributeDrawerVector2>();
        private static bool sceneGuiRegistered;

        private string label;
        private Vector2 current;
        private GUIStyle buttonStyle;
        private bool showHandles;
        private bool isDragging;
        private bool registered;
        private bool showToggle;
        private Color handleColor = Color.white;
        private int drawnForUpdateId = -1;

        protected override void Initialize()
        {
            label = Property.NiceName.ToTitleCase();
            current = ValueEntry.SmartValue;
            isDragging = false;
            buttonStyle = new GUIStyle(GUI.skin.button);
            drawnForUpdateId = -1;

            showToggle = Attribute.ShowToggle;
            showHandles = Attribute.DefaultShowHandles;
            handleColor = ParseColor(Attribute.HandleColor);

            RegisterDrawer();
        }

        private static string ShortPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            var parts = path.Replace("$", "").Split('.');
            var filtered = parts
                .Where(p => !string.IsNullOrEmpty(p) && !p.StartsWith("#") && !p.StartsWith("{"))
                .ToList();
            if (filtered.Count > 4)
                filtered = filtered.Skip(filtered.Count - 4).ToList();
            return string.Join(".", filtered);
        }

        private static Color ParseColor(string colorStr)
        {
            if (string.IsNullOrEmpty(colorStr)) return Color.white;

            if (!colorStr.StartsWith("#") && (colorStr.Length == 6 || colorStr.Length == 8))
            {
                if (ColorUtility.TryParseHtmlString("#" + colorStr, out var hexNoHash))
                    return hexNoHash;
            }

            if (ColorUtility.TryParseHtmlString(colorStr, out var hex))
                return hex;

            switch (colorStr.ToLowerInvariant())
            {
                case "red": return Color.red;
                case "green": return Color.green;
                case "blue": return Color.blue;
                case "yellow": return Color.yellow;
                case "cyan": return Color.cyan;
                case "magenta": return Color.magenta;
                case "white": return Color.white;
                case "black": return Color.black;
                case "gray":
                case "grey": return Color.gray;
                case "orange": return new Color(1f, 0.5f, 0f);
                case "purple": return new Color(0.5f, 0f, 0.5f);
                case "clear":
                case "transparent": return Color.clear;
                default: return Color.white;
            }
        }

        private void RegisterDrawer()
        {
            if (registered) return;

            for (int i = ActiveDrawers.Count - 1; i >= 0; i--)
            {
                var other = ActiveDrawers[i];
                if (other == null)
                {
                    ActiveDrawers.RemoveAt(i);
                    continue;
                }
                if (other == this) continue;
                try
                {
                    if (other.Property != null && Property != null && other.Property.Path == Property.Path)
                        other.UnregisterDrawer();
                }
                catch
                {
                    other.UnregisterDrawer();
                }
            }

            registered = true;
            ActiveDrawers.Add(this);
            if (!sceneGuiRegistered)
            {
                SceneView.duringSceneGui += StaticOnSceneGUI;
                sceneGuiRegistered = true;
            }
        }

        private void UnregisterDrawer()
        {
            if (!registered) return;
            registered = false;
            ActiveDrawers.Remove(this);
            showHandles = false;
            isDragging = false;
            if (ActiveDrawers.Count == 0 && sceneGuiRegistered)
            {
                SceneView.duringSceneGui -= StaticOnSceneGUI;
                sceneGuiRegistered = false;
            }
        }

        private static void StaticOnSceneGUI(SceneView sceneView)
        {
            for (int i = ActiveDrawers.Count - 1; i >= 0; i--)
            {
                var drawer = ActiveDrawers[i];
                if (drawer == null || !drawer.IsPropertyValid())
                {
                    if (drawer != null) drawer.UnregisterDrawer();
                    else ActiveDrawers.RemoveAt(i);
                    continue;
                }
                drawer.DrawSceneHandles();
            }
        }

        private bool IsPropertyValid()
        {
            try
            {
                if (Property == null) return false;
                if (Property.Tree == null) return false;
                var so = Property.Tree.UnitySerializedObject;
                if (so != null && so.targetObject == null) return false;
                if (!Property.IsReachableFromRoot()) return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool IsVisibleInInspector()
        {
            try
            {
                if (Property == null || Property.Tree == null)
                    return false;

                for (var p = Property.Parent; p != null; p = p.Parent)
                {
                    if (p.State == null)
                        continue;

                    bool isFoldout = p.GetAttribute<FoldoutGroupAttribute>() != null;
                    bool isCollection = p.ChildResolver is ICollectionResolver;

                    if ((isFoldout || isCollection) && !p.State.Expanded)
                        return false;
                }

                return drawnForUpdateId == Property.Tree.UpdateID;
            }
            catch
            {
                return true;
            }
        }

        private void DrawSceneHandles()
        {
            if (!showHandles)
            {
                isDragging = false;
                return;
            }

            if (!isDragging && !IsVisibleInInspector())
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            var handlePosition = Handles.PositionHandle(ValueEntry.SmartValue, Quaternion.identity);

            var drawLabel = string.IsNullOrEmpty(Attribute.Label) ? ShortPath(Property.Path) : label;
            var cam = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.camera : null;
            if (cam != null)
            {
                var offset = -cam.transform.up * HandleUtility.GetHandleSize(handlePosition) * 0.2f;
                Handles.color = handleColor;
                Handles.Label(handlePosition + offset, drawLabel, buttonStyle);
            }

            if (EditorGUI.EndChangeCheck())
            {
                isDragging = true;
                ValueEntry.SmartValue = handlePosition;
                current = handlePosition;
                try { ValueEntry.ApplyChanges(); }
                catch { UnregisterDrawer(); }
            }
            else if (GUIUtility.hotControl == 0)
            {
                isDragging = false;
            }
        }

        protected override void DrawPropertyLayout(GUIContent content)
        {
            if (!registered) RegisterDrawer();
            try { drawnForUpdateId = Property.Tree.UpdateID; } catch { drawnForUpdateId = -1; }

            GUILayout.BeginHorizontal();

            if (!string.IsNullOrEmpty(Attribute.Label))
                label = Attribute.Label;
            else
                label = content != null ? content.text : Property.NiceName;

            var value = EditorGUILayout.Vector2Field(label, ValueEntry.SmartValue);
            if (current != value)
            {
                ValueEntry.SmartValue = value;
                current = value;
                SceneView.RepaintAll();
                ValueEntry.ApplyChanges();
            }

            if (SirenixEditorGUI.IconButton(EditorIcons.Flag, buttonStyle))
                SetPositionToCurrentSceneViewFrame();

            if (SirenixEditorGUI.IconButton(EditorIcons.MagnifyingGlass, buttonStyle))
                SetFramePosition(current);

            if (showToggle)
            {
                if (SirenixEditorGUI.IconButton(showHandles ? EditorIcons.Checkmark : EditorIcons.X, buttonStyle))
                {
                    showHandles = !showHandles;
                    isDragging = false;
                    SceneView.RepaintAll();
                }
            }

            GUILayout.EndHorizontal();
        }

        private void SetPositionToCurrentSceneViewFrame()
        {
            if (SceneView.lastActiveSceneView == null || SceneView.lastActiveSceneView.camera == null) return;
            current = SceneView.lastActiveSceneView.camera.transform.position;
            ValueEntry.SmartValue = current;
            SceneView.RepaintAll();
            ValueEntry.ApplyChanges();
        }

        private void SetFramePosition(Vector2 position)
        {
            if (SceneView.lastActiveSceneView == null) return;
            SceneView.lastActiveSceneView.Frame(new Bounds(position, Vector3.one * 10), false);
        }

        ~PickFromSceneAttributeDrawerVector2()
        {
            UnregisterDrawer();
        }
    }
}

#endif
