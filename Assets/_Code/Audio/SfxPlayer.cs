using System;
using Awen.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace Awen.Audio {
  /// <summary>
  /// 挂在「音效预制体」上：自带一个 AudioSource。由 <see cref="AudioManager"/> 从对象池取出后，
  /// 调 <see cref="PlayClip"/> 播放，播放时长结束后自动归还对象池。
  /// （查表/加载在 AudioManager 完成，本组件只负责播放与回收。）
  /// </summary>
  [RequireComponent(typeof(AudioSource))]
  public class SfxPlayer : MonoBehaviour, IPoolable {
    private AudioSource _source;

    private void Awake() => _source = GetComponent<AudioSource>();

    void IPoolable.OnSpawn() { }

    void IPoolable.OnDespawn() {
      if (_source != null) {
        _source.Stop();
        _source.clip = null;
      }
    }

    /// <summary>播放给定 clip，播完自动归还对象池。</summary>
    public void PlayClip(AudioClip clip, AudioMixerGroup group, float volume) {
      if (clip == null) {
        PoolManager.Instance.Release(gameObject);
        return;
      }
      _source.outputAudioMixerGroup = group;
      _source.clip = clip;
      _source.volume = volume;
      _source.Play();
      AutoReleaseAsync(clip.length).Forget();
    }

    private async UniTaskVoid AutoReleaseAsync(float seconds) {
      try {
        await UniTask.Delay(
            TimeSpan.FromSeconds(seconds), cancellationToken: this.GetCancellationTokenOnDestroy());
      } catch (OperationCanceledException) {
        return;
      }
      if (this != null) {
        PoolManager.Instance.Release(gameObject);
      }
    }
  }
}
