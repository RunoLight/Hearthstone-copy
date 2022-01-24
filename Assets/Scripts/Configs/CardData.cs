using UnityEngine;

namespace Configs
{
    public enum ImageSourceType
    {
        AssetReference, Url
    }

    [CreateAssetMenu(menuName = "Card data", fileName = "Card data")]
    public class CardData : ScriptableObject
    {
        public string title;
        public string description;
        public int damage = 5;
        public int health = 5;
        public int manaCost = 5;
        public ImageSourceType sourceType = ImageSourceType.Url;
        public Sprite image;
        public string imageUrl = "https://picsum.photos/200/300";
    }
}