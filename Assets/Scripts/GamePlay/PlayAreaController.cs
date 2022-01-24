using System.Collections.Generic;
using DG.Tweening;
using GamePlay;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct CardPosition
{
    public Transform targetPosition;
    public PlayingCard card;
}

public class PlayAreaController : MonoBehaviour
{
    public Transform pointExample;
    public RectTransform pointsParent;
    public Transform cardsParent;
    public List<CardPosition> points = new List<CardPosition>();

    private void RefreshCardPositions(CardPosition newPoint)
    {
        foreach (CardPosition point in points)
        {
            point.card.KillTweens();
            point.card.AddTween(point.card.transform.DOMove(point.targetPosition.position, 1f));
            if (point.card == newPoint.card)
            {
                point.card.AddTween(point.card.transform.DORotate(Vector3.zero, 1f));
            }
        }
    }

    public void AttachCard(PlayingCard playingCard)
    {
        var cardPos = playingCard.transform.position.x;
        var newIndex = 0;
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            if (cardPos > p.targetPosition.position.x)
            {
                newIndex = i;
            }
            else
            {
                newIndex = i;
                break;
            }
        }
        if (newIndex == points.Count - 1 && points.Count != 0)
        {
            if (cardPos > points[points.Count - 1].targetPosition.position.x)
            {
                newIndex++;
            }
        }

        playingCard.transform.SetParent(cardsParent);
        var point = Instantiate(pointExample, pointsParent);
        point.SetSiblingIndex(newIndex);
        
        var cardPosition = new CardPosition
        {
            targetPosition = point,
            card = playingCard
        };
        points.Insert(newIndex, cardPosition);
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(pointsParent);
        RefreshCardPositions(cardPosition);
    }
}
