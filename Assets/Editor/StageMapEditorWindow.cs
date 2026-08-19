using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 탭에서는 카드 배치를, StarLine 탭에서는 별자리 선과 별의 모양을 편집한다.
/// </summary>
public class StageMapEditorWindow : EditorWindow
{
    private const string DataPath = "Assets/Data/StageMapData.asset";
    private const string VerticalRatesProperty = "verticalRates";
    private const string HorizontalSpacingProperty = "horizontalSpacing";
    private const string StarNodeCountProperty = "starNodeCountPerLink";
    private const string GlowLinePrefabProperty = "glowLinePrefab";
    private const string StarNodePrefabProperty = "starNodePrefab";
    private const string ImageColorProperty = "m_Color";
    private const string LineCoreName = "LineCore";
    private const string StarGlowName = "StarGlow";
    private const string StarCoreName = "StarCore";

    private static readonly string[] TabNames = { "스테이지", "StarLine" };

    private StageMapData stageMapData;
    private Vector2 scrollPosition;
    private Vector2 starLineScrollPosition;
    private int selectedTab;
    private int selectedIndex = -1;
    private int amount = 1;

    /// <summary>
    /// 스테이지 맵 에디터 창을 연다.
    /// </summary>
    [MenuItem("Tools/Stage Map Editor")]
    public static void Open()
    {
        GetWindow<StageMapEditorWindow>("Stage Map Editor");
    }

    /// <summary>
    /// 에디터 창이 활성화될 때 기본 스테이지 데이터를 불러온다.
    /// </summary>
    private void OnEnable()
    {
        stageMapData = AssetDatabase.LoadAssetAtPath<StageMapData>(DataPath);
    }

    /// <summary>
    /// 탭을 그리고 선택한 탭의 내용을 표시한다.
    /// </summary>
    private void OnGUI()
    {
        stageMapData = (StageMapData)EditorGUILayout.ObjectField(
            "Stage Map Data",
            stageMapData,
            typeof(StageMapData),
            false);

        if (stageMapData == null)
        {
            EditorGUILayout.HelpBox("StageMapData를 지정하세요.", MessageType.Info);
            return;
        }

        selectedTab = GUILayout.Toolbar(selectedTab, TabNames);
        EditorGUILayout.Space();

        if (selectedTab == 0)
        {
            DrawStageTab();
            return;
        }

        DrawStarLineTab();
    }

    /// <summary>
    /// X 간격과 카드 추가 및 삭제 조작부를 그린다.
    /// </summary>
    private void DrawStageTab()
    {
        SerializedObject dataObject = new SerializedObject(stageMapData);
        SerializedProperty verticalRates = dataObject.FindProperty(VerticalRatesProperty);
        SerializedProperty horizontalSpacing
            = dataObject.FindProperty(HorizontalSpacingProperty);
        dataObject.Update();
        selectedIndex = Mathf.Min(selectedIndex, verticalRates.arraySize - 1);

        horizontalSpacing.floatValue = Mathf.Max(
            1f,
            EditorGUILayout.FloatField("X 간격", horizontalSpacing.floatValue));
        DrawStageButtons(verticalRates);

        amount = Mathf.Max(1, EditorGUILayout.IntField("수량", amount));
        DrawEditButtons(dataObject, verticalRates);
        dataObject.ApplyModifiedProperties();
        DrawStageClearTestButton();
    }

