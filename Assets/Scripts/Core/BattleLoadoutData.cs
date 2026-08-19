/// <summary>
/// 스테이지 선택 씬에서 정해진 아르카나 목록과 보스를 전투 씬에 한 번 전달한다.
/// </summary>
public static class BattleLoadoutData
{
    private static ArcanaData[] selectedArcanaCards;
    private static BossData selectedBossData;

    /// <summary>
    /// 전투 씬에 전달할 아르카나 목록을 저장한다.
    /// </summary>
    public static void SaveSelectedArcanaCards(ArcanaData[] arcanaCards)
    {
        selectedArcanaCards = arcanaCards;
    }

    /// <summary>
    /// 저장된 아르카나 목록을 반환하고 전달 데이터를 비운다.
    /// </summary>
    public static ArcanaData[] TakeSelectedArcanaCards()
    {
        ArcanaData[] arcanaCards = selectedArcanaCards;
        selectedArcanaCards = null;
        return arcanaCards;
    }

    /// <summary>
    /// 전투 씬에 전달할 보스를 저장한다.
    /// </summary>
    public static void SaveSelectedBossData(BossData bossData)
    {
        selectedBossData = bossData;
    }

    /// <summary>
    /// 저장된 보스를 반환하고 전달 데이터를 비운다.
    /// </summary>
    public static BossData TakeSelectedBossData()
    {
        BossData bossData = selectedBossData;
        selectedBossData = null;
        return bossData;
    }
}
