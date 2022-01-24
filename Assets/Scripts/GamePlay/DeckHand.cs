using System;
using System.Collections.Generic;
using System.Linq;
using Configs;
using DG.Tweening;
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
        public static DeckHand I;
        public bool SomeCardSelected => selectedCard != null;
        public PlayingCard selectedCard;
        
        [SerializeField] private MapBorders borders;
        [SerializeField] private Transform handDeckCenter;
        [SerializeField] private Transform cardsParent;
        [SerializeField] private Transform selectedCardParent;
        [SerializeField] private Button btn;
        [SerializeField] private GraphicRaycaster graphicRaycaster;
        [SerializeField] private DeckSettings settings;
    
        private readonly List<PlayingCard> cards = new List<PlayingCard>();
        private int indexOfSelectedCard;

        private readonly PointerEventData clickData = new PointerEventData(EventSystem.current);
        private readonly List<RaycastResult> clickResult = new List<RaycastResult>();

        private Camera Сamera;

        private void Awake()
        {
            I = this;
            settings = GameConfigs.I.DeckSettings;
            Сamera = Camera.main;
        }

        private void Start()
        {
            SpawnCards();
            FitCards(true);
            btn.onClick.AddListener(ButtonCallback);
        }

        private async void ButtonCallback()
        {
            btn.interactable = false;

            HashSet<PlayingCard> used = new HashSet<PlayingCard>();
            PlayingCard card = null;
            
            while (cards.Count != 0)
            {
                used.Clear();
                do
                {
                    card = cards.FirstOrDefault(c => used.Contains(c) == false);
                    if (card != null)
                    {
                        used.Add(card);
                        
                        SelectCard(card);
                        var oldAmount = card.Health;
                        var newAmount = oldAmount;
                        while (oldAmount == newAmount) 
                            newAmount = Random.Range(settings.minimalValue, settings.maximalValue);
                    
                        await card.SetHealth(newAmount);
                    }
                } while (card != null);
            }
            Debug.Log("Game completed!");
        }

        private void SelectCard(PlayingCard card)
        {
            if (SomeCardSelected)
            {
                MoveCardToHand(selectedCard, indexOfSelectedCard);
            }
            
            selectedCard = card;
            
            Debug.Log($"Selected card {card.name}");
            selectedCard.transform.SetParent(selectedCardParent);
            selectedCard.SetRaycastTarget(false);

            indexOfSelectedCard = cards.IndexOf(selectedCard);
            cards.Remove(selectedCard);

            selectedCard.KillTweens();
            selectedCard.AddTween(selectedCard.transform.DORotate(Vector3.zero, 0.2f));

            FitCards(false);
        }

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
                if (selectedCard != null)
                {
                    var cardPosition = Сamera.ScreenToWorldPoint(mousePosition);
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
                    MoveCardToHand(selectedCard, indexOfSelectedCard);
                }
                selectedCard = null;
            }
        }

        private void SpawnCards()
        {
            var cardsAmount = Random.Range(settings.minimalCardAmount, settings.maximalCardAmount);
            for (var i = 0; i < cardsAmount; i++) cards.Add(Instantiate(settings.cardPrefab, cardsParent));

            foreach (var card in cards)
            {
                Debug.Log(card.name);
                card.OnMouse += (isSelected, c) =>
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
        public void DeleteCard(PlayingCard card)
        {
            cards.Remove(card);
        }
    }
}