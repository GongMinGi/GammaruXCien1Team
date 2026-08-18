using System.Collections.Generic;
using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    [SerializeField, Range(5, 7)] private int maxHandSize = 5;
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
        while (cards.Count < maxHandSize)
            cards.Add(bag.Draw());

        RefreshDisplay();
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

    public void ReturnCard(int index, ArcanaData card)
    {
        int clampedIndex = Mathf.Clamp(index, 0, cards.Count);
        cards.Insert(clampedIndex, card);
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (handDisplay != null)
            handDisplay.UpdateHand(cards);
    }
}
