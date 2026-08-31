using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    public static AudioManager Instance => instance;

    [Header("BGM")]
    public AudioClip homeBgm;   // 家园 BGM（温馨氛围）
    public AudioClip gameBgm;   // 游戏大世界 BGM

    [Header("战斗音效")]
    public AudioClip attackClip;   // 挥刀/攻击
    public AudioClip hitClip;      // 命中
    public AudioClip hurtClip;     // 受击
    public AudioClip deathClip;    // 死亡

    [Header("UI / 交互")]
    public AudioClip uiClickClip;     // 按钮点击
    public AudioClip possessInClip;   // 附身进入
    public AudioClip possessOutClip;  // 附身弹回

    [Header("音量")]
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private AudioSource bgmSource;   // 背景乐（2D 循环，跨场景保留）
    private AudioSource uiSource;    // UI 音效（2D 一次性）

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f;

        uiSource = gameObject.AddComponent<AudioSource>();
        uiSource.loop = false;
        uiSource.playOnAwake = false;
        uiSource.spatialBlend = 0f;
    }

    // ===== BGM =====
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource.clip == clip) return;
        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    // ===== 3D 位置音效（一次性：攻击/受击/死亡）=====
    // 用 PlayClipAtPoint：内部建临时对象自动销毁，角色不用挂 AudioSource
    public void PlaySFX(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, sfxVolume);
    }

    // ===== 2D UI 音效 =====
    public void PlaySFX2D(AudioClip clip)
    {
        if (clip == null) return;
        uiSource.PlayOneShot(clip, sfxVolume);
    }

    // ===== 语义化便捷接口（hook 点一行调用）=====
    public void PlayAttack(Vector3 pos) => PlaySFX(attackClip, pos);
    public void PlayHit(Vector3 pos)    => PlaySFX(hitClip, pos);
    public void PlayHurt(Vector3 pos)   => PlaySFX(hurtClip, pos);
    public void PlayDeath(Vector3 pos)  => PlaySFX(deathClip, pos);
    public void PlayUIClick()           => PlaySFX2D(uiClickClip);
    public void PlayPossessIn()         => PlaySFX2D(possessInClip);
    public void PlayPossessOut()        => PlaySFX2D(possessOutClip);

    // ===== 音量 =====
    public void SetBGMVolume(float v)
    {
        bgmVolume = Mathf.Clamp01(v);
        if (bgmSource != null) bgmSource.volume = bgmVolume;
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
    }
}
