using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Configs;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace GamePlay
{
    [Serializable]
    public struct CardDeck
    {
        [SerializeField] private List<CardData> cardConfigs;
        private LinkedList<CardData> configsListRuntime;
        public void Prepare()
        {
            configsListRuntime = new LinkedList<CardData>(cardConfigs);
            foreach (CardData data in configsListRuntime)
            {
                if (data.sourceType == ImageSourceType.Url)
                {
                    var request = UnityWebRequestTexture.GetTexture(data.imageUrl);
                    request.SendWebRequest().completed += operation =>
                    {
                        if (request.result != UnityWebRequest.Result.Success)
                        {
                            Debug.Log(request.error);
                        }
                        else
                        {
                            var texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                            data.image = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                                                       new Vector2(0, 0));
                        }
                    };
                }
            }
        }

        public async Task<CardData> GetCard()
        {
            var idx = Random.Range(0, configsListRuntime.Count);
            var elem = configsListRuntime.ElementAt(idx);
            configsListRuntime.Remove(elem);
            while (elem.image == null)
                await Task.Yield();
            
            return elem;
        }
    }
    
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

        public CardDeck deck;
    }
}