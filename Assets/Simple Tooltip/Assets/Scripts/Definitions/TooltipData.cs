using System;
using System.Collections.Generic;
using SimpleTooltip.Scripts.Core;
using SimpleTooltip.Scripts.Core.Blocks;
using SimpleTooltip.Scripts.Enums;
using UnityEngine;

namespace SimpleTooltip.Scripts
{
    [Serializable]
    public class TooltipData
    {
        [SerializeReference] public List<TooltipBlock> TooltipBlocks = new();

        public TooltipData() { }

        // Helpers rápidos
        public void AddText(string text, string styleId = "Normal")
        {
            // Asegúrate de tener referenciado el namespace correcto de tus bloques
            TooltipBlocks.Add(new TooltipTextBlock { Text = text, KeyStyleName = styleId });
        }

        public void AddImage(Sprite sprite)
        {
            TooltipBlocks.Add(new TooltipImageBlock { Sprite = sprite });
        }
    }
}
