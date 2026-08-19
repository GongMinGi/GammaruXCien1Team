using System.Collections.Generic;
using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    [SerializeField, Range(5, 7)] private int maxHandSize = 7;
    [SerializeField] private HandDisplay handDisplay;

    private readonly List<ArcanaData> cards = new();
    private ArcanaBag bag;

    public IReadOnlyList<ArcanaData> Cards => cards;

    public void Initialize(ArcanaBag sourceBag)
    {
        bag = sourceBag;
    }

    public void DrawToCapacity()
    {
        int heldPassiveCount = CountByUsageType(ArcanaUsageType.HeldPassive);
        int targetSize = maxHandSize - heldPassiveCount;
        while (cards.Count < targetSize)
            cards.Add(bag.Draw());

        RefreshDisplay();
    }

    private int CountByUsageType(ArcanaUsageType type)
    {
        int count = 0;
        foreach (ArcanaData card in cards)
            if (card.UsageType == type)
                count++;
        return count;
    }

    public bool TryGetCard(int index, out ArcanaData card)
    {
        if (index < 0 || index >= cards.Count)
        {
            card = null;
            return false;
        }

        card = cards[index];
        return true;
    }

    public bool TryTakeCard(int index, out ArcanaData card)
    {
        if (index < 0 || index >= cards.Count)
        {
            card = null;
            return false;
        }

        card = cards[index];
        cards.RemoveAt(index);
        RefreshDisplay();
        return true;
    }

    public void DrawOne(ArcanaBag sourceBag = null)
    {
        ArcanaBag drawBag = sourceBag ?? bag;
        if (drawBag == null) return;
        cards.Add(drawBag.Draw());
        RefreshDisplay();
    }

    public void ReplaceCard(int index, ArcanaData newCard)
    {
        if (index < 0 || index >= cards.Count || newCard == null) return;
        cards[index] = newCard;
        RefreshDisplay();
    }

    public void ReturnCard(int index, ArcanaData card)
    {
        int clampedIndex = Mathf.Clamp(index, 0, cards.Count);
        cards.Insert(clampedIndex, card);
        RefreshDisplay();
    }

    public bool TryMergeCards(int lo, int hi, ArcanaData result)
    {
        if (result == null || lo < 0 || hi <= lo || hi >= cards.Count)
            return false;

        cards.RemoveAt(hi);
        cards.RemoveAt(lo);
        cards.Insert(lo, result);
        RefreshDisplay();
        return true;
    }

    public void UndoMerge(int resultIndex, ArcanaData source1, int idx1,
                           ArcanaData source2, int idx2)
    {
        if (resultIndex < 0 || resultIndex >= cards.Count)
        {
            Debug.LogError("Invalid merge undo state.", this);
            return;
        }

        cards.RemoveAt(resultIndex);
        cards.Insert(idx1, source1);
        cards.Insert(idx2, source2);
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (handDisplay != null)
            handDisplay.UpdateHand(cards);
    }
}
