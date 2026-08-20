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

    private float loopStart;
    private float loopEnd;

    /// <summary>
    /// 배경음악을 반복 재생한다. 같은 곡이 이미 재생 중이면 끊지 않고 이어간다.
    /// loopEnd가 loopStart보다 크면 그 구간만 반복하고, 아니면 곡 전체를 반복한다.
    /// </summary>
    public void PlayBackgroundMusic(AudioClip clip, float sectionLoopStart, float sectionLoopEnd)
    {
        if (backgroundMusicSource.clip == clip && backgroundMusicSource.isPlaying)
        {
            return;
        }

        loopStart = Mathf.Max(0f, sectionLoopStart);
        loopEnd = sectionLoopEnd > loopStart ? sectionLoopEnd : 0f;

        backgroundMusicSource.clip = clip;
        backgroundMusicSource.volume = backgroundMusicVolume;
        // 구간 반복 중에도 loop를 켜 둔다. 프레임이 크게 밀려 끝 지점을 놓쳐도
        // 무음으로 죽지 않고 곡 처음으로 돌아간다.
        backgroundMusicSource.loop = true;
        backgroundMusicSource.Play();
    }

    /// <summary>
    /// 구간 반복이 설정된 배경음악을 끝 지점에서 시작 지점으로 되감는다.
    /// </summary>
    private void Update()
    {
        if (loopEnd <= 0f || !backgroundMusicSource.isPlaying)
        {
            return;
        }

        // ponytail: 프레임 단위 폴링이라 끝 지점을 최대 한 프레임(~16ms) 넘긴 뒤 되감긴다.
        // 이음매가 들리면 인트로/루프 클립 2개 + PlayScheduled 방식으로 올린다.
        if (backgroundMusicSource.time >= loopEnd)
        {
            backgroundMusicSource.time = loopStart;
        }
    }

    /// <summary>
    /// 재생 중인 배경음악을 멈춘다.
    /// </summary>
    public void StopBackgroundMusic()
    {
        loopEnd = 0f;
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
