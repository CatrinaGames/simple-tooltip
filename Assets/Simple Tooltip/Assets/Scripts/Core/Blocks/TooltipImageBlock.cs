using System;
using UnityEngine;

namespace SimpleTooltip.Scripts.Core.Blocks
{
    [Serializable]
    public class TooltipImageBlock : TooltipBlock
    {
        [Tooltip("Sprite to display in the tooltip")]
        public Sprite Sprite;
    }
}
