using System;
using System.Threading;
using Awen.Core;
using cfg.Audio;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace Awen.Audio {
  /// <summary>音量通道，对应 AudioMixer 的暴露参数（&lt;Channel&gt;Volume）与混音组。</summary>
  public enum AudioChannel { Master, Music, SFX, Voice }

  /// <summary>
  /// 音频管理器。通过 <b>AudioMixer</b> 控制音量；通过 <b>TbAudio 配置表</b>（id/asset/volume/loop/category）
  /// 驱动播放。统一入口 Play(AudioId)/Play(int) 按名字前缀（BGM_/SFX_/Ambient_/Voice_）自动路由：
  /// BGM（双源交叉淡入淡出，路由 Music 组）、Ambient（环境音，Music 组）、SFX（对象池预制体）、Voice。
  /// 资源地址按约定 <c>Audio/{category}/{asset}</c> 推导。
  /// </summary>
  public sealed class AudioManager : MonoSingleton<AudioManager> {
    private const float _defaultFade = 1f;
    private const string _sfxPrefabAddress = ResAddress.Prefabs_Audio_SfxPlayer;

    [SerializeField]
    private AudioMixer _mixer;

    private AudioSource _bgmA, _bgmB;
    private bool _bgmUsingA;
    private AudioSource _ambient;
    private AudioSource _voice;
    private AudioMixerGroup _sfxGroup;

    private CancellationTokenSource _bgmCts;
    private CancellationTokenSource _ambientCts;

    protected override void OnAwake() {
      var music = Group("Music");
      _sfxGroup = Group("SFX");
      var voiceGroup = Group("Voice");

      _bgmA = CreateSource("BGM A", true, music);
      _bgmB = CreateSource("BGM B", true, music);
      _ambient = CreateSource("Ambient", true, music);
      _voice = CreateSource("Voice", false, voiceGroup);

      ApplySavedVolumes();
    }

    protected override void OnDestroyed() {
      _bgmCts?.Cancel();
      _bgmCts?.Dispose();
      _ambientCts?.Cancel();
      _ambientCts?.Dispose();
    }

    private AudioMixerGroup Group(string name) {
      if (_mixer == null) {
        Debug.LogWarning("[AudioManager] 未指定 AudioMixer，音量控制将不可用。");
        return null;
      }
      var groups = _mixer.FindMatchingGroups(name);
      return groups != null && groups.Length > 0 ? groups[0] : null;
    }

    private AudioSource CreateSource(string name, bool loop, AudioMixerGroup group) {
      var go = new GameObject(name);
      go.transform.SetParent(transform, false);
      var src = go.AddComponent<AudioSource>();
      src.playOnAwake = false;
      src.loop = loop;
      src.spatialBlend = 0f;  // 2D
      src.outputAudioMixerGroup = group;
      return src;
    }

    // ── 统一入口（按配置表 + 类别路由） ────────────────────────

    /// <summary>按音频 ID 播放（即发即忘）。最常用：AudioManager.Instance.Play(AudioId.BGM_MainMenu)。</summary>
    public void Play(AudioId id) => PlayAsync(id).Forget();

    /// <summary>按 int ID 播放（即发即忘）。等价于 Play((AudioId)id)，便于按数值调用。</summary>
    public void Play(int id) => PlayAsync((AudioId)id).Forget();

    /// <summary>按音频 ID 播放并可 await：查 TbAudio，按 category 自动路由，应用 volume/loop。</summary>
    public async UniTask PlayAsync(AudioId id) {
      var e = Entry(id);
      if (e == null) {
        Debug.LogWarning($"[AudioManager] 音频表中无 ID：{id}");
        return;
      }

      string prefix = Prefix(id);
      string address = "Audio/" + prefix + "/" + e.Asset;
      switch (prefix) {
        case "BGM":
          await PlayBGMAsync(address, _defaultFade, e.Loop, e.Volume);
          break;
        case "Ambient":
          await PlayAmbientAsync(address, _defaultFade, e.Loop, e.Volume);
          break;
        case "Voice":
          await PlayVoiceAsync(address, e.Volume);
          break;
        default:
          await PlaySfxAsync(address, e.Volume);  // SFX 及其它
          break;
      }
    }

    private static AudioEntry Entry(AudioId id) => ConfigManager.Tables?.TbAudio?.GetOrDefault(id);

    /// <summary>枚举名的下划线前缀（BGM_MainMenu → "BGM"），决定类别与资源子目录。</summary>
    private static string Prefix(AudioId id) {
      string n = id.ToString();
      int i = n.IndexOf('_');
      return i < 0 ? n : n.Substring(0, i);
    }

    /// <summary>资源地址约定：Audio/{前缀}/{asset}，如 BGM_MainMenu + MainMenuBGM → "Audio/BGM/MainMenuBGM"。</summary>
    public static string Address(AudioEntry e) => "Audio/" + Prefix(e.Id) + "/" + e.Asset;

    // ── BGM（交叉淡入淡出，淡入目标 = 配置音量） ────────────────

    public async UniTask PlayBGMAsync(
        string address, float fade = _defaultFade, bool loop = true, float volume = 1f) {
      var clip = await ResourceManager.Instance.LoadAsync<AudioClip>(address);
      if (clip != null) {
        await PlayBGMAsync(clip, fade, loop, volume);
      }
    }

    public async UniTask PlayBGMAsync(
        AudioClip clip, float fade = _defaultFade, bool loop = true, float volume = 1f) {
      if (clip == null) {
        return;
      }

      _bgmCts?.Cancel();
      _bgmCts?.Dispose();
      _bgmCts = CancellationTokenSource.CreateLinkedTokenSource(
          this.GetCancellationTokenOnDestroy());
      var ct = _bgmCts.Token;

      var incoming = _bgmUsingA ? _bgmB : _bgmA;
      var outgoing = _bgmUsingA ? _bgmA : _bgmB;
      _bgmUsingA = !_bgmUsingA;

      incoming.clip = clip;
      incoming.loop = loop;
      incoming.volume = 0f;
      incoming.Play();

      try {
        await UniTask.WhenAll(
            FadeVolume(incoming, volume, fade, ct), FadeVolume(outgoing, 0f, fade, ct));
      } catch (OperationCanceledException) {
        return;
      }

      outgoing.Stop();
      outgoing.clip = null;
    }

    public void StopBGM(float fade = _defaultFade) {
      _bgmCts?.Cancel();
      FadeOutAndStop(_bgmUsingA ? _bgmA : _bgmB, fade).Forget();
    }

    // ── Ambient（环境音） ───────────────────────────────────────

    public async UniTask PlayAmbientAsync(
        string address, float fade = _defaultFade, bool loop = true, float volume = 1f) {
      var clip = await ResourceManager.Instance.LoadAsync<AudioClip>(address);
      if (clip == null) {
        return;
      }

      _ambientCts?.Cancel();
      _ambientCts?.Dispose();
      _ambientCts = CancellationTokenSource.CreateLinkedTokenSource(
          this.GetCancellationTokenOnDestroy());
      var ct = _ambientCts.Token;

      try {
        if (_ambient.isPlaying) {
          await FadeVolume(_ambient, 0f, fade * 0.5f, ct);
        }
        _ambient.clip = clip;
        _ambient.loop = loop;
        _ambient.volume = 0f;
        _ambient.Play();
        await FadeVolume(_ambient, volume, fade, ct);
      } catch (OperationCanceledException) {
      }
    }

    public void StopAmbient(float fade = _defaultFade) {
      _ambientCts?.Cancel();
      FadeOutAndStop(_ambient, fade).Forget();
    }

    // ── SFX（对象池预制体，播完自动回收） ──────────────────────

    /// <summary>直接播放给定 clip（走对象池预制体）。</summary>
    public void PlaySFX(AudioClip clip, float volume = 1f) => PlaySfxClipAsync(clip, volume).Forget();

    private async UniTask PlaySfxAsync(string address, float volume) {
      var clip = await ResourceManager.Instance.LoadAsync<AudioClip>(address);
      await PlaySfxClipAsync(clip, volume);
    }

    private async UniTask PlaySfxClipAsync(AudioClip clip, float volume) {
      if (clip == null) {
        return;
      }
      var prefab = await ResourceManager.Instance.LoadAsync<GameObject>(_sfxPrefabAddress);
      if (prefab == null) {
        return;
      }
      var go = PoolManager.Instance.Get(prefab, Vector3.zero, Quaternion.identity);
      var player = go.GetComponent<SfxPlayer>();
      if (player != null) {
        player.PlayClip(clip, _sfxGroup, volume);
      } else {
        PoolManager.Instance.Release(go);
      }
    }

    // ── Voice（语音） ───────────────────────────────────────────

    public async UniTask PlayVoiceAsync(string address, float volume = 1f) {
      var clip = await ResourceManager.Instance.LoadAsync<AudioClip>(address);
      if (clip == null) {
        return;
      }
      _voice.Stop();
      _voice.clip = clip;
      _voice.volume = volume;
      _voice.Play();
    }

    public void StopVoice() => _voice.Stop();

    // ── 音量（经 AudioMixer，dB） ───────────────────────────────

    public float GetVolume(AudioChannel channel) => PlayerPrefs.GetFloat(PrefsKey(channel), 1f);

    public void SetVolume(AudioChannel channel, float value01) {
      value01 = Mathf.Clamp01(value01);
      PlayerPrefs.SetFloat(PrefsKey(channel), value01);
      if (_mixer != null) {
        _mixer.SetFloat(ParamName(channel), LinearToDecibel(value01));
      }
    }

    private void ApplySavedVolumes() {
      if (_mixer == null) {
        return;
      }
      foreach (AudioChannel ch in Enum.GetValues(typeof(AudioChannel))) {
        _mixer.SetFloat(ParamName(ch), LinearToDecibel(GetVolume(ch)));
      }
    }

    private static string ParamName(AudioChannel channel) => channel + "Volume";
    private static string PrefsKey(AudioChannel channel) => "vol_" + channel;
    private static float LinearToDecibel(float v) => v <= 0.0001f ? -80f : Mathf.Log10(v) * 20f;

    // ── 淡入淡出（UniTask，非缩放时间） ────────────────────────

    private static async UniTask FadeVolume(
        AudioSource src, float to, float duration, CancellationToken ct) {
      if (src == null) {
        return;
      }
      if (duration <= 0f) {
        src.volume = to;
        return;
      }

      float from = src.volume;
      float t = 0f;
      while (t < duration) {
        ct.ThrowIfCancellationRequested();
        t += Time.unscaledDeltaTime;
        src.volume = Mathf.Lerp(from, to, t / duration);
        await UniTask.Yield(PlayerLoopTiming.Update, ct);
      }
      src.volume = to;
    }

    private async UniTaskVoid FadeOutAndStop(AudioSource src, float fade) {
      if (src == null) {
        return;
      }
      try {
        await FadeVolume(src, 0f, fade, this.GetCancellationTokenOnDestroy());
      } catch (OperationCanceledException) {
        return;
      }
      src.Stop();
    }
  }
}
