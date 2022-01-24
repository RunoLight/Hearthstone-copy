using Configs;
using UnityEngine;

namespace GamePlay
{
    public class GameConfigs : MonoBehaviour
    {
        public static GameConfigs I;

        GameConfigs()
        {
            I = this;
        }
    
        public CardSettings CardSettings => cardSettings;
        [SerializeField] private CardSettings cardSettings;

        public DeckSettings DeckSettings => deckSettings;
        [SerializeField] private DeckSettings deckSettings;


    }
}