    /// <summary>
    /// 별자리 선과 별의 두께, 크기, 색, 배치, 연출을 한곳에서 편집한다.
    /// </summary>
    private void DrawStarLineTab()
    {
        ConstellationView constellationView
            = Object.FindFirstObjectByType<ConstellationView>();

        if (constellationView == null)
        {
            EditorGUILayout.HelpBox(
                "ConstellationView가 있는 씬을 열어야 편집할 수 있습니다.",
                MessageType.Info);
            return;
        }

        SerializedObject constellationObject = new SerializedObject(constellationView);
        RectTransform glowLinePrefab = FindPrefabRoot(
            constellationObject,
            GlowLinePrefabProperty);
        RectTransform starNodePrefab = FindPrefabRoot(
            constellationObject,
            StarNodePrefabProperty);

        starLineScrollPosition = EditorGUILayout.BeginScrollView(starLineScrollPosition);
        constellationObject.Update();

        EditorGUILayout.LabelField("선", EditorStyles.boldLabel);
        DrawProperty(constellationObject, "lineThickness", "선 두께");
        DrawProperty(constellationObject, "lineGapRate", "별 여백 비율");
        DrawProperty(constellationObject, "minimumSegmentLength", "최소 선 길이");
        DrawPrefabColor(glowLinePrefab, null, "선 글로우 색");
        DrawPrefabColor(glowLinePrefab, LineCoreName, "선 심지 색");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("별", EditorStyles.boldLabel);
        DrawStarNodeCount();
        DrawProperty(constellationObject, "smallStarSize", "작은 별 지름");
        DrawProperty(constellationObject, "mediumStarSize", "중간 별 지름");
        DrawProperty(constellationObject, "largeStarSize", "큰 별 지름");
        DrawPrefabColor(starNodePrefab, StarGlowName, "별 헤일로 색");
        DrawPrefabColor(starNodePrefab, StarCoreName, "별 심지 색");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("배치", EditorStyles.boldLabel);
        DrawStageMapControllerProperties();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("연출", EditorStyles.boldLabel);
        DrawProperty(constellationObject, "starPopDuration", "별 등장 시간");
        DrawProperty(constellationObject, "segmentDrawDuration", "선 등장 시간");
        DrawProperty(constellationObject, "useTwinkle", "반짝임 사용");
        DrawProperty(constellationObject, "twinkleDuration", "반짝임 주기");
        DrawProperty(constellationObject, "twinkleMinimumAlpha", "반짝임 최저 알파");
        constellationObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        DrawConstellationRebuildButton();
        EditorGUILayout.HelpBox(
            "색은 프리팹에 바로 저장됩니다. 두께와 크기, 배치, 연출 값은 씬 컴포넌트라"
                + " Play Mode에서 바꾼 값은 정지하면 사라집니다.",
            MessageType.Info);
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 한 구간에 놓일 별 노드 수를 편집한다.
    /// </summary>
    private void DrawStarNodeCount()
    {
        SerializedObject dataObject = new SerializedObject(stageMapData);
        SerializedProperty starNodeCount = dataObject.FindProperty(StarNodeCountProperty);

        dataObject.Update();
        starNodeCount.intValue = Mathf.Max(
            2,
            EditorGUILayout.IntField("별 노드 수(양 끝 포함)", starNodeCount.intValue));
        dataObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 별자리 경로를 계산하는 컨트롤러의 배치 값을 편집한다.
    /// </summary>
    private void DrawStageMapControllerProperties()
    {
        StageMapController stageMapController
            = Object.FindFirstObjectByType<StageMapController>();

        if (stageMapController == null)
        {
            EditorGUILayout.HelpBox(
                "StageMapController를 찾을 수 없습니다.",
                MessageType.Info);
            return;
        }

        SerializedObject controllerObject = new SerializedObject(stageMapController);

        controllerObject.Update();
        DrawProperty(controllerObject, "cardEdgePadding", "카드 여백");
        DrawProperty(controllerObject, "constellationArcRate", "호 비율");
        DrawProperty(controllerObject, "starPositionJitter", "간격 흔들기");
        DrawProperty(controllerObject, "starSideJitter", "좌우 흩뿌리기");
        controllerObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Play Mode에서 바꾼 값으로 별자리를 다시 그리는 버튼을 그린다.
    /// </summary>
    private void DrawConstellationRebuildButton()
    {
        EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);

        if (GUILayout.Button("별자리 다시 그리기"))
        {
            StageMapController stageMapController
                = Object.FindFirstObjectByType<StageMapController>();

            if (stageMapController != null)
            {
                stageMapController.RebuildConstellationForTest();
            }
        }

        EditorGUI.EndDisabledGroup();
    }

    /// <summary>
    /// 지정한 이름의 직렬화 값을 한 줄로 그린다.
    /// </summary>
    private void DrawProperty(
        SerializedObject targetObject,
        string propertyName,
        string label)
    {
        SerializedProperty property = targetObject.FindProperty(propertyName);

        if (property == null)
        {
            EditorGUILayout.LabelField(label, "찾을 수 없음");
            return;
        }

        EditorGUILayout.PropertyField(property, new GUIContent(label));
    }

    /// <summary>
    /// 프리팹 안 Image의 색을 편집하고 바뀌면 프리팹에 바로 저장한다.
    /// </summary>
    private void DrawPrefabColor(
        RectTransform prefabRoot,
        string childName,
        string label)
    {
        Image image = FindPrefabImage(prefabRoot, childName);

        if (image == null)
        {
            EditorGUILayout.LabelField(label, "찾을 수 없음");
            return;
        }

        SerializedObject imageObject = new SerializedObject(image);
        SerializedProperty color = imageObject.FindProperty(ImageColorProperty);

        imageObject.Update();
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(color, new GUIContent(label));

        if (EditorGUI.EndChangeCheck())
        {
            imageObject.ApplyModifiedProperties();
            PrefabUtility.SavePrefabAsset(prefabRoot.gameObject);
        }
    }

    /// <summary>
    /// ConstellationView가 참조하는 프리팹의 최상위 RectTransform을 반환한다.
    /// </summary>
    private RectTransform FindPrefabRoot(
        SerializedObject constellationObject,
        string propertyName)
    {
        SerializedProperty property = constellationObject.FindProperty(propertyName);

        if (property == null)
        {
            return null;
        }

        return property.objectReferenceValue as RectTransform;
    }

    /// <summary>
    /// 프리팹 최상위나 지정한 자식에서 Image를 찾는다.
    /// </summary>
    private Image FindPrefabImage(RectTransform prefabRoot, string childName)
    {
        if (prefabRoot == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(childName))
        {
            return prefabRoot.GetComponent<Image>();
        }

        Transform child = prefabRoot.Find(childName);
        return child == null ? null : child.GetComponent<Image>();
    }

    /// <summary>
    /// 현재 스테이지를 선택할 수 있는 번호 카드 목록을 그린다.
    /// </summary>
    private void DrawStageButtons(SerializedProperty verticalRates)
    {
        scrollPosition = EditorGUILayout.BeginScrollView(
            scrollPosition,
            true,
            false,
            GUILayout.Height(72f));
        EditorGUILayout.BeginHorizontal();

        for (int i = 0; i < verticalRates.arraySize; i++)
        {
            Color previousColor = GUI.backgroundColor;

            if (i == selectedIndex)
            {
                GUI.backgroundColor = new Color(0.45f, 0.75f, 1f);
            }

            if (GUILayout.Button((i + 1).ToString(), GUILayout.Width(52f), GUILayout.Height(52f)))
            {
                selectedIndex = i;
            }

            GUI.backgroundColor = previousColor;
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();

        string selection = selectedIndex < 0 ? "없음" : (selectedIndex + 1).ToString();
        EditorGUILayout.LabelField("선택 스테이지", selection);
    }

    /// <summary>
    /// 선택한 위치를 기준으로 스테이지를 추가하거나 삭제하는 버튼을 그린다.
    /// </summary>
    private void DrawEditButtons(
        SerializedObject dataObject,
        SerializedProperty verticalRates)
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("추가"))
        {
            AddStages(dataObject, verticalRates);
        }

        if (GUILayout.Button("삭제"))
        {
            RemoveStages(dataObject, verticalRates);
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 선택한 카드 뒤에 입력한 수량만큼 새 스테이지를 추가한다.
    /// </summary>
    private void AddStages(
        SerializedObject dataObject,
        SerializedProperty verticalRates)
    {
        int insertIndex = selectedIndex < 0
            ? verticalRates.arraySize
            : selectedIndex + 1;

        Undo.RecordObject(stageMapData, "Add Stages");

        for (int i = 0; i < amount; i++)
        {
            int index = insertIndex + i;
            verticalRates.InsertArrayElementAtIndex(index);
            verticalRates.GetArrayElementAtIndex(index).floatValue
                = Random.Range(-1f, 1f);
        }

        selectedIndex = insertIndex + amount - 1;
        dataObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(stageMapData);
    }

    /// <summary>
    /// 선택한 카드부터 입력한 수량만큼 스테이지를 삭제한다.
    /// </summary>
    private void RemoveStages(
        SerializedObject dataObject,
        SerializedProperty verticalRates)
    {
        if (verticalRates.arraySize == 0)
        {
            return;
        }

        int removeIndex = selectedIndex < 0
            ? Mathf.Max(0, verticalRates.arraySize - amount)
            : selectedIndex;

        Undo.RecordObject(stageMapData, "Remove Stages");

        for (int i = 0; i < amount && removeIndex < verticalRates.arraySize; i++)
        {
            verticalRates.DeleteArrayElementAtIndex(removeIndex);
        }

        selectedIndex = Mathf.Min(removeIndex, verticalRates.arraySize - 1);
        dataObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(stageMapData);
    }

    /// <summary>
    /// Play Mode에서 현재 마지막 스테이지의 클리어를 시험하는 버튼을 그린다.
    /// </summary>
    private void DrawStageClearTestButton()
    {
        EditorGUILayout.Space();
        EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);

        if (GUILayout.Button("스테이지 클리어 시험"))
        {
            StageMapController stageMapController
                = Object.FindFirstObjectByType<StageMapController>();

            if (stageMapController != null)
            {
                stageMapController.CompleteLatestStageForTest();
            }
        }

        EditorGUI.EndDisabledGroup();
    }
}
