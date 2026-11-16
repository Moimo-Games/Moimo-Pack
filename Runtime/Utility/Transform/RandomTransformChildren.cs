using Sirenix.OdinInspector;
using UnityEngine;

namespace Moimo.Utility
{
    public class RandomTransformChildren : MonoBehaviour
    {
        [SerializeField] private Vector3RangeAxis position;
        [SerializeField] private QuaternionRange rotation;
        [SerializeField] private Vector3RangeAxis scale;

        [Button(ButtonSizes.Medium)]
        private void Randomize()
        {
            foreach (Transform _child in transform)
            {
                if (position.IsUniform)
                {
                    if (position.floatMin != default || position.floatMax != default)
                    {
                        _child.localPosition = position.RandomRange();
                    }
                }
                else
                {
                    if (position.vector3Min != default || position.vector3Max != default)
                    {
                        _child.localPosition = position.RandomRange();
                    }
                }

                if (rotation.min != default || rotation.max != default)
                {
                    _child.localRotation = rotation.RandomRange();
                }

                if (scale.IsUniform)
                {
                    if (scale.floatMin != default || scale.floatMax != default)
                    {
                        _child.localScale = scale.RandomRange();
                    }
                }
                else
                {
                    if (scale.vector3Min != default || scale.vector3Max != default)
                    {
                        _child.localScale = scale.RandomRange();
                    }
                }
            }
        }
    }
}