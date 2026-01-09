using System;
using UnityEngine;

namespace SimpleTooltip.Scripts.Core.Blocks
{
    [Serializable]
    public class TooltipSeparatorBlock : TooltipBlock
    {
        [Tooltip("Sprite to display as a separator in the tooltip")]
        public Sprite SeparatorSprite;
    }
}
