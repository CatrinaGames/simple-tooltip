using System.Collections;
using System.Collections.Generic;
using SimpleTooltip.Scripts.Core.Blocks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleTooltip.Scripts
{
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(VerticalLayoutGroup))]
    [RequireComponent(typeof(ContentSizeFitter))]
    public class STPanel : MonoBehaviour
    {
        [Header("Referencias UI")] public Image panelBackground;
        public Transform contentParent;

        [Header("Configuración")] public float maxTooltipWidth = 400f;

        private List<GameObject> _activeBlocks = new List<GameObject>();
        private RectTransform _rect;

        public RectTransform Rect => _rect ? _rect : GetComponent<RectTransform>();

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            if (!panelBackground) panelBackground = GetComponent<Image>();
        }

        public void Setup(TooltipData data, SimpleTooltipStyle style)
        {
            // 1. Limpiar bloques anteriores
            foreach (var obj in _activeBlocks) Destroy(obj);
            _activeBlocks.Clear();

            // 2. Aplicar Estilo al Panel
            if (panelBackground && style)
            {
                panelBackground.sprite = style.BackgroundSprite;
                panelBackground.color = style.BackgroundColor;
                panelBackground.type = Image.Type.Sliced;
            }

            // 3. Instanciar Bloques
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

        public IEnumerator RebuildLayoutRoutine()
        {
            var contentLE = contentParent.GetComponent<LayoutElement>();
            var contentRect = contentParent.GetComponent<RectTransform>();

            // 1. Desbloquear ancho para medir natural
            if (contentLE) contentLE.preferredWidth = -1;

            yield return new WaitForEndOfFrame();

            // 2. Reconstruir para medir
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            // 3. Aplicar límite si es necesario
            if (contentLE && contentRect.rect.width > maxTooltipWidth)
            {
                contentLE.preferredWidth = maxTooltipWidth;
            }

            // 4. Reconstrucción final
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
                // Asumimos una estructura en el Prefab:
                // - IconImage (Image)
                // - TextsContainer (VerticalLayout)
                //    - TitleText (TMP)
                //    - SubtitleText (TMP)

                // Buscamos componentes por nombre o jerarquía (Simple y efectivo)
                // Nota: Para máxima performance, crea un script "HeaderViewReference" en el prefab, pero esto funciona bien.

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
                // Asumimos Prefab: HorizontalLayout -> [KeyText (Alignment Left)] [ValueText (Alignment Right)]
                TextMeshProUGUI keyTxt = go.transform.Find("KeyText")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI valTxt = go.transform.Find("ValueText")?.GetComponent<TextMeshProUGUI>();
                Image icon = go.transform.Find("Icon")?.GetComponent<Image>(); // Opcional

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

        private void InstantiateBlock(GameObject prefab, System.Action<GameObject> configure)
        {
            if (!prefab) return;
            var go = Instantiate(prefab, contentParent);
            configure?.Invoke(go);
            _activeBlocks.Add(go);
        }

        // Helper para evitar repetir código de estilos
        private void ApplyTextStyle(TextMeshProUGUI tmp, string styleId, SimpleTooltipStyle style, bool force = false)
        {
            if (tmp == null) return;
            var styleDef = style.GetTextStyle(styleId);
            if (styleDef != null)
            {
                tmp.font = styleDef.fontAsset;
                tmp.fontSize = styleDef.fontSize;
                tmp.color = styleDef.color;
                if (force)
                    tmp.alignment =
                        styleDef
                            .alignment; // Cuidado: En KeyValue blocks, la alineación la dicta el prefab, no el estilo
                tmp.fontStyle = styleDef.fontStyle;
            }
        }
    }
}
