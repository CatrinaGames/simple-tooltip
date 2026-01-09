using System;
using UnityEngine;

namespace SimpleTooltip.Scripts.Core.Blocks
{
    [Serializable]
    public class TooltipKeyValueBlock : TooltipBlock
    {
        [Tooltip("Small icon to the side of the stat (e.g. sword icon)")]
        public Sprite Icon;
        [Tooltip("Stat name, e.g. 'Damage'")]
        public string Key;
        [Tooltip("Stat value, e.g. '50 - 64'")]
        public string Value;
        [Tooltip("Key style for the stat name, e.g. 'Body'")]
        public string KeyKeyStyle = "Body";
        [Tooltip("Key style for the stat value, e.g. 'ValueHighlight'")]
        public string KeyValueStyle = "ValueHighlight";
    }
}
