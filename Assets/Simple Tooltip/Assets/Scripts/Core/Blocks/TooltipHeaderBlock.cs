using System;
using UnityEngine;

namespace SimpleTooltip.Scripts.Core.Blocks
{
    [Serializable]
    public class TooltipHeaderBlock : TooltipBlock
    {
        [Tooltip("The icon to display in the header")]
        public Sprite Icon;
        [Tooltip("The main title text of the tooltip header")]
        public string Title;
        [Tooltip("The subtitle text of the tooltip header")]
        public string Subtitle;
        [Tooltip("The key style for the title text, e.g. 'Title'")]
        public string KeyTitleStyle = "Title";
        [Tooltip("The key style for the subtitle text, e.g. 'Rarity'")]
        public string KeySubtitleStyle = "Rarity";
    }
}
