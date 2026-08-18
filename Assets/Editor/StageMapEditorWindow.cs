using UnityEditor;
using UnityEngine;

/// <summary>
/// 스테이지 카드의 X 간격과 입력한 수량만큼의 추가 및 삭제를 편집한다.
/// </summary>
public class StageMapEditorWindow : EditorWindow
{
    private const string DataPath = "Assets/Data/StageMapData.asset";
    private const string VerticalRatesProperty = "verticalRates";
    private const string HorizontalSpacingProperty = "horizontalSpacing";

    private StageMapData stageMapData;
    private Vector2 scrollPosition;
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
    /// 데이터 선택란과 X 간격, 카드 추가 및 삭제 조작부를 그린다.
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
}
