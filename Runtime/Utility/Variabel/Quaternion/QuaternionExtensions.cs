using UnityEngine;

namespace Moimo.Utility
{
    public static class QuaternionExtensions
    {
        public static Quaternion RandomRange(this QuaternionRange range)
        {
            var _min = range.min.eulerAngles;
            var _max = range.max.eulerAngles;

            return Quaternion.Euler(
                Random.Range(_min.x, _max.x),
                Random.Range(_min.y, _max.y),
                Random.Range(_min.z, _max.z)
            );
        }

        public static Quaternion RandomSign(this QuaternionRange range)
        {
            var _min = range.min.eulerAngles;
            var _max = range.max.eulerAngles;

            return Quaternion.Euler(
                Random.value > 0.5f ? _min.x : _max.x,
                Random.value > 0.5f ? _min.y : _max.y,
                Random.value > 0.5f ? _min.z : _max.z
            );
        }

        public static Quaternion RandomNonCenter(this QuaternionRange range, float excludePercent)
        {
            var _min = range.min.eulerAngles;
            var _max = range.max.eulerAngles;

            var _minExclude = _min * excludePercent;
            var _maxExclude = _max * excludePercent;

            return Quaternion.Euler(
                Random.value > 0.5f ? Random.Range(_minExclude.x, _min.x) : Random.Range(_maxExclude.x, _max.x),
                Random.value > 0.5f ? Random.Range(_minExclude.y, _min.y) : Random.Range(_maxExclude.y, _max.y),
                Random.value > 0.5f ? Random.Range(_minExclude.z, _min.z) : Random.Range(_maxExclude.z, _max.z)
            );
        }
    }
}