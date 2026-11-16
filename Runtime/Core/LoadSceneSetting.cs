using System;
using Moimo.Modules;

namespace Moimo.Core
{
    public class LoadSceneSetting
    {
        public BlackscreenType enter = BlackscreenType.Fade;
        public BlackscreenType exit = BlackscreenType.Fade;
        
        public Action<float> onProgress;
        public bool removeAllPopup;
        public Action onComplete;
    }
}