using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using static UnityEngine.Networking.UnityWebRequest.Result;

public class PlayingCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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
    private Task glowUpTask;
    private Task glowDownTask;
    
    private CanvasGroup canvasGroup;

    /// <summary>
    /// Tweens used to rotate or move card. Used to destroy current working tweens to add new ones
    /// </summary>
    private readonly List<Tween> tweenSequence = new List<Tween>();

    private static readonly float MaxThickness = 1.3f;
    private static readonly float MinThickness = 0f;
    private static readonly float ThicknessStep = 0.06f;
    private static readonly int Thickness = Shader.PropertyToID("_Thickness");

    private static readonly Vector3 PropertySelectedScale = new Vector3(1.4f, 1.4f, 1f);
    private static readonly float PropertySelectionDuration = 0.3f;
    
    private static readonly Vector3 PropertyUnselectedScale = Vector3.one;
    private static readonly float PropertyUnselectDuration = 0.3f;
    
    private static readonly float PropertyEndedAdditionalWaitDuration = 0.2f;
    private static readonly float PropertyChangingTotalDuration = 1f;
    private static readonly float PropertyChangingTotalDurationShort = 0.2f;
    private static readonly int MaximumDeltaForShortDuration = 3;
    
    public void AddTween(Tween tweenToAdd)
    {
        tweenSequence.Add(tweenToAdd);    
    }
    
    public void KillTweens()
    {
        foreach (Tween tween in tweenSequence)
        {
            tween.Kill();
        }
        tweenSequence.Clear();
    }

    #region Properties

    public int Damage { get; private set; }
    public async Task SetDamage(int value)
    {
        var t = SetParameterSmoothly(Damage, value, textDamage);
        Damage = value;
        await t;
    }

    public int Health { get; private set; }
    public async Task SetHealth(int value)
    {
        var t = SetParameterSmoothly(Health, value, textDamage);
        Health = value;
        await t;
    }

    public int ManaCost { get; private set; }
    public async Task SetManaCost(int value)
    {
        var t = SetParameterSmoothly(ManaCost, value, textDamage);
        ManaCost = value;
        await t;
    }

    #endregion
    
    #region Initialization

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            cardImage.material = null;
            materialInstance = Instantiate(outlineMaterial);
            materialInstance.SetFloat(Thickness, MinThickness);
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

    #endregion

    public void Glow(bool active)
    {
        glowTaskCancelSource?.Cancel();
        glowTaskCancelSource = new CancellationTokenSource();
        if (active)
        {
            glowUpTask = GlowStart(glowTaskCancelSource.Token);
        }
        else
        {
            if (glowDownTask is { IsCompleted: false, IsCanceled: false } == false)
            {
                glowDownTask = GlowEnd(glowTaskCancelSource.Token);
            }
        }
    }
    
    #region PointerEvents
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (DeckHand.I.someCardGrabbed && DeckHand.I.selectedCard != this)
        {
            return;
        }
        OnMouse?.Invoke(true, this);
        Glow(true);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        OnMouse?.Invoke(false, this);
        if (DeckHand.I.selectedCard != this)
        {
            Glow(false);
        }
    }

    #endregion
    
    private void Setup(string title, string description, int health, int damage, int manaCost, Sprite avatar)
    {
        textTitle.text = title;
        textDescription.text = description;
        imageAvatar.sprite = avatar;
        
        Health = health;
        textHealth.text = health.ToString();
        Damage = damage;
        textDamage.text = damage.ToString();
        ManaCost = manaCost;
        textManaCost.text = manaCost.ToString();
    }

    private static async Task SetParameterSmoothly(int from, int to, TMP_Text textField)
    {
        if (from == to) return;
        
        var currValue = from;
        float timePerOneDelta;
        int delta;
        
        await textField.transform.DOScale(PropertySelectedScale,PropertySelectionDuration);
        var totalDelta = Mathf.Abs(from - to);
        if (totalDelta < MaximumDeltaForShortDuration)
            timePerOneDelta = PropertyChangingTotalDurationShort / totalDelta;
        else
            timePerOneDelta = PropertyChangingTotalDuration / totalDelta;
        do
        {
            await Task.Delay(TimeSpan.FromSeconds(timePerOneDelta));

            delta = currValue > to ? -1 :
                    currValue < to ?  1 :
                                       0;
            currValue += delta;
            textField.text = currValue.ToString();
            
        } while (delta != 0);
        
        await textField.transform.DOScale(PropertyUnselectedScale, PropertyUnselectDuration);
        await Task.Delay(TimeSpan.FromSeconds(PropertyEndedAdditionalWaitDuration));
    }

    public void SetRaycastTarget(bool isActive)
    {
        canvasGroup.interactable = isActive;
        canvasGroup.blocksRaycasts = isActive;
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
     if (mat.name == "Default UI Material") return;
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

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
    }
}