using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ObservationController : MonoBehaviour
{
    [SerializeField] private PlayerHand playerHand;
    [SerializeField] private HandDisplay handDisplay;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerDisplay playerDisplay;

    private bool observationActive;
    private Action onComplete;
    private ArcanaBag bag;
    private ArcanaData[] arcanaPool;

    private bool cardTargetSelectionActive;
    private GameObject observationPromptObject;

    public bool ValidateReferences()
    {
        if (playerHand == null || handDisplay == null ||
            gridManager == null || playerDisplay == null)
        {
            Debug.LogError("ObservationController references are not assigned.", this);
            return false;
        }
        return true;
    }

    public void BeginObservation(ArcanaBag sourceBag, ArcanaData[] pool,
        Action onDone)
    {
        bag = sourceBag;
        arcanaPool = pool;
        onComplete = onDone;
        cardTargetSelectionActive = false;

        if (!HasObservationCard())
        {
            onDone?.Invoke();
            return;
        }

        observationActive = true;
        ShowObservationPrompt();
        Debug.Log("관측 페이즈: 관측 카드 사용(숫자키) 또는 Enter/ESC로 스킵.");
    }

    private void Update()
    {
        if (!observationActive)
            return;

        HandleInput();
    }

    private void HandleInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null)
            return;

        if (cardTargetSelectionActive)
        {
            if (kb.digit1Key.wasPressedThisFrame) CompleteTransform(0);
            else if (kb.digit2Key.wasPressedThisFrame) CompleteTransform(1);
            else if (kb.digit3Key.wasPressedThisFrame) CompleteTransform(2);
            else if (kb.digit4Key.wasPressedThisFrame) CompleteTransform(3);
            else if (kb.digit5Key.wasPressedThisFrame) CompleteTransform(4);
            else if (kb.digit6Key.wasPressedThisFrame) CompleteTransform(5);
            else if (kb.digit7Key.wasPressedThisFrame) CompleteTransform(6);
            else if (kb.escapeKey.wasPressedThisFrame)
                cardTargetSelectionActive = false;
            return;
        }

        if (kb.enterKey.wasPressedThisFrame ||
            kb.numpadEnterKey.wasPressedThisFrame ||
            kb.escapeKey.wasPressedThisFrame)
        {
            EndObservation();
            return;
        }

        if (kb.digit1Key.wasPressedThisFrame) UseObservationCard(0);
        else if (kb.digit2Key.wasPressedThisFrame) UseObservationCard(1);
        else if (kb.digit3Key.wasPressedThisFrame) UseObservationCard(2);
        else if (kb.digit4Key.wasPressedThisFrame) UseObservationCard(3);
        else if (kb.digit5Key.wasPressedThisFrame) UseObservationCard(4);
        else if (kb.digit6Key.wasPressedThisFrame) UseObservationCard(5);
        else if (kb.digit7Key.wasPressedThisFrame) UseObservationCard(6);
    }

    private void UseObservationCard(int handIndex)
    {
        if (!playerHand.TryGetCard(handIndex, out ArcanaData card))
            return;

        if (card.UsageType != ArcanaUsageType.Observation)
            return;

        switch (card.ObservationAction)
        {
            case ObservationActionType.DrawCard:
                playerHand.TryTakeCard(handIndex, out _);
                playerHand.DrawOne(bag);
                Debug.Log($"{card.DisplayNumber} {card.KoreanName}: 카드 1장 드로우.");
                break;

            case ObservationActionType.TransformCard:
                playerHand.TryTakeCard(handIndex, out _);
                cardTargetSelectionActive = true;
                Debug.Log("변환할 카드를 선택하세요 (숫자키).");
                break;

            default:
                return;
        }

        if (!HasObservationCard() && !cardTargetSelectionActive)
            EndObservation();
    }

    private void CompleteTransform(int targetIndex)
    {
        if (!playerHand.TryGetCard(targetIndex, out ArcanaData targetCard))
            return;

        ArcanaData replacement = PickRandomFromPool();
        if (replacement == null)
        {
            cardTargetSelectionActive = false;
            return;
        }

        playerHand.ReplaceCard(targetIndex, replacement);
        cardTargetSelectionActive = false;
        Debug.Log($"{targetCard.DisplayNumber} → {replacement.DisplayNumber} {replacement.KoreanName}");

        if (!HasObservationCard())
            EndObservation();
    }

    private ArcanaData PickRandomFromPool()
    {
        if (arcanaPool == null || arcanaPool.Length == 0)
            return null;

        var handIds = new HashSet<int>();
        foreach (ArcanaData card in playerHand.Cards)
            handIds.Add(card.Id);

        var candidates = new List<ArcanaData>();
        foreach (ArcanaData card in arcanaPool)
        {
            if (!handIds.Contains(card.Id))
                candidates.Add(card);
        }

        if (candidates.Count == 0)
            return arcanaPool[UnityEngine.Random.Range(0, arcanaPool.Length)];

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private bool HasObservationCard()
    {
        foreach (ArcanaData card in playerHand.Cards)
        {
            if (card.UsageType == ArcanaUsageType.Observation)
                return true;
        }
        return false;
    }

    private void EndObservation()
    {
        observationActive = false;
        cardTargetSelectionActive = false;
        HideObservationPrompt();
        Action callback = onComplete;
        onComplete = null;
        callback?.Invoke();
    }

    private void ShowObservationPrompt()
    {
        HideObservationPrompt();
        observationPromptObject = new GameObject("ObservationPrompt");
        observationPromptObject.transform.SetParent(transform);
        Vector2Int gridPos = playerDisplay.GridPosition;
        Vector3 pos = gridManager.GridToWorldPosition(gridPos.x, gridPos.y);
        observationPromptObject.transform.position = pos + Vector3.up * 0.8f;

        TextMesh text = observationPromptObject.AddComponent<TextMesh>();
        text.text = "\uad00\ucee1: \uc22b\uc790\ud0a4\ub85c \uc0ac\uc6a9 / Enter\ub85c \uc2a4\ud0b5";
        text.anchor = TextAnchor.MiddleCenter;
        text.fontSize = 32;
        text.characterSize = 0.06f;
        text.color = new Color(0.4f, 0.8f, 1f);

        MeshRenderer mr = observationPromptObject.GetComponent<MeshRenderer>();
        mr.sortingOrder = 20;
    }

    private void HideObservationPrompt()
    {
        if (observationPromptObject != null)
        {
            Destroy(observationPromptObject);
            observationPromptObject = null;
        }
    }
}
