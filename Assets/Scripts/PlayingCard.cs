using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using static UnityEngine.Networking.UnityWebRequest.Result;

public class PlayingCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Mouse enter when true, exit when false
    public event Action<bool, PlayingCard> OnMouse;
    
    [SerializeField] private TMP_Text textTitle;
    [SerializeField] private TMP_Text textDescription;
    [SerializeField] private TMP_Text textHealth;
    [SerializeField] private TMP_Text textDamage;
    [SerializeField] private TMP_Text textManaCost;
    [SerializeField] private Image imageAvatar;
    
    [SerializeField] private Image cardImage;
    [SerializeField] private Material outlineMaterial;
    private Material materialInstance;

    private CancellationTokenSource glowTaskCancelSource;
    private Task glowTask;
    
    private static readonly float MaxThickness = 1.3f;
    private static readonly float MinThickness = 0f;
    private static readonly float ThicknessStep = 0.06f;
    private static readonly int Thickness = Shader.PropertyToID("_Thickness");
    
    private void Awake()
    {
        cardImage.material = null;
        materialInstance = Instantiate(outlineMaterial);
        materialInstance.SetFloat(Thickness, 0f);
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (DeckHand.I.someCardGrabbed && DeckHand.I.selectedCard != this)
        {
            return;
        }
        OnMouse?.Invoke(true, this);
        glowTaskCancelSource?.Cancel();
        glowTaskCancelSource = new CancellationTokenSource();
        glowTask = GlowStart(glowTaskCancelSource.Token);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        OnMouse?.Invoke(false, this);
        
        glowTaskCancelSource?.Cancel();
        glowTaskCancelSource = new CancellationTokenSource();
        glowTask = GlowEnd(glowTaskCancelSource.Token);
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
        
        int delta;

        do
        {
            await Task.Delay(TimeSpan.FromSeconds(timePerOneDelta));

            delta = currentSomething > amount ? -1 :
                    currentSomething < amount ?  1 :
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


    private void Setup(string title, string description, int health, int damage, int manaCost, Sprite avatar)
    {
        textTitle.text = title;
        textDescription.text = description;
        textHealth.text = health.ToString();
        textDamage.text = damage.ToString();
        textManaCost.text = manaCost.ToString();
        imageAvatar.sprite = avatar;
    }

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

    #region GlowEffect

    private async Task GlowStart(CancellationToken t)
    {
        cardImage.material = materialInstance;
        
        var mat = cardImage.material;
        var newThickness = mat.GetFloat(Thickness);
        while (newThickness < MaxThickness)
        {
            newThickness = Mathf.Clamp(newThickness + ThicknessStep, MinThickness, MaxThickness);
            mat.SetFloat(Thickness, newThickness);
            await Task.Yield();
            if (t.IsCancellationRequested)
            {
                return;
            }
        }
    }
    
    // TODO: Если отпустить карту на то чтобы она поехала сама и не двигать мышкой то не будет спадать glow
    private async Task GlowEnd(CancellationToken t)
    {
        var mat = cardImage.material;
        var newThickness = mat.GetFloat(Thickness);
        while (newThickness > MinThickness)
        {
            newThickness = Mathf.Clamp(newThickness - ThicknessStep, MinThickness, MaxThickness);
            mat.SetFloat(Thickness, newThickness);
            await Task.Yield();
            if (t.IsCancellationRequested)
            {
                return;
            }
        }
        cardImage.material = null;
    }

    #endregion
}