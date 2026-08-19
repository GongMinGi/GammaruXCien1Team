/// <summary>
/// 스테이지 선택 씬에서 고른 아르카나 목록을 전투 씬에 한 번 전달한다.
/// </summary>
public static class BattleLoadoutData
{
    private static ArcanaData[] selectedArcanaCards;

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
}
