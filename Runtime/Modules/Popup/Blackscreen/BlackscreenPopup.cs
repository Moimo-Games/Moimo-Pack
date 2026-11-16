using System;
using System.Collections;
using Moimo.Animation;
using UnityEngine;

namespace Moimo.Modules
{
    public class BlackscreenPopup : UIBehaviour
    {
        [SerializeField] private ValueThis fade;

        public void Fade(FadeType type, bool instant = false, Action onComplete = null)
        {
            if (type == FadeType.In) In(instant, onComplete);
            else Out(instant, onComplete);
        }

        public void In(bool instant = false, Action onComplete = null)
        {
            fade.Set(0, 1, instant, onComplete);
        }

        public void Out(bool instant = false, Action onComplete = null)
        {
            onComplete += Remove;
            fade.Set(1, 0, instant, onComplete);
        }

        public IEnumerator FadeCoroutine(FadeType type, bool instant = false)
        {
            if (type == FadeType.In) yield return InCoroutine(instant);
            else yield return OutCoroutine(instant);
        }

        public IEnumerator InCoroutine(bool instant = false)
        {
            var _complete = false;
            In(instant, () => _complete = true);

            while (!_complete) yield return null;
        }

        public IEnumerator OutCoroutine(bool instant = false)
        {
            var _complete = false;
            Out(instant, () => _complete = true);

            while (!_complete) yield return null;
        }
    }
}