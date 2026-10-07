using System;

namespace Moimo.Core
{
    public class LoadSceneSetting
    {
        public bool fadeInInstant;
        public bool fadeOutInstant;
        
        public Action<float> onProgress;
        public bool removeAllPopup;
        public Action onComplete;
    }
}