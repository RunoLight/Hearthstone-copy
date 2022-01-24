using GamePlay;
using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "Deck Settings", menuName = "Deck Settings")]
    public class DeckSettings : ScriptableObject
    {
        [Header("Cards amount when generated")]
        public int minimalCardAmount = 4;
        public int maximalCardAmount = 6;
        [Space(15)]
        public PlayingCard cardPrefab;
        [Header("Space between cards in hand")]        
        public float gapFromOneItemToTheNextOne = 1;
        [Header("Total degree twist of hand cards")]
        public float totalTwist = 20f;
        [Header("Bend value for arc where card positioned at")]
        public float scalingFactor = 0.01f;
        [Header("Hover selection")]
        public Vector3 scaleCardWhenSelected = new Vector3(1.2f, 1.2f, 1f);
        public float scaleCardDuration = 0.2f;
        [Header("Deselection")]
        public Vector3 scaleBack = Vector3.one;
        public float scaleBackDuration = 0.2f;
        [Header("New value generator")] 
        public int minimalValue = -2;
        public int maximalValue = 9;
        [Space(10)] 
        public float cardFitDuration = 0.3f;
    }
}