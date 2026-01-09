using System.Collections.Generic;
using SimpleTooltip.Scripts.Core;
using UnityEngine;

namespace SimpleTooltip.Scripts
{
    [CreateAssetMenu(fileName = "NewTooltip", menuName = "Game/Systems/Tooltip/Tooltip Definition", order = 0)]
    public class TooltipDefinition : ScriptableObject
    {
        public List<TooltipBlock> blocks = new List<TooltipBlock>();
    }
}
