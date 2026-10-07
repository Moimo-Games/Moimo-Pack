using System;
using System.Collections;
using Moimo.Core;
using UnityEngine;

namespace Moimo.Utility
{
    public static partial class CoroutineUtility
    {
        public static void StartCoroutineLoop(this GameObject key, float repeating, Action callBack)
        {
            if (key == null || callBack == null || GAME.Manager == null) return;
            var _coroutine = ExecuteCoroutine(key, LoopCallbackCoroutine(key, repeating, callBack)); 
            TryAddCoroutine(key, _coroutine);
        }
        public static void StartCoroutineLoop(this GameObject key, float repeating, float startDelay, Action callBack)
        {
            if (key == null || callBack == null || GAME.Manager == null) return;
            var _coroutine = ExecuteCoroutine(key, StartCoroutineDelayed(startDelay, LoopCallbackCoroutine(key, repeating, callBack))); 
            TryAddCoroutine(key, _coroutine);
        }
        private static IEnumerator LoopCallbackCoroutine(GameObject key, float repeating, Action callBack)
        {
            while (true)
            {
                if (key == null)
                {
                    yield break;
                }
            
                callBack?.Invoke();
                yield return new WaitForSeconds(repeating);
            }
        }
    }
}
