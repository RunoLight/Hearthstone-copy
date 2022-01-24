using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Configs;
using DG.Tweening;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace GamePlay
{
    [Serializable]
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
        public static PlayingCard SelectedCard;
        
        /// <summary> Used to restrict the area where card can be dragged </summary>
        [SerializeField] private MapBorders borders;
        /// <summary> Deck is centered here</summary>
        [SerializeField] private Transform handDeckCenter;
        [SerializeField] private Transform cardsParent;
        [SerializeField] private Transform selectedCardParent;
        [SerializeField] private Button btn;
        [SerializeField] private GraphicRaycaster graphicRaycaster;
        [SerializeField] private DeckSettings settings;
        [SerializeField] private TMP_Text gameEndText;
    
        /// <summary> Cards currently in hand</summary>
        private readonly List<PlayingCard> cards = new List<PlayingCard>();
        private int indexOfSelectedCard;

        // Cache to minimize allocation
        private readonly PointerEventData clickData = new PointerEventData(EventSystem.current);
        private readonly List<RaycastResult> clickResult = new List<RaycastResult>();
        private Camera Сamera;

        private void Awake()
        {
            settings = GameConfigs.I.DeckSettings;
            GameConfigs.I.deck.Prepare();
            Сamera = Camera.main;
            btn.onClick.AddListener(ButtonCallback);
        }

        private async void Start()
        {
            await SpawnCards();
            FitCards(true);
        }

        /// <summary>
        /// Run in cycles, changes health of cards in hand from left to right, ends when hand become empty
        /// </summary>
        private async void ButtonCallback()
        {
            btn.interactable = false;

            HashSet<PlayingCard> used = new HashSet<PlayingCard>();
            PlayingCard card = null;
            
            while (cards.Count != 0)
            {
                // One cycle of parameter change through all cards present in hand
                {
                    used.Clear();
                    do
                    {
                        card = cards.FirstOrDefault(c => used.Contains(c) == false);
                        if (card == null) continue;
                        
                        used.Add(card);
                        FitCards(false);
                        card.transform.SetAsLastSibling();
                        await GenerateNewHealth(card);
                    } while (card != null);
                }
            }
            
            if (SelectedCard != null)
            {
                // Wait until card drag will end
                while (SelectedCard != null)
                {
                    await Task.Yield();
                }

                bool cardGotBackToHand = cards.Count != 0;
                if (cardGotBackToHand)
                {
                    while (cards.Count != 0)
                    {
                        await GenerateNewHealth(cards[0]);
                    }
                }
            }
            
            gameEndText.gameObject.SetActive(true);
            Debug.Log("Game completed!");

            async Task GenerateNewHealth(PlayingCard c)
            {
                var oldAmount = c.Health;
                var newAmount = oldAmount;
                while (oldAmount == newAmount) 
                    newAmount = Random.Range(settings.minimalValue, settings.maximalValue);
                    
                await c.SetHealth(newAmount);
            }
        }

        private void SelectCard(PlayingCard card)
        {
            if (card.availableToSelect == false)
            {
                return;
            }
            
            if (SelectedCard != null)
            {
                MoveCardToHand(SelectedCard, indexOfSelectedCard);
            }

            SelectedCard = card;
            
            Debug.Log($"Selected card {card.name}");
            SelectedCard.transform.SetParent(selectedCardParent);
            SelectedCard.SetRaycastTarget(false);

            indexOfSelectedCard = cards.IndexOf(SelectedCard);
            cards.Remove(SelectedCard);

            SelectedCard.KillTweens();
            SelectedCard.AddTween(SelectedCard.transform.DORotate(Vector3.zero, 0.2f));

            FitCards(false);
        }

        /// <summary></summary>
        /// <param name="card"></param>
        /// <param name="index">index in card array from positioned left to right</param>
        private void MoveCardToHand(PlayingCard card, int index)
        {
            cards.Insert(index, card);
            card.SetRaycastTarget(true);
            card.transform.SetParent(cardsParent);
            card.transform.SetSiblingIndex(index);
            card.transform.DOScale(settings.scaleBack, settings.scaleBackDuration);
            
            card.transform.SetParent(cardsParent);
            card.transform.SetSiblingIndex(cards.FindIndex(c => c == card));
            card.transform.DOScale(settings.scaleBack, settings.scaleBackDuration);
            
            FitCards(false);
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
                    SelectCard(cardComponent);
                }
            }
            else if (Mouse.current.leftButton.isPressed)
            {
                var mousePosition = Mouse.current.position.ReadValue();
                if (SelectedCard != null)
                {
                    var cardPosition = Сamera.ScreenToWorldPoint(mousePosition);
                    cardPosition.z = 0;
                    cardPosition.x = Mathf.Clamp(cardPosition.x, borders.MinX, borders.MaxX);
                    cardPosition.y = Mathf.Clamp(cardPosition.y, borders.MinY, borders.MaxY);
                    SelectedCard.transform.position = cardPosition;
                }
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                if (SelectedCard == null) return;
                SelectedCard.Glow(false);

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
                    playAreaController.AttachCard(SelectedCard);
                }
                else
                {
                    MoveCardToHand(SelectedCard, indexOfSelectedCard);
                }
                SelectedCard = null;
            }
        }

        private async Task SpawnCards()
        {
            var cardsAmount = Random.Range(settings.minimalCardAmount, settings.maximalCardAmount);
            var currentHandData = new List<CardData>(cardsAmount);
            for (var i = 0; i < cardsAmount; i++)
                currentHandData.Add(await GameConfigs.I.deck.GetCard());

            for (var i = 0; i < cardsAmount; i++)
            {
                var data = currentHandData[i];
                var newCard = Instantiate(settings.cardPrefab, cardsParent);
                newCard.Setup(data);
                newCard.OnHover += (isSelected, c) =>
                {
                    if (isSelected)
                    {
                        c.transform.SetParent(selectedCardParent);
                        c.transform.DOScale(settings.scaleCardWhenSelected, settings.scaleCardDuration);
                    }
                    else
                    {
                        c.transform.SetParent(cardsParent);
                        c.transform.SetSiblingIndex(cards.FindIndex(pc => pc == c));
                        c.transform.DOScale(settings.scaleBack, settings.scaleBackDuration);
                    }
                };
                newCard.OnDestroy += () =>
                {
                    DeleteCard(newCard);
                };
                cards.Add(newCard);
            }
        }

        private void FitCards(bool isInstant)
        {
            if (cards.Count == 0) return;

            var howManyAdded = 0;
            var numberOfCards = cards.Count;
            var twistPerCard = settings.totalTwist / numberOfCards;
            var startTwist = numberOfCards % 2 == 0
                ? settings.totalTwist / 2f - twistPerCard / 2
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
                        position.x += 0.5f * settings.gapFromOneItemToTheNextOne;
                    }
                    else if (idx == halfCards - 1)
                    {
                        position.x -= 0.5f * settings.gapFromOneItemToTheNextOne;
                    }
                    else if (idx < halfCards - 1)
                    {
                        var deltaFromCenter = Mathf.Abs(idx - (halfCards - 1));

                        position.x -= 0.5f * settings.gapFromOneItemToTheNextOne;
                        position.x -= deltaFromCenter * settings.gapFromOneItemToTheNextOne;
                    }
                    else if (idx > halfCards)
                    {
                        var deltaFromCenter = Mathf.Abs(idx - halfCards);
                        position.x += 0.5f * settings.gapFromOneItemToTheNextOne;
                        position.x += deltaFromCenter * settings.gapFromOneItemToTheNextOne;
                    }
                }
                else
                {
                    var deltaFromCenter = Mathf.Abs(idx - halfCards);
                    var amountToMove = deltaFromCenter * settings.gapFromOneItemToTheNextOne;
                    if (idx < halfCards)
                        position.x -= amountToMove;
                    else if (idx > halfCards)
                        position.x += amountToMove;
                }

                tr.SetParent(cardsParent);
                tr.SetSiblingIndex(idx);

                var twistForThisCard = startTwist - howManyAdded * twistPerCard;
                var rotationAnglesForThisCard = new Vector3(0, 0, twistForThisCard);
                var nudgeThisCard = Mathf.Abs(twistForThisCard) * settings.scalingFactor;
                position.y -= nudgeThisCard;

                if (isInstant)
                {
                    tr.position = position;
                    item.transform.eulerAngles = rotationAnglesForThisCard;
                }
                else
                {
                    item.KillTweens();
                    item.AddTween(tr.DOMove(position, settings.cardFitDuration));
                    item.AddTween(item.transform.DORotate(rotationAnglesForThisCard, settings.cardFitDuration));
                }

                howManyAdded++;
            }
        }

        private void DeleteCard(PlayingCard card)
        {
            cards.Remove(card);
            SelectedCard = null;
            FitCards(false);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(handDeckCenter.transform.position, Vector3.forward, 0.1f);
            Handles.color = Color.green;
            for (int i = 0; i < cards.Count; i++)
            {
                var c = cards[i];
                if (c != null)
                {
                    Handles.DrawWireDisc(c.transform.position, Vector3.forward, 0.1f);
                }
            }
        }
#endif
    }
}