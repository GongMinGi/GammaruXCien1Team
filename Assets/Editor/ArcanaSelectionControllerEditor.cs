using UnityEditor;
using UnityEngine;

/// <summary>
/// 아르카나 더미의 마지막 카드 번호와 행 배치를 편집한다.
/// </summary>
[CustomEditor(typeof(ArcanaSelectionController))]
public class ArcanaSelectionControllerEditor : Editor
{
    private const string MaximumDisplayedArcanaIdProperty
        = "maximumDisplayedArcanaId";
    private const string DeckRowLayoutProperty = "deckRowLayout";

    private SerializedProperty maximumDisplayedArcanaId;
    private SerializedProperty deckRowLayout;

    /// <summary>
    /// Inspector에서 사용할 직렬화 프로퍼티를 찾는다.
    /// </summary>
    private void OnEnable()
    {
        maximumDisplayedArcanaId = serializedObject.FindProperty(
            MaximumDisplayedArcanaIdProperty);
        deckRowLayout = serializedObject.FindProperty(DeckRowLayoutProperty);
    }

    /// <summary>
    /// 기존 참조와 아르카나 더미 표시 설정을 Inspector에 그린다.
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            MaximumDisplayedArcanaIdProperty,
            DeckRowLayoutProperty);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            "부채꼴 카드 설정",
            EditorStyles.boldLabel);
        maximumDisplayedArcanaId.intValue = EditorGUILayout.IntSlider(
            "마지막 아르카나 번호",
            maximumDisplayedArcanaId.intValue,
            ArcanaCatalog.MinArcanaId + 1,
            ArcanaCatalog.MaxArcanaId - 1);
        EditorGUILayout.PropertyField(
            deckRowLayout,
            new GUIContent("부채꼴 행 배치"));

        serializedObject.ApplyModifiedProperties();
    }
}
