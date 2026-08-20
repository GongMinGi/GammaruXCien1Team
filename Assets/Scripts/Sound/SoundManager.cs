using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 모든 씬에서 공용으로 사용하는 사운드 재생 창구다.
/// 씬이 바뀌면 그 씬의 배경음악으로 자동 교체한다.
/// </summary>
public class SoundManager : MonoBehaviour
{
    private const string PrefabResourcePath = "SoundManager";

    public static SoundManager Instance;

    [SerializeField] private SoundLibrary soundLibrary;
    [SerializeField] private SoundPlayer soundPlayer;

    /// <summary>
    /// 어떤 씬에서 게임을 시작하든 매니저가 존재하도록 자동으로 생성한다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInstance()
    {
        GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
        Instantiate(prefab);
    }

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        PlayBackgroundMusicOfScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        PlayBackgroundMusicOfScene(scene.name);
    }

    private void PlayBackgroundMusicOfScene(string sceneName)
    {
        SceneBackgroundMusic backgroundMusic = soundLibrary.FindBackgroundMusic(sceneName);
        if (backgroundMusic == null || backgroundMusic.clip == null)
        {
            soundPlayer.StopBackgroundMusic();
            return;
        }

        soundPlayer.PlayBackgroundMusic(
            backgroundMusic.clip,
            backgroundMusic.loopStart,
            backgroundMusic.loopEnd);
    }

    /// <summary>
    /// 라이브러리에 등록된 이름으로 효과음을 재생한다.
    /// </summary>
    public void PlaySoundEffect(string soundEffectName)
    {
        AudioClip clip = soundLibrary.FindSoundEffect(soundEffectName);
        soundPlayer.PlaySoundEffect(clip);
    }

    /// <summary>
    /// 재생 중인 배경음악을 멈춘다.
    /// </summary>
    public void StopBackgroundMusic()
    {
        soundPlayer.StopBackgroundMusic();
    }
}
