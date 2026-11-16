using UnityEngine;

namespace Moimo.Utility
{
    public enum AnchorPreset
    {
        None,
        
        TopLeft,
        TopCenter,
        TopRight,
        TopStretch,
        StretchTop = TopStretch,

        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        MiddleStretch,
        StretchMiddle = MiddleStretch,

        BottomLeft,
        BottomCenter,
        BottomRight,
        BottomStretch,
        StretchBottom = BottomStretch,

        StretchLeft,
        StretchCenter,
        StretchRight,
        Stretch
    }
}
