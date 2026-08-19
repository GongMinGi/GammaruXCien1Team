using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class ArcanaCodexPanelTests
{
    private const string CatalogPath = "Assets/Data/Arcana/ArcanaCatalog.asset";

    [Test]
    public void FormatListLabel_ProducesNumberNameKoreanForm()
    {
        ArcanaData card = ScriptableObject.CreateInstance<ArcanaData>();
        SerializedObject so = new SerializedObject(card);
        so.FindProperty("displayNumber").stringValue = "I";
        so.FindProperty("arcanaName").stringValue = "The Magician";
        so.FindProperty("koreanName").stringValue = "마법사";
        so.ApplyModifiedPropertiesWithoutUndo();

        Assert.AreEqual("I - The Magician / 마법사",
            ArcanaCodexPanel.FormatListLabel(card));

        UnityEngine.Object.DestroyImmediate(card);
    }

    [Test]
    public void Catalog_HasCompleteDisplayDataForIds1To20()
    {
        ArcanaCatalog catalog =
            AssetDatabase.LoadAssetAtPath<ArcanaCatalog>(CatalogPath);
        Assert.IsNotNull(catalog, $"{CatalogPath}를 불러오지 못했습니다.");

        for (int id = 1; id <= 20; id++)
        {
            ArcanaData card = catalog.GetById(id);
            Assert.IsNotNull(card, $"id {id} 아르카나가 없습니다.");
            Assert.IsNotEmpty(card.DisplayNumber, $"id {id} DisplayNumber가 비어 있습니다.");
            Assert.IsNotEmpty(card.ArcanaName, $"id {id} ArcanaName이 비어 있습니다.");
            Assert.IsNotEmpty(card.KoreanName, $"id {id} KoreanName이 비어 있습니다.");
            Assert.IsNotEmpty(card.EffectDescription, $"id {id} EffectDescription이 비어 있습니다.");
        }
    }

    [Test]
    public void UsageLabel_CoversEveryUsageType()
    {
        foreach (ArcanaUsageType type in
            (ArcanaUsageType[])Enum.GetValues(typeof(ArcanaUsageType)))
        {
            Assert.IsNotEmpty(ArcanaInfoPanel.UsageLabel(type),
                $"{type}에 대한 표기가 비어 있습니다.");
        }
    }
}
