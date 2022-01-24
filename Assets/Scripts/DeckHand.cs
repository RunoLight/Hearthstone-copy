using System.Collections.Generic;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[System.Serializable]
public struct MapBorders
{
    [SerializeField] private Transform top;
    [SerializeField] private Transform bottom;
    [SerializeField] private Transform left;
    [SerializeField] private Transform right;

    public float MinX => left.transform.position.x;
    public float MaxX => right.transform.position.x;
    public float MinY => bottom.transform.position.y;
    public float MaxY => top.transform.position.y;
}

public class DeckHand : MonoBehaviour
{
    public static DeckHand I;

    public PlayingCard cardPrefab;

    public int minimalCardAmount;
    public int maximalCardAmount;

    public MapBorders borders;
    
    public Transform handDeckCenter;
    public Transform cardsParent;
    public Transform selectedCardParent;

    [SerializeField] private GraphicRaycaster graphicRaycaster;

    [HideInInspector] public int howManyAdded;
    public float gapFromOneItemToTheNextOne;

    public List<PlayingCard> cards;

    public float totalTwist = 20f;
    public float scalingFactor = 0.01f;

    public Button btn;

    private int indexOfSelectedCard;
    public PlayingCard selectedCard;

    public bool someCardGrabbed;
    private readonly PointerEventData clickData = new PointerEventData(EventSystem.current);
    private readonly List<RaycastResult> clickResult = new List<RaycastResult>();

    private void Start()
    {
        I = this;

        SpawnCards();
        FitCards();
        btn.onClick.AddListener(async () =>
        {
            btn.interactable = false;
            foreach (var card in cards)
            {
                var oldAmount = card.Health;
                var newAmount = oldAmount;
                while (oldAmount == newAmount) newAmount = Random.Range(-2, 9);

                await card.SetHealth(newAmount);
            }

            btn.interactable = true;
        });
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            clickData.position = Mouse.current.position.ReadValue();
            clickResult.Clear();
            graphicRaycaster.Raycast(clickData, clickResult);

            if (clickResult.Count == 0) return;
            
            var cardComponent = clickResult[0].gameObject.GetComponentInParent<PlayingCard>();
            if (cardComponent != null)
            {
                selectedCard = cardComponent;
                Debug.Log($"Selected card {cardComponent.name}");
                selectedCard.transform.SetParent(selectedCardParent);
                selectedCard.SetRaycastTarget(false);
                someCardGrabbed = true;

                indexOfSelectedCard = cards.IndexOf(selectedCard);
                cards.Remove(selectedCard);
                selectedCard.transform.DORotate(Vector3.zero, 1f);

                FitCards();
            }
        }
        else if (Mouse.current.leftButton.isPressed)
        {
            var mousePosition = Mouse.current.position.ReadValue();
            if (selectedCard != null)
            {
                var cardPosition = Camera.main.ScreenToWorldPoint(mousePosition);
                cardPosition.z = 0;
                cardPosition.x = Mathf.Clamp(cardPosition.x, borders.MinX, borders.MaxX);
                cardPosition.y = Mathf.Clamp(cardPosition.y, borders.MinY, borders.MaxY);
                selectedCard.transform.position = cardPosition;
            }
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (selectedCard == null) return;
            selectedCard.Glow(false);
                
            clickData.position = Mouse.current.position.ReadValue();
            clickResult.Clear();
            graphicRaycaster.Raycast(clickData, clickResult);
            if (clickResult.Count == 0) return;

            PlayAreaController playAreaController = null;
            foreach (var result in clickResult)
            {
                if (result.gameObject.TryGetComponent(out playAreaController))
                {
                    break;
                }
            }

            if (playAreaController != null)
            {
                playAreaController.AttachCard(selectedCard);
            }
            else
            {
                cards.Insert(indexOfSelectedCard, selectedCard);
                indexOfSelectedCard = 0;
                selectedCard.SetRaycastTarget(true);
                FitCards();
            }
            
            selectedCard = null;
            someCardGrabbed = false;
        }
    }

    public void SpawnCards()
    {
        var cardsAmount = Random.Range(minimalCardAmount, maximalCardAmount);
        for (var i = 0; i < cardsAmount; i++) cards.Add(Instantiate(cardPrefab, cardsParent));

        foreach (var card in cards)
        {
            Debug.Log(card.name);
            card.OnMouse += (isSelected, c) =>
            {
                if (isSelected)
                {
                    c.transform.SetParent(selectedCardParent);
                    c.transform.DOScale(new Vector3(1.2f, 1.2f, 1f), 0.3f);
                }
                else
                {
                    c.transform.SetParent(cardsParent);
                    c.transform.SetSiblingIndex(cards.FindIndex(pc => pc == c));
                    c.transform.DOScale(new Vector3(1f, 1f, 1f), 0.3f);
                }
            };
        }
    }

    private void FitCards()
    {
        if (cards.Count == 0) return;

        howManyAdded = 0;
        var numberOfCards = cards.Count;
        var twistPerCard = totalTwist / numberOfCards;
        var startTwist = numberOfCards % 2 == 0
            ? totalTwist / 2f - twistPerCard / 2
            : Mathf.Floor(numberOfCards / 2f) * twistPerCard;

        var idx = -1;
        foreach (var item in cards)
        {
            idx++;

            var tr = item.transform;
            var position = handDeckCenter.position;

            var halfCards = cards.Count / 2;
            if (cards.Count % 2 == 0)
            {
                if (idx == halfCards)
                {
                    position.x += 0.5f * gapFromOneItemToTheNextOne;
                }
                else if (idx == halfCards - 1)
                {
                    position.x -= 0.5f * gapFromOneItemToTheNextOne;
                }
                else if (idx < halfCards - 1)
                {
                    var deltaFromCenter = Mathf.Abs(idx - (halfCards - 1));

                    position.x -= 0.5f * gapFromOneItemToTheNextOne;
                    position.x -= deltaFromCenter * gapFromOneItemToTheNextOne;
                }
                else if (idx > halfCards)
                {
                    var deltaFromCenter = Mathf.Abs(idx - halfCards);
                    position.x += 0.5f * gapFromOneItemToTheNextOne;
                    position.x += deltaFromCenter * gapFromOneItemToTheNextOne;
                }
            }
            else
            {
                var deltaFromCenter = Mathf.Abs(idx - halfCards);
                var amountToMove = deltaFromCenter * gapFromOneItemToTheNextOne;
                if (idx < halfCards)
                    position.x -= amountToMove;
                else if (idx > halfCards)
                    position.x += amountToMove;
            }

            tr.position = position;
            tr.SetParent(cardsParent);

            var twistForThisCard = startTwist - howManyAdded * twistPerCard;

            item.transform.eulerAngles = new Vector3(0, 0, twistForThisCard);

            var nudgeThisCard = Mathf.Abs(twistForThisCard);
            nudgeThisCard *= scalingFactor;
            item.transform.Translate(0f, -nudgeThisCard, 0f);

            howManyAdded++;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Handles.color = Color.yellow;
        Handles.DrawWireDisc(handDeckCenter.transform.position, Vector3.forward, 0.1f);
        Handles.color = Color.green;
        foreach (var item in cards)
            Handles.DrawWireDisc(item.transform.position, Vector3.forward, 0.1f);
    }
#endif
}