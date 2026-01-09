using System;
using System.Collections;
using System.Collections.Generic;
using SimpleTooltip.Scripts.Core.Blocks;
using SimpleTooltip.Scripts.Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleTooltip.Scripts
{
    /// <summary>
    /// Controls the individual tooltip panel, handling the instantiation and configuration of content blocks.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(VerticalLayoutGroup))]
    [RequireComponent(typeof(ContentSizeFitter))]
    public class STPanel : MonoBehaviour
    {
        [Header("UI References")] public Image PanelBackground;
        public Transform ContentParent;

        [Header("Configuration")] public float MaxTooltipWidth = 400f;

        private List<GameObject> _activeBlocks = new List<GameObject>();
        private RectTransform _rect;

        public RectTransform Rect => _rect ? _rect : GetComponent<RectTransform>();

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            if (!PanelBackground) PanelBackground = GetComponent<Image>();
        }

        /// <summary>
        /// Sets up the panel with the provided data and style.
        /// </summary>
        /// <param name="data">The data containing content blocks.</param>
        /// <param name="style">The visual style definition.</param>
        public void Setup(TooltipData data, SimpleTooltipStyle style)
        {
            // 1. Clear previous blocks
            foreach (var obj in _activeBlocks) Destroy(obj);
            _activeBlocks.Clear();

            // 2. Apply Style to Panel
            if (PanelBackground && style)
            {
                PanelBackground.sprite = style.BackgroundSprite;
                PanelBackground.color = style.BackgroundColor;
                PanelBackground.type = Image.Type.Sliced;
            }

            // 3. Instantiate Blocks
            if (data != null && style != null)
            {
                foreach (var block in data.TooltipBlocks)
                {
                    if (block is TooltipTextBlock tb) CreateTextBlock(tb, style);
                    else if (block is TooltipImageBlock ib) CreateImageBlock(ib, style);
                    else if (block is TooltipHeaderBlock hb) CreateHeaderBlock(hb, style);
                    else if (block is TooltipSeparatorBlock sb) CreateSeparatorBlock(sb, style);
                    else if (block is TooltipKeyValueBlock kb) CreateKeyValueBlock(kb, style);
                }
            }
        }

        /// <summary>
        /// Coroutine to force layout rebuilds to ensure correct sizing.
        /// </summary>
        public IEnumerator RebuildLayoutRoutine()
        {
            var contentLE = ContentParent.GetComponent<LayoutElement>();
            var contentRect = ContentParent.GetComponent<RectTransform>();

            // 1. Unlock width to measure naturally
            if (contentLE) contentLE.preferredWidth = -1;

            yield return new WaitForEndOfFrame();

            // 2. Rebuild to measure
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            // 3. Apply limit if necessary
            if (contentLE && contentRect.rect.width > MaxTooltipWidth)
            {
                contentLE.preferredWidth = MaxTooltipWidth;
            }

            // 4. Final rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(Rect);
        }

        // =================================================================================
        // FACTORIES
        // =================================================================================

        private void CreateTextBlock(TooltipTextBlock blockData, SimpleTooltipStyle style) => InstantiateBlock(
            style.TextPrefab,
            go =>
            {
                TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = blockData.Text;
                    ApplyTextStyle(tmp, blockData.KeyStyleName, style);
                }
            });

        private void CreateImageBlock(TooltipImageBlock blockData, SimpleTooltipStyle style) => InstantiateBlock(
            style.ImagePrefab,
            go =>
            {
                Image img = go.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = blockData.Sprite;
                    img.preserveAspect = true;
                }
            });

        private void CreateSeparatorBlock(TooltipSeparatorBlock block, SimpleTooltipStyle style) => InstantiateBlock(
            style.SeparatorPrefab,
            go =>
            {
                Image img = go.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = block.SeparatorSprite;
                }
            });

        private void CreateHeaderBlock(TooltipHeaderBlock block, SimpleTooltipStyle style) => InstantiateBlock(
            style.HeaderPrefab,
            go =>
            {
                // We assume a structure in the Prefab:
                // - IconImage (Image)
                // - TextsContainer (VerticalLayout)
                //    - TitleText (TMP)
                //    - SubtitleText (TMP)

                // We search for components by name or hierarchy (Simple and effective)
                // Note: For maximum performance, create a "HeaderViewReference" script on the prefab, but this works well.

                Image icon = go.transform.Find("IconImage")?.GetComponent<Image>();
                TextMeshProUGUI title = go.transform.Find("TextsContainer/TitleText")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI subtitle =
                    go.transform.Find("TextsContainer/SubtitleText")?.GetComponent<TextMeshProUGUI>();

                if (icon) icon.sprite = block.Icon;

                if (title)
                {
                    title.text = block.Title;
                    ApplyTextStyle(title, block.KeyTitleStyle, style, true);
                }

                if (subtitle)
                {
                    subtitle.text = block.Subtitle;
                    ApplyTextStyle(subtitle, block.KeySubtitleStyle, style, true);
                }
            });

        private void CreateKeyValueBlock(TooltipKeyValueBlock block, SimpleTooltipStyle style) => InstantiateBlock(
            style.KeyValuePrefab,
            go =>
            {
                // We assume Prefab: HorizontalLayout -> [KeyText (Alignment Left)] [ValueText (Alignment Right)]
                TextMeshProUGUI keyTxt = go.transform.Find("KeyText")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI valTxt = go.transform.Find("ValueText")?.GetComponent<TextMeshProUGUI>();
                Image icon = go.transform.Find("Icon")?.GetComponent<Image>(); // Optional

                if (keyTxt)
                {
                    keyTxt.text = block.Key;
                    ApplyTextStyle(keyTxt, block.KeyKeyStyle, style);
                }

                if (valTxt)
                {
                    valTxt.text = block.Value;
                    ApplyTextStyle(valTxt, block.KeyValueStyle, style);
                }

                if (icon)
                {
                    icon.gameObject.SetActive(block.Icon != null);
                    icon.sprite = block.Icon;
                }
            });

        private void InstantiateBlock(GameObject prefab, Action<GameObject> configure)
        {
            if (!prefab) return;
            var go = Instantiate(prefab, ContentParent);
            configure?.Invoke(go);
            _activeBlocks.Add(go);
        }

        // Helper to avoid repeating style code
        private void ApplyTextStyle(TextMeshProUGUI tmp, string styleId, SimpleTooltipStyle style, bool force = false)
        {
            if (tmp == null) return;
            var styleDef = style.GetTextStyle(styleId);
            if (styleDef != null)
            {
                tmp.font = styleDef.FontAsset;
                tmp.fontSize = styleDef.FontSize;
                tmp.color = styleDef.Color;
                if (force)
                    tmp.alignment =
                        styleDef.Alignment; // Caution: In KeyValue blocks, alignment is dictated by the prefab, not the style
                tmp.fontStyle = styleDef.FontStyle;
            }
        }
    }
}
