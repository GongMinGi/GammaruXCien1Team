using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BuildBossCardBoard
{
    private const string SpriteSheetPath = "Assets/Arts/Battle/ingame_monstercard (1).png";
    private const string FontPath = "Assets/Fonts/PF스타더스트 3.0 ExtraBold.ttf";

    private const int SlabCount = 4;
    private const float SlabStepX = 0.62f;
    private const float SunY = -0.45f;

    [MenuItem("Tools/Build Boss Card Board")]
    public static void Execute()
    {
        BossCardDisplay display = Object.FindFirstObjectByType<BossCardDisplay>();
        Object[] sheet = AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath);
        Sprite boardSprite = FindSprite(sheet, "ingame_monstercard (1)_1");
        Sprite sunSprite = FindSprite(sheet, "ingame_monstercard (1)_2");
        Sprite slabSprite = FindSprite(sheet, "ingame_monstercard (1)_3");
        Font turnTextFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);

        Transform root = display.transform;
        for (int i = root.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(root.GetChild(i).gameObject);

        GameObject boardObject = new GameObject("Board", typeof(SpriteRenderer));
        boardObject.transform.SetParent(root, false);

        SpriteRenderer boardRenderer = boardObject.GetComponent<SpriteRenderer>();
        boardRenderer.sprite = boardSprite;
        boardRenderer.sortingOrder = 0;

        SpriteRenderer[] slabRenderers = new SpriteRenderer[SlabCount];
        SpriteRenderer[] slabCardRenderers = new SpriteRenderer[SlabCount];
        SpriteRenderer[] sunRenderers = new SpriteRenderer[SlabCount];
        Text[] sunTurnTexts = new Text[SlabCount];

        float startX = -(SlabCount - 1) * SlabStepX * 0.5f;

        for (int i = 0; i < SlabCount; i++)
        {
            GameObject slabObject = new GameObject("Slab (" + (i + 1) + ")",
                typeof(SpriteRenderer), typeof(BoxCollider2D));
            slabObject.transform.SetParent(root, false);
            slabObject.transform.localPosition = new Vector3(startX + i * SlabStepX, 0f, 0f);

            SpriteRenderer slabRenderer = slabObject.GetComponent<SpriteRenderer>();
            slabRenderer.sprite = slabSprite;
            slabRenderer.sortingOrder = 1;

            slabObject.GetComponent<BoxCollider2D>().size = slabSprite.bounds.size;

            GameObject cardObject = new GameObject("CardImage", typeof(SpriteRenderer));
            cardObject.transform.SetParent(slabObject.transform, false);
            cardObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);

            SpriteRenderer cardRenderer = cardObject.GetComponent<SpriteRenderer>();
            cardRenderer.sortingOrder = 2;
            cardRenderer.enabled = false;

            slabRenderers[i] = slabRenderer;
            slabCardRenderers[i] = cardRenderer;
        }

        for (int i = 0; i < SlabCount; i++)
        {
            GameObject sunObject = new GameObject("Sun (" + (i + 1) + ")", typeof(SpriteRenderer));
            sunObject.transform.SetParent(root, false);
            sunObject.transform.localPosition = new Vector3(startX + i * SlabStepX, SunY, 0f);

            SpriteRenderer sunRenderer = sunObject.GetComponent<SpriteRenderer>();
            sunRenderer.sprite = sunSprite;
            sunRenderer.sortingOrder = 3;

            GameObject canvasObject = new GameObject("TurnTextCanvas",
                typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(sunObject.transform, false);

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(100f, 100f);
            canvasRect.localScale = new Vector3(0.004f, 0.004f, 0.004f);
            canvasRect.localPosition = new Vector3(0f, 0f, -0.01f);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.sortingOrder = 4;

            GameObject textObject = new GameObject("TurnText",
                typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(canvasObject.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text turnText = textObject.GetComponent<Text>();
            turnText.font = turnTextFont;
            turnText.fontSize = 60;
            turnText.alignment = TextAnchor.MiddleCenter;
            turnText.horizontalOverflow = HorizontalWrapMode.Overflow;
            turnText.verticalOverflow = VerticalWrapMode.Overflow;
            turnText.color = new Color(1f, 0.99f, 0f, 1f);
            turnText.raycastTarget = false;

            sunRenderers[i] = sunRenderer;
            sunTurnTexts[i] = turnText;
        }

        SerializedObject serializedDisplay = new SerializedObject(display);
        AssignArray(serializedDisplay.FindProperty("slabRenderers"), slabRenderers);
        AssignArray(serializedDisplay.FindProperty("slabCardRenderers"), slabCardRenderers);
        AssignArray(serializedDisplay.FindProperty("sunRenderers"), sunRenderers);
        AssignArray(serializedDisplay.FindProperty("sunTurnTexts"), sunTurnTexts);
        serializedDisplay.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(display.gameObject.scene);
        Debug.Log("Boss card board built under " + display.name + ".", display);
    }

    private static Sprite FindSprite(Object[] assets, string spriteName)
    {
        for (int i = 0; i < assets.Length; i++)
        {
            Sprite sprite = assets[i] as Sprite;
            if (sprite != null && sprite.name == spriteName)
                return sprite;
        }

        return null;
    }

    private static void AssignArray(SerializedProperty arrayProperty, Object[] values)
    {
        arrayProperty.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            arrayProperty.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
