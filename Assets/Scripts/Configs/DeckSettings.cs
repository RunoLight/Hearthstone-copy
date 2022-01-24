using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "Deck Settings", menuName = "Deck Settings")]
    public class DeckSettings : ScriptableObject
    {
        public int minimalCardAmount = 4;
        public int maximalCardAmount = 6;
        
        public PlayingCard cardPrefab;
        
        public float gapFromOneItemToTheNextOne = 1;

        public float totalTwist = 20f;
        public float scalingFactor = 0.01f;

        public Vector3 scaleCardWhenSelected = new Vector3(1.2f, 1.2f, 1f);
        public float scaleCardDuration = 0.2f;
        
        public Vector3 scaleBack = Vector3.one;
        public float scaleBackDuration = 0.2f;
    }
}