using UnityEngine;

namespace Moimo.Utility
{
    public static class ClipboardExtension
    {
        public static void CopyToClipboard(this string str)
        {
            GUIUtility.systemCopyBuffer = str;
        }
    }
}
