using System;
using TMPro;
using UnityEngine;

namespace SimpleTooltip.Scripts.Models
{
    [Serializable]
    public class TextStyleData
    {
        public string id; // Ej: "Title", "Body", "Rare", "Legendary"
        public Color color = Color.white;
        public TMP_FontAsset fontAsset;
        public float fontSize = 14f;
        public FontStyles fontStyle = FontStyles.Normal;
        public TextAlignmentOptions alignment = TextAlignmentOptions.Left;
    }
}
