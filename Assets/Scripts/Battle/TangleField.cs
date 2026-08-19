using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 실타래 필드 상태. 십자 5칸 묶음을 여러 개 동시에 보관하며, 소멸도 묶음 단위다.
/// 보스 로직은 들어가지 않는다(생성 시점은 ClownBossMechanic이 정한다).
/// </summary>
public class TangleField : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Color tangleColor = new Color(0.85f, 0.75f, 0.95f, 0.7f);
    [SerializeField, Min(0.01f)] private float visualSize = 0.5f;

    private sealed class Tangle
    {
        public Vector2Int Center;
        public Vector2Int[] Cells;
        public GameObject Visual;
    }

    private readonly List<Tangle> tangles = new();

    public int Count => tangles.Count;

    /// 아무 실타래든 이 칸을 포함하면 true
    public bool Contains(Vector2Int cell)
    {
        foreach (Tangle tangle in tangles)
        {
            if (System.Array.IndexOf(tangle.Cells, cell) >= 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 중심 기준 십자 5칸에 실타래를 만든다.
    /// ponytail: 같은 중심의 실타래가 이미 있으면 무시 — 기획에 중복 생성 규칙이 없어 정한 값.
    /// 겹침을 허용해야 하면 이 검사만 지우면 된다.
    /// </summary>
    public bool Spawn(Vector2Int center)
    {
        foreach (Tangle tangle in tangles)
        {
            if (tangle.Center == center)
                return false;
        }

        Vector2Int[] cells = ClownWheel.CrossCells(center);
        tangles.Add(new Tangle
        {
            Center = center,
            Cells = cells,
            Visual = CreateVisual(cells)
        });
        return true;
    }

    /// <summary>
    /// 그 칸을 포함한 실타래를 전부 제거한다.
    /// ponytail: 십자끼리 칸이 겹칠 때 공유 칸이 파괴되면 겹친 실타래를 모두 없앤다.
    /// 기획에 명시가 없어 정한 규칙 — 하나만 없애려면 첫 일치에서 break를 넣는다.
    /// </summary>
    public void RemoveContaining(Vector2Int cell)
    {
        for (int i = tangles.Count - 1; i >= 0; i--)
        {
            if (System.Array.IndexOf(tangles[i].Cells, cell) < 0)
                continue;

            if (tangles[i].Visual != null)
                Destroy(tangles[i].Visual);
            tangles.RemoveAt(i);
        }
    }

    /// 전투 시작 시 초기화
    public void ClearAll()
    {
        foreach (Tangle tangle in tangles)
        {
            if (tangle.Visual != null)
                Destroy(tangle.Visual);
        }
        tangles.Clear();
    }

    private GameObject CreateVisual(Vector2Int[] cells)
    {
        if (gridManager == null)
            return null;

        Sprite sprite = CreateTangleSprite();

        GameObject root = new GameObject("Tangle");
        root.transform.SetParent(transform, false);

        foreach (Vector2Int cell in cells)
        {
            GameObject visual = new GameObject($"Cell ({cell.x}, {cell.y})");
            visual.transform.SetParent(root.transform, false);
            visual.transform.position =
                gridManager.GridToWorldPosition(cell.x, cell.y);
            visual.transform.localScale = new Vector3(visualSize, visualSize, 1f);

            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = tangleColor;
            renderer.sortingOrder = 1;
        }

        return root;
    }

    private static Sprite CreateTangleSprite()
    {
        int size = 16;
        Texture2D texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Point;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 실 격자 느낌: 4픽셀 간격 줄무늬
                bool thread = x % 4 == 0 || y % 4 == 0;
                texture.SetPixel(x, y, thread ? Color.white : new Color(1f, 1f, 1f, 0.15f));
            }
        }

        texture.Apply();
        return Sprite.Create(
            texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
