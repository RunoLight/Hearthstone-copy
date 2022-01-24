using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class DeckHand : MonoBehaviour
{
    public static DeckHand I;
    
    public PlayingCard cardPrefab;

    public int minimalCardAmount;
    public int maximalCardAmount;

    public Transform handDeckCenter;
    public Transform cardsParent;
    public Transform selectedCardParent;

    [SerializeField] private GraphicRaycaster graphicRaycaster;
    private readonly PointerEventData clickData = new PointerEventData(EventSystem.current);
    private readonly List<RaycastResult> clickResult = new List<RaycastResult>();

    [HideInInspector] public int howManyAdded;
    public float gapFromOneItemToTheNextOne;

    public List<PlayingCard> cards;

    public float totalTwist = 20f;
    public float scalingFactor = 0.01f;

    public Button btn;

    public PlayingCard selectedCard = null;

    public bool someCardGrabbed = false;

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            clickData.position = Mouse.current.position.ReadValue();
            clickResult.Clear();

            graphicRaycaster.Raycast(clickData, clickResult);

            if (clickResult.Count != 0)
            {
                GameObject ui_element = clickResult[0].gameObject;
                var cardComponent = ui_element.GetComponentInParent<PlayingCard>();
                if (cardComponent != null)
                {
                    selectedCard = cardComponent;
                    Debug.Log($"Selected card {cardComponent.name}");
                    selectedCard.transform.SetParent(selectedCardParent);
                    someCardGrabbed = true;
                }
            }
        }
        else if (Mouse.current.leftButton.isPressed)
        {
            var mousePosition = Mouse.current.position.ReadValue();
            if (selectedCard != null)
            {
                var cardPosition = Camera.main.ScreenToWorldPoint(mousePosition);
                cardPosition.z = 0;
                selectedCard.transform.position = cardPosition;
            }
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (selectedCard != null)
            {
                selectedCard.transform.DOMove(Vector3.one, 1f);
                selectedCard = null;
                someCardGrabbed = false;
            }
        }
    }

    private void Start()
    {
        I = this;
        
        SpawnCards();
        FitCards();
        btn.onClick.AddListener(async () =>
        {
            btn.interactable = false;
            foreach (PlayingCard card in cards)
            {
                var oldAmount = card.GetSomething();
                int newAmount = oldAmount;
                while (oldAmount == newAmount)
                {
                    newAmount = Random.Range(-2, 9);
                }

                await card.SetSomething(newAmount);
            }

            btn.interactable = true;
        });
    }

    public void SpawnCards()
    {
        var cardsAmount = Random.Range(minimalCardAmount, maximalCardAmount);
        for (int i = 0; i < cardsAmount; i++)
        {
            cards.Add(Instantiate(cardPrefab, cardsParent));
        }

        foreach (PlayingCard card in cards)
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
            Vector3 position = Vector3.zero;

            int halfCards = cards.Count / 2;
            if (cards.Count % 2 == 0)
            {
                if (idx == halfCards)
                {
                    position = handDeckCenter.position - new Vector3(0.5f * gapFromOneItemToTheNextOne, 0, 0);
                }
                else if (idx == halfCards - 1)
                {
                    position = handDeckCenter.position + new Vector3(0.5f * gapFromOneItemToTheNextOne, 0, 0);
                }
                else if (idx < halfCards - 1)
                {
                    position = handDeckCenter.position - new Vector3(0.5f * gapFromOneItemToTheNextOne, 0, 0);
                    var deltaFromCenter = Mathf.Abs(idx - (halfCards - 1));
                    position -= new Vector3(deltaFromCenter * gapFromOneItemToTheNextOne, 0, 0);
                }
                else if (idx > halfCards)
                {
                    position = handDeckCenter.position + new Vector3(0.5f * gapFromOneItemToTheNextOne, 0, 0);
                    var deltaFromCenter = Mathf.Abs(idx - (halfCards));
                    position += new Vector3(deltaFromCenter * gapFromOneItemToTheNextOne, 0, 0);
                }
            }
            else
            {
                if (idx < halfCards)
                {
                    position = handDeckCenter.position;
                    var deltaFromCenter = Mathf.Abs(idx - (halfCards));
                    position -= new Vector3(deltaFromCenter * gapFromOneItemToTheNextOne, 0, 0);
                }
                else if (idx > halfCards)
                {
                    position = handDeckCenter.position;
                    var deltaFromCenter = Mathf.Abs(idx - (halfCards));
                    position += new Vector3(deltaFromCenter * gapFromOneItemToTheNextOne, 0, 0);
                }
                else if (idx == halfCards)
                {
                    position = handDeckCenter.position;
                }
            }

            // position = start.position + new Vector3(howManyAdded * gapFromOneItemToTheNextOne, 0, 0);
            tr.position = position;
            tr.SetParent(cardsParent);

            var twistForThisCard = startTwist - howManyAdded * twistPerCard;

            // item.transform.Rotate( 0f, 0f, -6f );
            // twistForThisCard = twistForThisCard * Random.Range(0.9f, 1.1f);
            item.transform.Rotate(0f, 0f, twistForThisCard);

            var nudgeThisCard = Mathf.Abs(twistForThisCard);
            nudgeThisCard *= scalingFactor;
            // nudgeThisCard = nudgeThisCard * Random.Range(0.9f, 1.1f);
            item.transform.Translate(0f, -nudgeThisCard, 0f);

            // items.Remove(item);
            howManyAdded++;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // start
        Handles.color = Color.yellow;
        Handles.DrawWireDisc(handDeckCenter.transform.position, Vector3.forward, 0.1f);
        Handles.color = Color.green;
        foreach (var item in cards)
        {
            Handles.DrawWireDisc(item.transform.position, Vector3.forward, 0.1f);
        }
    }
#endif
}