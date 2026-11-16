using UnityEngine;

namespace Moimo.Utility
{
    public static class Vector3Extensions
    {
        public static Vector3 RandomRange(this Vector3Range range)
        {
            return new Vector3(
                Random.Range(range.min.x, range.max.x),
                Random.Range(range.min.y, range.max.y),
                Random.Range(range.min.z, range.max.z)
            );
        }

        public static Vector3 RandomSign(this Vector3Range range)
        {
            return new Vector3(
                Random.value > 0.5f ? range.min.x : range.max.x,
                Random.value > 0.5f ? range.min.y : range.max.y,
                Random.value > 0.5f ? range.min.z : range.max.z
            );
        }

        public static Vector3 RandomNonCenter(this Vector3Range range, float excludePercent)
        {
            var _minExclude = new Vector3(
                range.min.x * excludePercent,
                range.min.y * excludePercent,
                range.min.z * excludePercent);

            var _maxExclude = new Vector3(
                range.max.x * excludePercent,
                range.max.y * excludePercent,
                range.max.z * excludePercent);

            return new Vector3(
                Random.value > 0.5f ? Random.Range(_minExclude.x, range.min.x) : Random.Range(_maxExclude.x, range.max.x),
                Random.value > 0.5f ? Random.Range(_minExclude.y, range.min.y) : Random.Range(_maxExclude.y, range.max.y),
                Random.value > 0.5f ? Random.Range(_minExclude.z, range.min.z) : Random.Range(_maxExclude.z, range.max.z)
            );
        }

        public static Vector3 RandomRange(this Vector3RangeAxis range)
        {
            if (range.IsUniform)
            {
                return Vector3.one * Random.Range(range.floatMin, range.floatMax);
            }

            return new Vector3(
                Random.Range(range.vector3Min.x, range.vector3Max.x),
                Random.Range(range.vector3Min.y, range.vector3Max.y),
                Random.Range(range.vector3Min.z, range.vector3Max.z)
            );
        }

        public static Vector3 RandomSign(this Vector3RangeAxis range)
        {
            if (range.IsUniform)
            {
                return Vector3.one * (Random.value > 0.5f ? range.floatMin : range.floatMax);
            }

            return new Vector3(
                Random.value > 0.5f ? range.vector3Min.x : range.vector3Max.x,
                Random.value > 0.5f ? range.vector3Min.y : range.vector3Max.y,
                Random.value > 0.5f ? range.vector3Min.z : range.vector3Max.z
            );
        }

        public static Vector3 RandomNonCenter(this Vector3RangeAxis range, float excludePercent)
        {
            if (range.IsUniform)
            {
                float _minExclude = range.floatMin * excludePercent;
                float _maxExclude = range.floatMax * excludePercent;

                float _random = Random.value > 0.5f
                    ? Random.Range(_minExclude, range.floatMin)
                    : Random.Range(_maxExclude, range.floatMax);

                return Vector3.one * _random;
            }

            var _minExcludeVector = range.vector3Min * excludePercent;
            var _maxExcludeVector = range.vector3Max * excludePercent;

            return new Vector3(
                Random.value > 0.5f ? Random.Range(_minExcludeVector.x, range.vector3Min.x) : Random.Range(_maxExcludeVector.x, range.vector3Max.x),
                Random.value > 0.5f ? Random.Range(_minExcludeVector.y, range.vector3Min.y) : Random.Range(_maxExcludeVector.y, range.vector3Max.y),
                Random.value > 0.5f ? Random.Range(_minExcludeVector.z, range.vector3Min.z) : Random.Range(_maxExcludeVector.z, range.vector3Max.z)
            );
        }
    }
}