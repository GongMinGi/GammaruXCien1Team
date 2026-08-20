using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 튜토리얼 단계를 순서대로 진행한다.
/// 플레이어가 기다리던 동작을 마쳤다는 알림을 받으면 다음 단계로 넘어간다.
/// </summary>
public class TutorialController : MonoBehaviour
{
    private static TutorialController runningTutorialController;

    [SerializeField] private TutorialView tutorialView;
    [SerializeField] private TutorialStepData[] tutorialSteps;

    private int currentStepIndex;
    private bool isTutorialRunning;
    private int stepShownFrameCount;

    private void Awake()
    {
        tutorialView.Close();
    }

    /// <summary>
    /// 튜토리얼을 첫 단계부터 시작한다.
    /// </summary>
    public void StartTutorial()
    {
        runningTutorialController = this;
        isTutorialRunning = true;
        currentStepIndex = 0;
        tutorialView.Open();
        ShowCurrentStep();
    }

    /// <summary>
    /// 실행 중에 만들어지는 UI를 지정한 단계의 강조 대상으로 정한다.
    /// </summary>
    public void SetStepHighlightTarget(int stepIndex, RectTransform highlightTarget)
    {
        tutorialSteps[stepIndex].highlightTarget = highlightTarget;
    }

    /// <summary>
    /// 클릭으로 넘기는 단계에서 마우스를 누르면 다음 단계로 넘어간다.
    /// 단계가 나타난 프레임의 클릭은 그 단계를 띄운 클릭이므로 넘긴다.
    /// </summary>
    private void Update()
    {
        if (!isTutorialRunning
            || !tutorialSteps[currentStepIndex].completesOnClick
            || Time.frameCount == stepShownFrameCount
            || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        GoToNextStep();
    }

    /// <summary>
    /// 플레이어가 어떤 동작을 마쳤음을 알린다.
    /// 튜토리얼이 진행 중이 아니면 아무 일도 일어나지 않는다.
    /// </summary>
    public static void NotifyActionCompleted(string actionName)
    {
        if (runningTutorialController == null)
        {
            return;
        }

        runningTutorialController.ProceedIfWaitingFor(actionName);
    }

    /// <summary>
    /// 알림이 지금 기다리던 동작과 같으면 다음 단계로 넘어간다.
    /// </summary>
    private void ProceedIfWaitingFor(string actionName)
    {
        if (tutorialSteps[currentStepIndex].waitingActionName != actionName)
        {
            return;
        }

        GoToNextStep();
    }

    /// <summary>
    /// 다음 단계를 표시하고, 마지막 단계였다면 튜토리얼을 끝낸다.
    /// </summary>
    private void GoToNextStep()
    {
        currentStepIndex++;

        if (currentStepIndex < tutorialSteps.Length)
        {
            ShowCurrentStep();
            return;
        }

        FinishTutorial();
    }

    private void ShowCurrentStep()
    {
        TutorialStepData step = tutorialSteps[currentStepIndex];

        stepShownFrameCount = Time.frameCount;
        tutorialView.ShowStep(
            step.highlightTarget,
            step.descriptionText,
            step.completesOnClick);
    }

    private void FinishTutorial()
    {
        runningTutorialController = null;
        isTutorialRunning = false;
        tutorialView.Close();
    }
}
