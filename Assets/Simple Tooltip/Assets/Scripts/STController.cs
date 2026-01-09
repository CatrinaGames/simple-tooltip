using System.Collections;
using System.Collections.Generic;
using SimpleTooltip.Scripts.Core;
using SimpleTooltip.Scripts.Core.Blocks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleTooltip.Scripts
{
    /// <summary>
    /// Controller for the Simple Tooltip system.
    /// Handles text resizing, styling, and positioning relative to the mouse cursor with screen boundary checks.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(RectTransform))]
    public class STController : MonoBehaviour
    {
        [Header("UI References")] public Image panelBackground;

        [Tooltip("El contenedor hijo que tiene el VerticalLayoutGroup")]
        public Transform contentParent;

        [Header("Size Settings")] [Tooltip("El ancho máximo antes de que el texto empiece a saltar de línea.")]
        public float maxTooltipWidth = 400f;

        [Header("Positioning Settings")] public Vector2 offsetTopRight = new Vector2(10f, 10f);
        public Vector2 offsetTopLeft = new Vector2(-10f, 10f);
        public Vector2 offsetBottomRight = new Vector2(50f, -20f);
        public Vector2 offsetBottomLeft = new Vector2(-10f, -20f);

        private CanvasGroup canvasGroup;
        private RectTransform rect;
        private List<GameObject> activeBlocks = new List<GameObject>();

        private object currentOwner;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (panelBackground == null) panelBackground = GetComponent<Image>();

            HideTooltip(null);
        }

        private void Update()
        {
            if (gameObject.activeSelf)
            {
                FollowMouseAndClamp();
            }
        }

        public void ShowTooltip(List<TooltipBlock> blocks, SimpleTooltipStyle style, object owner)
        {
            if (blocks == null || style == null) return;

            currentOwner = owner;

            // 1. Ocultar mientras se construye
            canvasGroup.alpha = 0f;
            gameObject.SetActive(true);

            // 2. Limpieza
            foreach (var obj in activeBlocks)
            {
                if (obj != null) Destroy(obj);
            }

            activeBlocks.Clear();

            // 3. Estilo Fondo
            if (panelBackground != null)
            {
                panelBackground.sprite = style.BackgroundSprite;
                panelBackground.color = style.BackgroundColor;
                panelBackground.type = Image.Type.Sliced;
            }

            // 4. Generar Bloques
            foreach (var block in blocks)
            {
                // BLOQUES BÁSICOS
                if (block is TooltipTextBlock textBlock)
                    CreateTextBlock(textBlock, style);
                else if (block is TooltipImageBlock imageBlock)
                    CreateImageBlock(imageBlock, style);

                // BLOQUES AVANZADOS (NUEVOS)
                else if (block is TooltipSeparatorBlock sepBlock)
                    CreateSeparatorBlock(sepBlock, style);
                else if (block is TooltipHeaderBlock headerBlock)
                    CreateHeaderBlock(headerBlock, style);
                else if (block is TooltipKeyValueBlock kvBlock)
                    CreateKeyValueBlock(kvBlock, style);
            }

            // 5. Reconstrucción inteligente del Layout
            StartCoroutine(RebuildLayoutRoutine());
        }

        public void HideTooltip(object requester)
        {
            if (currentOwner != null && currentOwner != requester)
            {
                return;
            }

            currentOwner = null;
            gameObject.SetActive(false);
            foreach (var obj in activeBlocks) Destroy(obj);
            activeBlocks.Clear();
        }

        // =================================================================================
        // RUTINA DE LAYOUT INTELIGENTE
        // =================================================================================

        private IEnumerator RebuildLayoutRoutine()
        {
            // Referencia al LayoutElement del contenedor de contenido (ContentParent)
            LayoutElement contentLE = contentParent.GetComponent<LayoutElement>();
            RectTransform contentRect = contentParent.GetComponent<RectTransform>();

            // PASO 1: Resetear límites para medir el tamaño natural
            // Desactivamos preferredWidth para que el ContentSizeFitter lo expanda todo lo necesario
            if (contentLE != null) contentLE.preferredWidth = -1;

            // Esperar fin de frame para que Unity instancie y calcule tamaños iniciales
            yield return new WaitForEndOfFrame();

            // Forzar reconstrucción para obtener el ancho "natural" (sin saltos de línea)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            // PASO 2: Comprobar si nos pasamos del máximo
            if (contentLE != null)
            {
                // Si el ancho natural supera el máximo permitido...
                if (contentRect.rect.width > maxTooltipWidth)
                {
                    // ...entonces SÍ aplicamos el límite para forzar el Word Wrap (salto de línea)
                    contentLE.preferredWidth = maxTooltipWidth;
                }
                else
                {
                    // Si es pequeño, nos aseguramos que siga en automático para que se ajuste al texto
                    contentLE.preferredWidth = -1;
                }
            }

            // PASO 3: Reconstrucción Final
            // Ahora que hemos decidido si limitar o no, reconstruimos todo el árbol
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect); // Ajustar hijo
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect); // Ajustar padre (fondo)

            // Posicionar y Mostrar
            FollowMouseAndClamp();
            canvasGroup.alpha = 1f;
        }

        // =================================================================================
        // FACTORIES
        // =================================================================================

        private void CreateTextBlock(TooltipTextBlock blockData, SimpleTooltipStyle style)
        {
            if (style.TextPrefab == null) return;
            GameObject go = Instantiate(style.TextPrefab, contentParent);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();

            if (tmp != null)
            {
                tmp.text = blockData.Text;
                ApplyTextStyle(tmp, blockData.KeyStyleName, style, true);
            }

            activeBlocks.Add(go);
        }

        private void CreateImageBlock(TooltipImageBlock blockData, SimpleTooltipStyle style)
        {
            if (style.ImagePrefab == null) return;
            GameObject go = Instantiate(style.ImagePrefab, contentParent);
            Image img = go.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = blockData.Sprite;
                img.preserveAspect = true;
            }

            activeBlocks.Add(go);
        }

        private void CreateSeparatorBlock(TooltipSeparatorBlock block, SimpleTooltipStyle style)
        {
            if (style.SeparatorPrefab == null) return;
            GameObject go = Instantiate(style.SeparatorPrefab, contentParent);
            Image img = go.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = block.SeparatorSprite;
            }
            activeBlocks.Add(go);
        }

        private void CreateHeaderBlock(TooltipHeaderBlock block, SimpleTooltipStyle style)
        {
            if (style.HeaderPrefab == null) return;
            GameObject go = Instantiate(style.HeaderPrefab, contentParent);

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

            activeBlocks.Add(go);
        }

        private void CreateKeyValueBlock(TooltipKeyValueBlock block, SimpleTooltipStyle style)
        {
            if (style.KeyValuePrefab == null) return;
            GameObject go = Instantiate(style.KeyValuePrefab, contentParent);

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

            activeBlocks.Add(go);
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
                if (force) tmp.alignment = styleDef.alignment; // Cuidado: En KeyValue blocks, la alineación la dicta el prefab, no el estilo
                tmp.fontStyle = styleDef.fontStyle;
            }
        }

        private void FollowMouseAndClamp()
        {
            Vector2 mousePos = Vector2.zero;

#if ENABLE_LEGACY_INPUT_MANAGER
            mousePos = Input.mousePosition;
#elif ENABLE_INPUT_SYSTEM
    // Usamos Pointer.current para dar soporte tanto a Mouse como a Pen/Touch simulado
    if (UnityEngine.InputSystem.Pointer.current != null)
    {
        mousePos = UnityEngine.InputSystem.Pointer.current.position.ReadValue();
    }
#endif
            float realWidth = rect.sizeDelta.x * transform.lossyScale.x;
            float realHeight = rect.sizeDelta.y * transform.lossyScale.y;

            bool flipX = (mousePos.x + realWidth > Screen.width);
            bool flipY = (mousePos.y + realHeight > Screen.height);

            Vector2 finalPivot = new Vector2(flipX ? 1 : 0, flipY ? 1 : 0);
            Vector2 finalOffset;

            if (!flipX && !flipY) finalOffset = offsetTopRight;
            else if (flipX && !flipY) finalOffset = offsetTopLeft;
            else if (!flipX && flipY) finalOffset = offsetBottomRight;
            else finalOffset = offsetBottomLeft;

            rect.pivot = finalPivot;
            rect.position = new Vector3(mousePos.x + finalOffset.x, mousePos.y + finalOffset.y, 0f);
        }
    }
}
