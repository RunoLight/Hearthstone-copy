using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static UnityEngine.Networking.UnityWebRequest.Result;

public class PlayingCard : MonoBehaviour
{
    // Mouse enter when true, exit when false
    public event Action<bool, PlayingCard> OnMouse;
    
    private void OnMouseEnter()
    {
        Debug.Log("mouse enter");
        OnMouse?.Invoke(true, this);
    }

    private void OnMouseExit()
    {
        OnMouse?.Invoke(false, this);
        
    }
    
    public async Task SetSomething(int amount)
    {
        var currentSomething = Convert.ToInt32(textDamage.text);

        await textDamage.transform.DOScale(new Vector3(1.4f, 1.4f, 1f), 0.3f);

        var totalTime = 1f;
        var totalDelta = Mathf.Abs(currentSomething - amount);
        if (totalDelta < 3)
        {
            totalTime = 0.2f;
        }
        var timePerOneDelta = totalTime / totalDelta;
        
        var delta = 0;
        do
        {
            await Task.Delay(TimeSpan.FromSeconds(timePerOneDelta));

            delta = currentSomething > amount ? -1 :
                currentSomething < amount ? 1 :
                0;

            currentSomething += delta;
            textDamage.text = currentSomething.ToString();
            
        } while (delta != 0);
        var t = textDamage.transform.DOScale(new Vector3(1f, 1f, 1f), 0.3f);
        await t;
        await Task.Delay(TimeSpan.FromSeconds(0.2f));
    }

    public int GetSomething()
    {
        return Convert.ToInt32(textDamage.text);
    }

    [SerializeField] private TMP_Text textTitle;
    [SerializeField] private TMP_Text textDescription;
    [SerializeField] private TMP_Text textHealth;
    [SerializeField] private TMP_Text textDamage;
    [SerializeField] private TMP_Text textManaCost;
    [SerializeField] private Image imageAvatar;

    private void Setup(string title, string description, int health, int damage, int manaCost, Sprite avatar)
    {
        textTitle.text = title;
        textDescription.text = description;
        textHealth.text = health.ToString();
        textDamage.text = damage.ToString();
        textManaCost.text = manaCost.ToString();
        imageAvatar.sprite = avatar;
    }




    // Start is called before the first frame update
    IEnumerator Start()
    {
        Sprite sprite = null;

        var request = UnityWebRequestTexture.GetTexture("https://picsum.photos/200/300");
        yield return request.SendWebRequest();
        if (request.result != Success)
        {
            Debug.Log(request.error);
        }
        else
        {
            var texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
        }

        Setup("Kronx Dragongoof", "Description", 2, 3, 4, sprite);
    }
}