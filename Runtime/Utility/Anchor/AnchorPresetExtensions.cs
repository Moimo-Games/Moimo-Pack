
using UnityEngine;

namespace Moimo.Utility
{
    public static class AnchorPresetExtensions
    {
        private static readonly Vector2[] AnchorMin =
        {
            new(0f, 1f),
            new(0.5f, 1f),
            new(1f, 1f),
            new(0f, 1f),

            new(0f, 0.5f),
            new(0.5f, 0.5f),
            new(1f, 0.5f),
            new(0f, 0.5f),

            new(0f, 0f),
            new(0.5f, 0f),
            new(1f, 0f),
            new(0f, 0f),

            new(0f, 0f),
            new(0.5f, 0f),
            new(1f, 0f),
            new(0f, 0f)
        };

        private static readonly Vector2[] AnchorMax =
        {
            new(0f, 1f),
            new(0.5f, 1f),
            new(1f, 1f),
            new(1f, 1f),

            new(0f, 0.5f),
            new(0.5f, 0.5f),
            new(1f, 0.5f),
            new(1f, 0.5f),

            new(0f, 0f),
            new(0.5f, 0f),
            new(1f, 0f),
            new(1f, 0f),

            new(0f, 1f),
            new(0.5f, 1f),
            new(1f, 1f),
            new(1f, 1f)
        };

        public static void SetAnchor(this RectTransform rect, AnchorPreset preset)
        {
            int index = (int)preset - 1;

            if (index < 0 || index >= AnchorMin.Length)
                return;

            var min = AnchorMin[index];
            var max = AnchorMax[index];

            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = (min + max) * 0.5f;
        }

        public static AnchorPreset ToAnchorPreset(this RectTransform rect)
        {
            var min = rect.anchorMin;
            var max = rect.anchorMax;

            if (min == Vector2.zero && max == Vector2.one)
                return AnchorPreset.Stretch;

            if (min.x == max.x)
            {
                if (min.y == max.y)
                {
                    if (min.x != 0f && min.x != 0.5f && min.x != 1f)
                        return AnchorPreset.None;

                    if (min.y != 0f && min.y != 0.5f && min.y != 1f)
                        return AnchorPreset.None;

                    return (AnchorPreset)(1 + (int)((1f - min.y) * 2f) * 4 + (int)(min.x * 2f));
                }

                if (min.y == 0f && max.y == 1f)
                {
                    if (min.x != 0f && min.x != 0.5f && min.x != 1f)
                        return AnchorPreset.None;

                    return (AnchorPreset)(13 + (int)(min.x * 2f));
                }
            }
            else if (min.y == max.y)
            {
                if (min.x == 0f && max.x == 1f)
                {
                    if (min.y != 0f && min.y != 0.5f && min.y != 1f)
                        return AnchorPreset.None;

                    return (AnchorPreset)(4 + (int)((1f - min.y) * 2f) * 4);
                }
            }

            return AnchorPreset.None;
        }

        public static void SetAnchorTopLeft(this RectTransform rect) => rect.SetAnchor(AnchorPreset.TopLeft);
        public static void SetAnchorTopCenter(this RectTransform rect) => rect.SetAnchor(AnchorPreset.TopCenter);
        public static void SetAnchorTopRight(this RectTransform rect) => rect.SetAnchor(AnchorPreset.TopRight);
        public static void SetAnchorTopStretch(this RectTransform rect) => rect.SetAnchor(AnchorPreset.TopStretch);
        public static void SetAnchorStretchTop(this RectTransform rect) => rect.SetAnchor(AnchorPreset.StretchTop);

        public static void SetAnchorMiddleLeft(this RectTransform rect) => rect.SetAnchor(AnchorPreset.MiddleLeft);
        public static void SetAnchorMiddleCenter(this RectTransform rect) => rect.SetAnchor(AnchorPreset.MiddleCenter);
        public static void SetAnchorMiddleRight(this RectTransform rect) => rect.SetAnchor(AnchorPreset.MiddleRight);
        public static void SetAnchorMiddleStretch(this RectTransform rect) => rect.SetAnchor(AnchorPreset.MiddleStretch);
        public static void SetAnchorStretchMiddle(this RectTransform rect) => rect.SetAnchor(AnchorPreset.StretchMiddle);

        public static void SetAnchorBottomLeft(this RectTransform rect) => rect.SetAnchor(AnchorPreset.BottomLeft);
        public static void SetAnchorBottomCenter(this RectTransform rect) => rect.SetAnchor(AnchorPreset.BottomCenter);
        public static void SetAnchorBottomRight(this RectTransform rect) => rect.SetAnchor(AnchorPreset.BottomRight);
        public static void SetAnchorBottomStretch(this RectTransform rect) => rect.SetAnchor(AnchorPreset.BottomStretch);
        public static void SetAnchorStretchBottom(this RectTransform rect) => rect.SetAnchor(AnchorPreset.StretchBottom);

        public static void SetAnchorStretchLeft(this RectTransform rect) => rect.SetAnchor(AnchorPreset.StretchLeft);
        public static void SetAnchorStretchCenter(this RectTransform rect) => rect.SetAnchor(AnchorPreset.StretchCenter);
        public static void SetAnchorStretchRight(this RectTransform rect) => rect.SetAnchor(AnchorPreset.StretchRight);
        public static void SetAnchorStretch(this RectTransform rect) => rect.SetAnchor(AnchorPreset.Stretch);
    }
}
