using UnityEngine;

/// <summary>
/// 씬 이름과 그 씬에서 재생할 배경음악을 묶는다.
/// </summary>
[System.Serializable]
public class SceneBackgroundMusic
{
    public string sceneName;
    public AudioClip clip;
}

/// <summary>
/// 효과음 이름과 클립을 묶는다.
/// </summary>
[System.Serializable]
public class SoundEffectEntry
{
    public string soundEffectName;
    public AudioClip clip;
}

/// <summary>
/// 게임 전체에서 사용하는 사운드 클립을 보관한다.
/// </summary>
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Sound/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [SerializeField] private SceneBackgroundMusic[] sceneBackgroundMusics;
    [SerializeField] private SoundEffectEntry[] soundEffects;

    /// <summary>
    /// 해당 씬의 배경음악을 반환한다. 등록되지 않았다면 null을 반환한다.
    /// </summary>
    public AudioClip FindBackgroundMusic(string sceneName)
    {
        for (int index = 0; index < sceneBackgroundMusics.Length; index++)
        {
            if (sceneBackgroundMusics[index].sceneName == sceneName)
            {
                return sceneBackgroundMusics[index].clip;
            }
        }

        return null;
    }

    /// <summary>
    /// 해당 이름의 효과음을 반환한다. 등록되지 않았다면 null을 반환한다.
    /// </summary>
    public AudioClip FindSoundEffect(string soundEffectName)
    {
        for (int index = 0; index < soundEffects.Length; index++)
        {
            if (soundEffects[index].soundEffectName == soundEffectName)
            {
                return soundEffects[index].clip;
            }
        }

        return null;
    }
}
