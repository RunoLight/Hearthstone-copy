using UnityEngine;
using UnityEngine.UI;

public enum ImageSourceType
{
    AssetReference, Url
}

[CreateAssetMenu(menuName = "Card data", fileName = "Card data")]
public class CardData : ScriptableObject
{
    public string title;
    public string description;
    public int damage;
    public int health;
    public int manaCost;
    public ImageSourceType sourceType;
    public Image image;
    public string imageUrl;
}
