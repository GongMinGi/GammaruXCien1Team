using UnityEngine;

/// <summary>
/// AudioSource를 직접 다뤄 사운드를 소리로 출력한다.
/// </summary>
public class SoundPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource backgroundMusicSource;
    [SerializeField] private AudioSource soundEffectSource;
    [SerializeField, Range(0f, 1f)] private float backgroundMusicVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float soundEffectVolume = 1f;

    /// <summary>
    /// 배경음악을 반복 재생한다. 같은 곡이 이미 재생 중이면 끊지 않고 이어간다.
    /// </summary>
    public void PlayBackgroundMusic(AudioClip clip)
    {
        if (backgroundMusicSource.clip == clip && backgroundMusicSource.isPlaying)
        {
            return;
        }

        backgroundMusicSource.clip = clip;
        backgroundMusicSource.volume = backgroundMusicVolume;
        backgroundMusicSource.loop = true;
        backgroundMusicSource.Play();
    }

    /// <summary>
    /// 재생 중인 배경음악을 멈춘다.
    /// </summary>
    public void StopBackgroundMusic()
    {
        backgroundMusicSource.Stop();
    }

    /// <summary>
    /// 효과음을 한 번 재생한다. 배경음악이나 다른 효과음을 끊지 않는다.
    /// </summary>
    public void PlaySoundEffect(AudioClip clip)
    {
        soundEffectSource.PlayOneShot(clip, soundEffectVolume);
    }
}
