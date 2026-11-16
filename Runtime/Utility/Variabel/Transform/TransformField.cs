using System;
using UnityEngine;

namespace Moimo.Utility
{
    [Serializable]
    public struct TransformField
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public TransformField(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public TransformField(Vector3 position, Vector3 eulerAngles, Vector3 scale)
        {
            this.position = position;
            rotation = Quaternion.Euler(eulerAngles);
            this.scale = scale;
        }

        public TransformField(Vector3 scale)
        {
            position = default;
            rotation = default;
            this.scale = scale;
        }

        public TransformField(Transform transform)
        {
            position = transform.position;
            rotation = transform.rotation;
            scale = transform.localScale;
        }

        public Vector3 eulerAngles
        {
            set => rotation = Quaternion.Euler(value);
            get => rotation.eulerAngles;
        }

        public void ApplyTo(Transform target)
        {
            target.SetPositionAndRotation(position, rotation);
            target.localScale = scale;
        }

        public static implicit operator TransformField(Transform t)
            => new(t.position, t.rotation, t.localScale);
    }
}