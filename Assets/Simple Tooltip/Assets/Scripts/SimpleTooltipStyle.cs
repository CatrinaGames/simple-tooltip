using System;
using System.Collections.Generic;
using SimpleTooltip.Scripts.Models;
using UnityEngine;

namespace SimpleTooltip.Scripts
{
    [Serializable]
    [CreateAssetMenu]
    public class SimpleTooltipStyle : ScriptableObject
    {
        [Header("Main Container")]
        public Sprite BackgroundSprite;
        public Color BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        public Vector4 padding = new Vector4(15, 15, 15, 15);

        [Header("Standard Blocks")]
        public GameObject TextPrefab;       // TextMeshProUGUI
        public GameObject ImagePrefab;      // Image (Preserve Aspect)

        [Header("Advanced Blocks")]
        public GameObject SeparatorPrefab;  // Image (altura pequeña, stretch width)
        public GameObject HeaderPrefab;     // Layout Horizontal: [Image] + [Vertical Layout (Title, Subtitle)]
        public GameObject KeyValuePrefab;   // Layout Horizontal: [Text (Left)] + [Text (Right)]

        [Header("Typography")]
        public List<TextStyleData> TextStyles;

        public TextStyleData GetTextStyle(string id) => TextStyles.Find(x => x.id == id);
    }
}
