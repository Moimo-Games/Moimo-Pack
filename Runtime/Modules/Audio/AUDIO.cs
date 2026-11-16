
using System;
using Moimo.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Moimo.Modules
{
    public static class AUDIO
    {
        private static AudioManager manager;

        internal static void Initialize()
        {
            manager = GAME.Get<AudioManager>();
        }
        
        public static float BGMVolume
        {
            get => PlayerPrefs.GetFloat("BGMVolume", 0.7f);
            set
            {
                manager.BGMSource.volume = value;
                PlayerPrefs.SetFloat("BGMVolume", value);
            }
        }
        
        public static float SFXVolume
        {
            get => PlayerPrefs.GetFloat("SFXVolume", 1);
            set
            {
                manager.SFXSource.volume = value;
                PlayerPrefs.SetFloat("SFXVolume", value);
            }
        }
        
        public static void PlayLoop()
        {
            var _source = manager.BGMSource;
            StopLoop(onComplete: () =>
            {
                _source.Play();
                
                _source.gameObject.LeanCancel();
                _source.gameObject.LeanValue(value => _source.volume = value, 0, BGMVolume, 0.5f);
            });
        }
        
        public static void PlayLoop(this AudioClip clip)
        {
            if (clip == null) return;
            var _source = manager.BGMSource;
            if (_source.clip == clip) return;
            
            StopLoop(onComplete: () =>
            {
                _source.clip = clip;
                _source.Play();
                
                _source.gameObject.LeanCancel();
                _source.gameObject.LeanValue(value => _source.volume = value, 0, BGMVolume, 0.5f);
            });
        }

        public static void PlayLoop(this AssetReferenceT<AudioClip> bgm)
        {
            if (bgm == null || !bgm.RuntimeKeyIsValid()) return;
            if (manager.CurrentBGM == bgm && manager.BGMHandle.IsValid()) return;

            ReleaseBGM();

            manager.CurrentBGM = bgm;
            manager.BGMHandle = bgm.LoadAssetAsync();

            manager.BGMHandle.Completed += _handle =>
            {
                if (manager.CurrentBGM != bgm || !_handle.IsValid()) return;
                if (_handle.Result == null) return;

                PlayLoop(_handle.Result);
            };
        }

        private static void ReleaseBGM()
        {
            manager.CurrentBGM = null;

            if (manager.BGMHandle.IsValid())
            {
                Addressables.Release(manager.BGMHandle);
            }

            manager.BGMHandle = default;
        }

        public static void StopLoop(Action onComplete = null)
        {
            var _source = manager.BGMSource;
            if (_source.clip == null)
            {
                onComplete?.Invoke();
                return;
            }
            
            _source.gameObject.LeanCancel();
            _source.gameObject.LeanValue(value => _source.volume = value, _source.volume, 0, 0.5f)
                .setOnComplete(() =>
                {
                    _source.clip = null;
                    onComplete?.Invoke();
                });
        }
        
        public static void PlayOneShot(this AudioClip clip)
        {
            if (clip == null) return;
            manager.SFXSource.PlayOneShot(clip);
        }
        
        public static void PlayOneShot(this AudioClip[] clips)
        {
            if (clips == null) return;
            foreach (var _clip in clips)
            {
                if  (_clip == null) continue;
                manager.SFXSource.PlayOneShot(_clip);
            }
        }
    }
}
