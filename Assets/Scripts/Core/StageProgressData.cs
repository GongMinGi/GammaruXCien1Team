/// <summary>
/// 현재 실행에서 선택한 스테이지와 잠금 해제된 스테이지 수를 보관한다.
/// </summary>
public static class StageProgressData
{
    private static int selectedStageNumber;
    private static int maximumStageCount;
    private static int unlockedStageCount = 1;

    /// <summary>
    /// 현재 잠금 해제된 스테이지 수를 반환한다.
    /// </summary>
    public static int UnlockedStageCount
    {
        get { return unlockedStageCount; }
    }

    /// <summary>
    /// 플레이할 스테이지 번호와 전체 스테이지 수를 저장한다.
    /// </summary>
    public static void SelectStage(int stageNumber, int stageCount)
    {
        selectedStageNumber = stageNumber;
        maximumStageCount = stageCount;
    }

    /// <summary>
    /// 현재 마지막 스테이지를 클리어했다면 다음 스테이지를 잠금 해제한다.
    /// </summary>
    public static bool CompleteSelectedStage()
    {
        if (selectedStageNumber != unlockedStageCount
            || unlockedStageCount >= maximumStageCount)
        {
            return false;
        }

        unlockedStageCount++;
        return true;
    }
}
