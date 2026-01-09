using System;
using TMPro;
using UnityEngine;

namespace SimpleTooltip.Scripts.Models
{
    /// <summary>
    /// Defines the visual style properties for text elements within a tooltip.
    /// </summary>
    [Serializable]
    public class TextStyleData
    {
        /// <summary>
        /// Unique identifier for the style (e.g., "Title", "Body", "Rare", "Legendary").
        /// </summary>
        public string ID;

        /// <summary>
        /// The color of the text.
        /// </summary>
        public Color Color = Color.white;

        /// <summary>
        /// The TextMeshPro font asset to use.
        /// </summary>
        public TMP_FontAsset FontAsset;

        /// <summary>
        /// The size of the font.
        /// </summary>
        public float FontSize = 14f;

        /// <summary>
        /// The font style (e.g., Bold, Italic).
        /// </summary>
        public FontStyles FontStyle = FontStyles.Normal;

        /// <summary>
        /// The text alignment options.
        /// </summary>
        public TextAlignmentOptions Alignment = TextAlignmentOptions.Left;
    }
}
