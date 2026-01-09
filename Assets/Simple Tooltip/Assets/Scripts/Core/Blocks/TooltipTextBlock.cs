using System;
using UnityEngine;

namespace SimpleTooltip.Scripts.Core.Blocks
{
    [Serializable]
    public class TooltipTextBlock : TooltipBlock
    {
        [TextArea]
        [Tooltip("Text to display in the tooltip")]
        public string Text;
        [Tooltip("Key style name to apply to the text")]
        public string KeyStyleName;
    }
}
