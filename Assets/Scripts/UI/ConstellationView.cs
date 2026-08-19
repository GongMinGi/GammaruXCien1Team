using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 별자리 위에 놓이는 별 노드의 크기 단계.
/// </summary>
public enum StarNodeSize
{
    Small,
    Medium,
    Large
}

/// <summary>
/// 별자리 경로 위에 놓일 별 하나의 위치와 크기 단계.
/// </summary>
public struct StarPoint
{
    public Vector2 Position;
    public StarNodeSize Size;

    public StarPoint(Vector2 position, StarNodeSize size)
    {
        Position = position;
        Size = size;
    }
}

/// <summary>
/// 별 노드를 먼저 놓고 그 사이마다 짧은 선 조각을 따로 만들어 별자리를 그린다.
/// 선은 별 중심에 닿지 않고 항상 여백을 두고 끊긴다.
/// </summary>
public class ConstellationView : MonoBehaviour
{
    [Header("프리팹")]
    [SerializeField] private RectTransform glowLinePrefab;
    [SerializeField] private RectTransform starNodePrefab;

    [Header("선")]
    // 밝은 심지 두께와 색은 GlowLine 프리팹에서, 글로우 두께는 여기에서 조절한다.
    [SerializeField] private float lineThickness = 6f;
    // 별 심지 지름에 곱해서 선 끝과 별 중심 사이에 둘 여백을 정한다.
    [SerializeField] private float lineGapRate = 0.6f;
    // 여백을 빼고 남은 길이가 이보다 짧으면 선을 그리지 않는다.
    [SerializeField] private float minimumSegmentLength = 10f;

    [Header("별 심지 지름")]
    [SerializeField] private float smallStarSize = 14f;
    [SerializeField] private float mediumStarSize = 22f;
    [SerializeField] private float largeStarSize = 34f;

    [Header("연출")]
    [SerializeField] private float starPopDuration = 0.18f;
    [SerializeField] private float segmentDrawDuration = 0.25f;
    [SerializeField] private bool useTwinkle = true;
    [SerializeField] private float twinkleDuration = 1.6f;
    [SerializeField] private float twinkleMinimumAlpha = 0.55f;

    private RectTransform lineLayer;
    private RectTransform starLayer;

    /// <summary>
    /// 선과 별을 나눠 담을 하위 레이어를 만든다.
    /// 선 레이어를 먼저 만들므로 별이 항상 선 위에 그려진다.
    /// </summary>
    private void Awake()
    {
        lineLayer = CreateLayer("Lines");
        starLayer = CreateLayer("Stars");
    }

    /// <summary>
    /// 별자리 한 구간을 즉시 표시한다.
    /// </summary>
    public void ShowLink(StarPoint[] starPoints)
    {
        for (int i = 0; i < starPoints.Length - 1; i++)
        {
            CreateSegment(starPoints[i], starPoints[i + 1]);
        }

        for (int i = 0; i < starPoints.Length; i++)
        {
            RectTransform star = CreateStar(starPoints[i]);
            StartTwinkle(star.GetComponent<CanvasGroup>());
        }
    }

    /// <summary>
    /// 별 → 선 → 별 순서로 이어지는 연출로 별자리 한 구간을 표시한다.
    /// isClosedLoop가 참이면 마지막 별에서 첫 별로 돌아오는 선을 하나 더 그린다.
    /// 별을 만들 때마다 그 위치를 starCreatedHandler로 알려 화면이 따라오게 한다.
    /// </summary>
    public IEnumerator PlayLinkRoutine(
        StarPoint[] starPoints,
        bool isClosedLoop,
        UnityAction<Vector2> starCreatedHandler)
    {
        for (int i = 0; i < starPoints.Length; i++)
        {
            if (i > 0)
            {
                yield return DrawSegment(starPoints[i - 1], starPoints[i]);
            }

            RectTransform star = CreateStar(starPoints[i]);

            starCreatedHandler(starPoints[i].Position);
            yield return PlayStarPop(star).WaitForCompletion();
        }

        if (isClosedLoop)
        {
            yield return DrawSegment(starPoints[starPoints.Length - 1], starPoints[0]);
        }
    }

    /// <summary>
    /// 생성된 별과 선을 모두 지운다.
    /// </summary>
    public void Clear()
    {
        StopAllCoroutines();
        DestroyChildren(lineLayer);
        DestroyChildren(starLayer);
    }

    /// <summary>
    /// 두 별 사이에 선 하나를 만들어 다 그려질 때까지 기다린다.
    /// 여백을 빼고 남는 길이가 없으면 선을 그리지 않고 넘어간다.
    /// </summary>
    private IEnumerator DrawSegment(StarPoint from, StarPoint to)
    {
        RectTransform segment = CreateSegment(from, to);

        if (segment != null)
        {
            yield return PlaySegmentDraw(segment).WaitForCompletion();
        }
    }

