using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 타이틀 화면 게임 시작 버튼의 효과음을 재생하고 씬 전환을 시작한다.
/// </summary>
public class GameStartButtonView : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private SceneTransitionController sceneTransitionController;

    /// <summary>
    /// 버튼에 포인터가 올라오면 효과음을 재생한다.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        SoundManager.Instance.PlaySoundEffect("ButtonHover");
    }

    /// <summary>
    /// 버튼 클릭에 연결한다. 효과음을 재생하고 씬 전환을 시작한다.
    /// </summary>
    public void StartGame()
    {
        SoundManager.Instance.PlaySoundEffect("ButtonClick");
        SoundManager.Instance.PlaySoundEffect("GameStart");
        sceneTransitionController.StartSceneTransition();
    }
}
