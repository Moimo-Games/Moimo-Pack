using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Moimo.Utility
{
    [Serializable]
    [InlineProperty(LabelWidth = 60), LabelWidth(80)]
    public struct Vector3RangeAxis
    {
        [ShowIf("IsSeparate")] [LabelText("Min")]
        public Vector3 vector3Min;
        [ShowIf("IsSeparate")] [LabelText("Max")]
        public Vector3 vector3Max;

        [ShowIf("IsUniform")] [LabelText("Min")]
        public float floatMin;
        [ShowIf("IsUniform")] [LabelText("Max")]
        public float floatMax;

        public RandomAxisMode mode;

        public bool IsUniform => mode == RandomAxisMode.Uniform;
        public bool IsSeparate => mode == RandomAxisMode.Separate;

        public void Set(Vector3 minValue, Vector3 maxValue)
        {
            vector3Min = minValue;
            vector3Max = maxValue;
        }

        public void Set(Vector3 value)
        {
            vector3Min = value;
            vector3Max = value;
        }

        public void Add(Vector3 value)
        {
            vector3Min += value;
            vector3Max += value;
        }

        public void Set(float minValue, float maxValue)
        {
            floatMin = minValue;
            floatMax = maxValue;
        }

        public void Set(float value)
        {
            floatMin = value;
            floatMax = value;
        }

        public void Add(float value)
        {
            floatMin += value;
            floatMax += value;
        }
    }
}