using System;
using System.Collections.Generic;
using SimpleTooltip.Scripts.Core;
using SimpleTooltip.Scripts.Core.Blocks;
using UnityEngine;

namespace SimpleTooltip.Scripts.Definitions
{
    /// <summary>
    /// Represents the data structure for a tooltip, containing a list of content blocks.
    /// </summary>
    [Serializable]
    public class TooltipData
    {
        /// <summary>
        /// List of blocks that make up the tooltip content.
        /// Uses [SerializeReference] to support polymorphism in the Inspector.
        /// </summary>
        [SerializeReference] public List<TooltipBlock> TooltipBlocks = new();

        public TooltipData() { }

        // Helper methods

        /// <summary>
        /// Adds a text block to the tooltip.
        /// </summary>
        /// <param name="text">The text content to display.</param>
        /// <param name="styleId">The style key to apply to the text (default is "Normal").</param>
        public void AddText(string text, string styleId = "Normal")
        {
            TooltipBlocks.Add(new TooltipTextBlock { Text = text, KeyStyleName = styleId });
        }

        /// <summary>
        /// Adds an image block to the tooltip.
        /// </summary>
        /// <param name="sprite">The sprite to display.</param>
        public void AddImage(Sprite sprite)
        {
            TooltipBlocks.Add(new TooltipImageBlock { Sprite = sprite });
        }
    }
}
