using Sirenix.OdinInspector;
using UnityEngine;

namespace Moimo.Editor.BakedScene
{
    [System.Serializable]
    public class SceneBakeItem
    {
        [TableColumnWidth(28, Resizable = false)]
        [LabelText("")]
        public bool selected;

        [ReadOnly, TableColumnWidth(220)]
        public string path;

        [ReadOnly, TableColumnWidth(140)]
        public string name;

        [ReadOnly, TableColumnWidth(90)]
        public string status = "Waiting";

        [ReadOnly, TableColumnWidth(70)]
        public bool hasLightingData;

        [ReadOnly, TableColumnWidth(70)]
        public bool hasOcclusionData;
    }
}