    /// <summary>
    /// 별을 작게 시작해 제 크기로 부풀리며 나타낸다.
    /// </summary>
    private Tween PlayStarPop(RectTransform star)
    {
        CanvasGroup starGroup = star.GetComponent<CanvasGroup>();
        Vector3 fullScale = star.localScale;
        Sequence popSequence = DOTween.Sequence();

        star.localScale = fullScale * 0.2f;
        starGroup.alpha = 0f;
        popSequence.Append(star.DOScale(fullScale, starPopDuration).SetEase(Ease.OutBack));
        popSequence.Join(starGroup.DOFade(1f, starPopDuration));
        popSequence.OnComplete(delegate
        {
            StartTwinkle(starGroup);
        });
        return popSequence;
    }

    /// <summary>
    /// 선을 길이 0에서 제 길이까지 늘리며 함께 서서히 밝힌다.
    /// </summary>
    private Tween PlaySegmentDraw(RectTransform segment)
    {
        CanvasGroup segmentGroup = segment.GetComponent<CanvasGroup>();
        Vector2 fullSize = segment.sizeDelta;
        Sequence drawSequence = DOTween.Sequence();

        segment.sizeDelta = new Vector2(0f, fullSize.y);
        segmentGroup.alpha = 0f;
        drawSequence.Append(
            segment.DOSizeDelta(fullSize, segmentDrawDuration).SetEase(Ease.OutSine));
        drawSequence.Join(segmentGroup.DOFade(1f, segmentDrawDuration * 0.6f));
        return drawSequence;
    }

    /// <summary>
    /// 별마다 조금씩 다른 주기로 밝기가 오르내리게 한다.
    /// </summary>
    private void StartTwinkle(CanvasGroup starGroup)
    {
        if (!useTwinkle)
        {
            return;
        }

        starGroup.DOFade(twinkleMinimumAlpha, twinkleDuration * Random.Range(0.7f, 1.3f))
            .SetDelay(Random.Range(0f, twinkleDuration))
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    /// <summary>
    /// 두 별 사이를 잇는 선 조각 하나를 만든다.
    /// 양 끝에서 별 크기만큼 여백을 두므로 선이 별 중심을 지나가지 않는다.
    /// </summary>
    private RectTransform CreateSegment(StarPoint from, StarPoint to)
    {
        Vector2 direction = to.Position - from.Position;
        float distance = direction.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            return null;
        }

        float startGap = GetStarSize(from.Size) * lineGapRate;
        float endGap = GetStarSize(to.Size) * lineGapRate;
        float length = distance - startGap - endGap;

        if (length < minimumSegmentLength)
        {
            return null;
        }

        RectTransform segment = Instantiate(glowLinePrefab, lineLayer, false);
        Vector2 unitDirection = direction / distance;

        segment.anchoredPosition = from.Position + unitDirection * startGap;
        segment.sizeDelta = new Vector2(length, lineThickness);
        segment.localEulerAngles = new Vector3(
            0f,
            0f,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        return segment;
    }

    /// <summary>
    /// 지정한 지점에 크기 단계에 맞는 별 노드를 만든다.
    /// </summary>
    private RectTransform CreateStar(StarPoint starPoint)
    {
        RectTransform star = Instantiate(starNodePrefab, starLayer, false);
        float scale = GetStarSize(starPoint.Size) / starNodePrefab.sizeDelta.x;

        star.anchoredPosition = starPoint.Position;
        star.localScale = new Vector3(scale, scale, 1f);
        star.localEulerAngles = new Vector3(0f, 0f, GetStarAngle(starPoint.Position));
        return star;
    }

    /// <summary>
    /// 크기 단계에 해당하는 별 심지의 지름을 반환한다.
    /// </summary>
    private float GetStarSize(StarNodeSize size)
    {
        if (size == StarNodeSize.Large)
        {
            return largeStarSize;
        }

        if (size == StarNodeSize.Medium)
        {
            return mediumStarSize;
        }

        return smallStarSize;
    }

    /// <summary>
    /// 별이 모두 같은 방향으로 놓이지 않도록 위치에서 회전 각도를 만든다.
    /// </summary>
    private float GetStarAngle(Vector2 position)
    {
        return Mathf.Repeat(position.x * 3.13f + position.y * 7.77f, 90f);
    }

    /// <summary>
    /// 부모 영역을 그대로 채우는 빈 하위 레이어를 만든다.
    /// </summary>
    private RectTransform CreateLayer(string layerName)
    {
        GameObject layerObject = new GameObject(layerName, typeof(RectTransform));
        RectTransform layer = layerObject.GetComponent<RectTransform>();

        layer.SetParent(transform, false);
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = Vector2.zero;
        layer.offsetMax = Vector2.zero;
        return layer;
    }

    /// <summary>
    /// 레이어에 생성된 오브젝트를 모두 제거한다.
    /// 반짝임처럼 끝나지 않는 트윈이 남지 않도록 먼저 정리한다.
    /// </summary>
    private void DestroyChildren(RectTransform layer)
    {
        for (int i = layer.childCount - 1; i >= 0; i--)
        {
            Transform child = layer.GetChild(i);
            CanvasGroup childGroup = child.GetComponent<CanvasGroup>();

            child.DOKill();

            if (childGroup != null)
            {
                childGroup.DOKill();
            }

            Destroy(child.gameObject);
        }
    }
}
