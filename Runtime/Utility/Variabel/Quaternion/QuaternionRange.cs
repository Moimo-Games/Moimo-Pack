using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Moimo.Utility
{
    [Serializable]
    [InlineProperty(LabelWidth = 60), LabelWidth(80)]
    public struct QuaternionRange
    {
        public Quaternion min;
        public Quaternion max;

        public void Set(Quaternion minValue, Quaternion maxValue)
        {
            min = minValue;
            max = maxValue;
        }

        public void Set(Quaternion value)
        {
            min = value;
            max = value;
        }

        public void Add(Quaternion value)
        {
            min *= value;
            max *= value;
        }
    }
}