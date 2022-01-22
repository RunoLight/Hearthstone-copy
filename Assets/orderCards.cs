using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class orderCards : MonoBehaviour
{
    public PlayingCard cardPrefab;

    public int minimalCardAmount;
    public int maximalCardAmount;
    
    public Transform start; //Location where to start adding my cards
    public Transform HandDeck; //The hand panel reference
    [HideInInspector] public int howManyAdded;
    public float gapFromOneItemToTheNextOne; //the gap I need between each card

    public List<PlayingCard> cards;

    public float totalTwist = 20f;
    public float scalingFactor = 0.01f;

    private void Start()
    {
        SpawnCards();
        FitCards();
    }

    public void SpawnCards()
    {
        var cardsAmount = Random.Range(minimalCardAmount, maximalCardAmount);
        for (int i = 0; i < cardsAmount; i++)
        {
            cards.Add(Instantiate(cardPrefab, HandDeck.transform));
        }
    }

    public void FitCards()
    {
        if (cards.Count == 0) //if list is null, stop function
            return;

        howManyAdded = 0;
        // 20f for example, try various values
        var numberOfCards = cards.Count;
        var twistPerCard = totalTwist / numberOfCards;
        var startTwist = numberOfCards % 2 == 0 ? totalTwist / 2f - twistPerCard / 2 : Mathf.Floor(numberOfCards / 2f) * twistPerCard;
        // var startTwist = ;
        // var startTwist = Mathf.Floor(numberOfCards / 2f) * twistPerCard; // % 2 == 1
        // var startTwist = (numberOfCards / 2f - 1)* twistPerCard; // % 2 == 0

        // that should be roughly one-tenth the height of one
        // of your cards, just experiment until it works well

        foreach (var item in cards)
        {
            var tr = item.transform;
            var position = start.position + new Vector3(howManyAdded * gapFromOneItemToTheNextOne, 0, 0);
            tr.position = position;
            tr.SetParent(HandDeck);

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
        Handles.DrawWireDisc(start.transform.position, Vector3.forward, 20f);
        Handles.color = Color.green;
        foreach (var item in cards)
        {
            Handles.DrawWireDisc(item.transform.position, Vector3.forward, 20f);
        }
    }
#endif
}