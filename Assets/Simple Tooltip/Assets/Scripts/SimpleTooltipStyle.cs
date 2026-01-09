using System;
using System.Collections.Generic;
using SimpleTooltip.Scripts.Models;
using UnityEngine;

namespace SimpleTooltip.Scripts
{
    /// <summary>
    /// ScriptableObject that defines the visual style and prefabs for the tooltip system.
    /// </summary>
    [Serializable]
    [CreateAssetMenu]
    public class SimpleTooltipStyle : ScriptableObject
    {
        /// <summary>
        /// The background sprite for the tooltip panel.
        /// </summary>
        [Header("Main Container")]
        public Sprite BackgroundSprite;

        /// <summary>
        /// The background color tint.
        /// </summary>
        public Color BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        /// <summary>
        /// Padding for the content inside the tooltip (Left, Top, Right, Bottom).
        /// </summary>
        public Vector4 Padding = new Vector4(15, 15, 15, 15);

        /// <summary>
        /// Prefab for text blocks (must contain TextMeshProUGUI).
        /// </summary>
        [Header("Standard Blocks")]
        public GameObject TextPrefab;       // TextMeshProUGUI

        /// <summary>
        /// Prefab for image blocks (must contain Image).
        /// </summary>
        public GameObject ImagePrefab;      // Image (Preserve Aspect)

        /// <summary>
        /// Prefab for a separator line.
        /// </summary>
        [Header("Advanced Blocks")]
        public GameObject SeparatorPrefab;  // Image (small height, stretch width)

        /// <summary>
        /// Prefab for a header block.
        /// </summary>
        public GameObject HeaderPrefab;     // Horizontal Layout: [Image] + [Vertical Layout (Title, Subtitle)]

        /// <summary>
        /// Prefab for a key-value pair row.
        /// </summary>
        public GameObject KeyValuePrefab;   // Horizontal Layout: [Text (Left)] + [Text (Right)]

        /// <summary>
        /// List of defined text styles (e.g., Title, Body, Warning).
        /// </summary>
        [Header("Typography")]
        public List<TextStyleData> TextStyles;

        /// <summary>
        /// Retrieves a text style by its ID.
        /// </summary>
        /// <param name="id">The ID of the style to find.</param>
        /// <returns>The matching TextStyleData or null if not found.</returns>
        public TextStyleData GetTextStyle(string id) => TextStyles.Find(x => x.ID == id);
    }
}
