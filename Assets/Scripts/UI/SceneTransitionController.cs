using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// DOTween 페이드로 타이틀 씬에서 스테이지 선택 씬으로 전환한다.
/// </summary>
public class SceneTransitionController : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private string stageSceneName = "StageSelectScene";
    [SerializeField] private float fadeDuration = 0.6f;

    /// <summary>
    /// 화면을 가린 뒤 스테이지 선택 씬을 불러오는 전환을 시작한다.
    /// </summary>
    public void LoadStageScene()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += FadeIn;
        fadeCanvasGroup.blocksRaycasts = true;
        fadeCanvasGroup.DOFade(1f, fadeDuration).OnComplete(LoadScene);
    }

    /// <summary>
    /// 설정된 스테이지 선택 씬을 불러온다.
    /// </summary>
    private void LoadScene()
    {
        SceneManager.LoadScene(stageSceneName);
    }

    /// <summary>
    /// 새 씬이 로드되면 가려진 화면을 다시 보여준다.
    /// </summary>
    private void FadeIn(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= FadeIn;
        fadeCanvasGroup.DOFade(0f, fadeDuration).OnComplete(FinishTransition);
    }

    /// <summary>
    /// 페이드가 끝난 전환 오브젝트를 제거한다.
    /// </summary>
    private void FinishTransition()
    {
        Destroy(gameObject);
    }
}
