using System.Collections;
using System.Collections.Generic;
using Configs;
using UnityEngine;
using UnityEngine.Serialization;

public class GameConfigs : MonoBehaviour
{
    public static GameConfigs I;

    public CardSettings CardSettings => cardSettings;
    [SerializeField] private CardSettings cardSettings;
    
    void Start()
    {
        I = this;
    }
}
