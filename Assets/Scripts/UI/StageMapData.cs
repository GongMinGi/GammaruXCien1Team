using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지 카드 순서, 세로 배치 비율, X 간격, 별 노드 수를 보관한다.
/// </summary>
[CreateAssetMenu(fileName = "StageMapData", menuName = "Stage/Stage Map Data")]
public class StageMapData : ScriptableObject
{
    [SerializeField] private List<float> verticalRates = new List<float>();
    [SerializeField] private float horizontalSpacing = 320f;
    [SerializeField] private int starNodeCountPerLink = 4;

    /// <summary>
    /// 현재 저장된 스테이지 수를 반환한다.
    /// </summary>
    public int StageCount
    {
        get { return verticalRates.Count; }
    }

    /// <summary>
    /// 스테이지 카드 사이의 X 간격을 반환한다.
    /// </summary>
    public float HorizontalSpacing
    {
        get { return horizontalSpacing; }
    }

    /// <summary>
    /// 스테이지 사이를 잇는 별자리에 놓일 별 노드 수를 반환한다.
    /// 카드 옆에 붙는 양 끝 별을 포함하므로 최소 2개다.
    /// </summary>
    public int StarNodeCountPerLink
    {
        get { return starNodeCountPerLink; }
    }

    /// <summary>
    /// 지정한 스테이지의 세로 배치 비율을 반환한다.
    /// </summary>
    public float GetVerticalRate(int index)
    {
        return verticalRates[index];
    }
}
