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
using Pose = Moimo.Field.Pose;

namespace Moimo.Attribute
{
    public class PickFromSceneAttributeDrawerPose : OdinAttributeDrawer<PickFromSceneAttribute, Pose>
    {
        private static readonly List<PickFromSceneAttributeDrawerPose> ActiveDrawers = new List<PickFromSceneAttributeDrawerPose>();
        private static bool sceneGuiRegistered;

        private string label;
        private Pose current;
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
            if (!string.IsNullOrEmpty(Attribute.Label))
                label = Attribute.Label;
            else
                label = ShortPath(Property.Path);

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

            var pose = ValueEntry.SmartValue;

            EditorGUI.BeginChangeCheck();
            var position = Handles.PositionHandle(pose.position, pose.rotation);
            var rotation = Handles.RotationHandle(pose.rotation, position);

            if (EditorGUI.EndChangeCheck())
            {
                isDragging = true;
                pose.position = position;
                pose.rotation = rotation;
                ValueEntry.SmartValue = pose;
                current = pose;
                try { ValueEntry.ApplyChanges(); }
                catch { UnregisterDrawer(); }
            }
            else if (GUIUtility.hotControl == 0)
            {
                isDragging = false;
            }

            DrawVisual(pose);
        }

        protected override void DrawPropertyLayout(GUIContent content)
        {
            if (!registered) RegisterDrawer();
            try { drawnForUpdateId = Property.Tree.UpdateID; } catch { drawnForUpdateId = -1; }

            var pose = ValueEntry.SmartValue;

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            pose.position = EditorGUILayout.Vector3Field("Position", pose.position);
            pose.eulerAngles = EditorGUILayout.Vector3Field("Rotation", pose.eulerAngles);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(24));
            GUILayout.Space(2);

            if (SirenixEditorGUI.IconButton(EditorIcons.Flag, buttonStyle))
                SetPositionToCurrentSceneViewFrame();

            if (SirenixEditorGUI.IconButton(EditorIcons.MagnifyingGlass, buttonStyle))
                SetFramePosition(pose.position);

            if (showToggle)
            {
                if (SirenixEditorGUI.IconButton(showHandles ? EditorIcons.Checkmark : EditorIcons.X, buttonStyle))
                {
                    showHandles = !showHandles;
                    isDragging = false;
                    SceneView.RepaintAll();
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (current.position != pose.position || current.rotation != pose.rotation)
            {
                ValueEntry.SmartValue = pose;
                current = pose;
                SceneView.RepaintAll();
                ValueEntry.ApplyChanges();
            }
        }

        private void DrawVisual(Pose pose)
        {
            var size = HandleUtility.GetHandleSize(pose.position) * 0.8f;
            Handles.color = handleColor;
            Handles.ArrowHandleCap(0, pose.position, pose.rotation, size, EventType.Repaint);

            var cam = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.camera : null;
            if (cam == null) return;
            var offset = -cam.transform.up * HandleUtility.GetHandleSize(pose.position) * 0.2f;
            var drawLabel = string.IsNullOrEmpty(Attribute.Label) ? label : Attribute.Label;
            Handles.Label(pose.position + offset, drawLabel, buttonStyle);
        }

        private void SetPositionToCurrentSceneViewFrame()
        {
            if (SceneView.lastActiveSceneView == null || SceneView.lastActiveSceneView.camera == null) return;
            var pose = ValueEntry.SmartValue;
            pose.position = SceneView.lastActiveSceneView.camera.transform.position;
            pose.rotation = SceneView.lastActiveSceneView.camera.transform.rotation;
            ValueEntry.SmartValue = pose;
            current = pose;
            SceneView.RepaintAll();
            ValueEntry.ApplyChanges();
        }

        private void SetFramePosition(Vector3 position)
        {
            if (SceneView.lastActiveSceneView == null) return;
            SceneView.lastActiveSceneView.Frame(new Bounds(position, Vector3.one * 10), false);
        }

        ~PickFromSceneAttributeDrawerPose()
        {
            UnregisterDrawer();
        }
    }
}

#endif
