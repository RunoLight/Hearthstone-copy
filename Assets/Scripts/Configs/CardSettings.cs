using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "Card Settings", menuName = "Card Settings")]
    public class CardSettings : ScriptableObject
    {
        [Header("Shader settings")]
        public float MaxThickness = 1.3f;
        public float MinThickness = 0f;
        public float ThicknessStep = 0.06f;
        [Header("Shader property id")]
        public int Thickness = Shader.PropertyToID("_Thickness");
        [Space(20)]
        [Header("Scale when smooth editing some of card parameters")]
        public Vector3 PropertySelectedScale = new Vector3(1.4f, 1.4f, 1f);
        public float PropertySelectionDuration = 0.3f;
        [Header("Scale back")]
        public Vector3 PropertyUnselectedScale = Vector3.one;
        public float PropertyUnselectDuration = 0.3f;
        [Header("Additional waiting when scaling back ended")]
        public float PropertyEndedAdditionalWaitDuration = 0.2f;
        [Header("Long")]
        [Header("Total duration of smooth editing value")]
        public float PropertyChangingTotalDuration = 1f;
        [Header("Short")]
        public float PropertyChangingTotalDurationShort = 0.2f;
        [Header("Maximum cards for short duration")]
        public int MaximumDeltaForShortDuration = 3;
    }
}