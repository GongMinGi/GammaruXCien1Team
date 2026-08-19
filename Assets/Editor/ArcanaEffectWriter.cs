using UnityEditor;
using UnityEngine;

public static class ArcanaEffectWriter
{
    [MenuItem("Tools/Write Arcana Effect Descriptions")]
    public static void WriteAll()
    {
        string[] descriptions = new string[]
        {
            "",
            "약한 무속성 피해. 이번 턴 속성 공격 사용 시 동일 속성 부여, 피해량 증가.",
            "소모하여 카드 1장 드로우. (관측 페이즈)",
            "일정량의 체력을 회복합니다.",
            "다음 공격 1회에 속성 하나를 선택해 부여합니다.",
            "3칸 타이밍 동안 받는 피해 감소. (절댓값)",
            "보유 시 사망할 경우 체력 10%로 부활. 드로우 1장 감소. 전투 당 1회.",
            "지정 방향으로 3칸 돌진합니다.",
            "무속성 중간 피해를 입힙니다.",
            "해당 타이밍 공격 회피. 즉사기 불가. 3턴 재사용 대기.",
            "카드 1장을 랜덤한 해금 카드로 변환. (관측 페이즈)",
            "시전 중 피해 감소. 완료 시 무속성 중간 피해. 피격 시 피해량 증가.",
            "4칸 동안 피해 1회를 3칸에 걸쳐 분산. 8칸 이후 사용 불가.",
            "시전 중 받는 피해 증가. 완료 시 무속성 강한 피해.",
            "다음 카드 시전 시간 1 감소. 은둔자/악마와 함께 사용 불가.",
            "다음 카드 코스트 +1, 효과 1회 추가 적용. 은둔자/절제와 함께 사용 불가.",
            "주변 8칸 중 1곳에 탑 배치. 탑은 공격 시 동일 속성 추가 피해.",
            "별 속성 강한 피해 3회. 피해 시마다 체력 회복.",
            "달 속성 강한 피해 3회. 시전 순간 공격 회피.",
            "해 속성 강한 피해 3회. 피해 시마다 화상 중첩 부여.",
            "패의 모든 카드 소거 후 매우 강한 무속성 피해. 소거 카드 합에 비례해 피해 증가.",
            "멸망하는 세계의 운명을 뒤엎고, 새로운 운명을 창조합니다."
        };

        int count = 0;
        for (int i = 0; i < descriptions.Length; i++)
        {
            string path = $"Assets/Data/Arcana/Arcana{i:D2}.asset";
            ArcanaData data = AssetDatabase.LoadAssetAtPath<ArcanaData>(path);
            if (data == null)
            {
                Debug.LogWarning($"[ArcanaEffectWriter] {path} 없음, 건너뜀");
                continue;
            }

            SerializedObject so = new SerializedObject(data);
            SerializedProperty prop = so.FindProperty("effectDescription");
            if (prop == null)
            {
                Debug.LogError($"[ArcanaEffectWriter] {path}에 effectDescription 프로퍼티 없음");
                continue;
            }

            prop.stringValue = descriptions[i];
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(data);
            count++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[ArcanaEffectWriter] {count}개 아르카나 효과 설명 입력 완료");
    }
}
