using System;
using UnityEngine;

namespace Moimo.Attribute
{
    [AttributeUsage(AttributeTargets.Field)]
    public class PickFromSceneAttribute : System.Attribute
    {
        public PickFromSceneAttribute()
        {
            Label = "";
            ShowToggle = true;
            DefaultShowHandles = true;
            HandleColor = "white";
        }
        public PickFromSceneAttribute(bool showToggle = true)
        {
            Label = "";
            ShowToggle = showToggle;
            DefaultShowHandles = !showToggle;
            HandleColor = "white";
        }
        public PickFromSceneAttribute(
            string label = "",
            bool showToggle = true,
            bool defaultShowHandles = false,
            string handleColor = "white")
        {
            Label = label;
            ShowToggle = showToggle;
            DefaultShowHandles = defaultShowHandles;
            HandleColor = handleColor;
        }

        public string Label { get; }
        public bool ShowToggle { get; }
        public bool DefaultShowHandles { get; }
        public string HandleColor { get; }
    }
}
