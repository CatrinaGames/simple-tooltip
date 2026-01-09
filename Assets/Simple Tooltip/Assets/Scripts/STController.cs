using System.Collections;
using System.Collections.Generic;
using SimpleTooltip.Scripts.Core;
using SimpleTooltip.Scripts.Core.Blocks;
using SimpleTooltip.Scripts.Enums;
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
        [Header("Configuración Principal")]
        [Tooltip("El Prefab del STPanel")]
        public GameObject panelPrefab;

        [Header("Offset Settings (Relative to Mouse)")]
        public Vector2 offsetTopRight = new Vector2(25f, 0f);
        public Vector2 offsetTopLeft = new Vector2(-5f, 5f);
        public Vector2 offsetBottomRight = new Vector2(30f, -25f);
        public Vector2 offsetBottomLeft = new Vector2(-15f, -15f);

        [Header("Spacing")]
        public float spacing = 10f;

        // Componentes Layout (Debes agregarlos al objeto STController en el editor o se agregarán solos)
        private CanvasGroup _canvasGroup;
        private RectTransform _rect;

        // Estado
        private List<STPanel> _panelsPool = new();
        private object _currentOwner;
        private TooltipOrientation _currentOrientation;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();

            // Configuración inicial crítica para que el layout manual funcione bien
            _rect.pivot = new Vector2(0, 1);
            _rect.anchorMin = new Vector2(0.5f, 0.5f); // Anclaje libre para moverlo
            _rect.anchorMax = new Vector2(0.5f, 0.5f);

            HideTooltip(null);
        }

        private void Update()
        {
            if (gameObject.activeSelf)
            {
                FollowMouseAndLayout();
            }
        }

        public void ShowTooltip(List<TooltipData> dataList, SimpleTooltipStyle style, TooltipOrientation orientation, object owner)
        {
            if (dataList == null || dataList.Count == 0) return;

            _currentOwner = owner;
            _currentOrientation = orientation;
            _canvasGroup.alpha = 0f; // Ocultar mientras construimos
            gameObject.SetActive(true);

            // 1. Preparar paneles (Pooling)
            PreparePanels(dataList.Count);

            // 2. Llenar paneles con datos
            for (int i = 0; i < dataList.Count; i++)
            {
                _panelsPool[i].Setup(dataList[i], style);
                _panelsPool[i].gameObject.SetActive(true);
            }

            // 3. Iniciar proceso de reconstrucción y mostrar
            StartCoroutine(RebuildRoutine());
        }

        public void HideTooltip(object requester)
        {
            if (_currentOwner != null && _currentOwner != requester) return;

            _currentOwner = null;
            gameObject.SetActive(false);
            // Desactivar todos los paneles para la próxima
            foreach (var p in _panelsPool) p.gameObject.SetActive(false);
        }

        // --- LOGICA DE POSICIONAMIENTO Y LAYOUT MANUAL ---

        private void FollowMouseAndLayout()
        {
            Vector2 mousePos = GetMousePosition();

            // 1. Calcular tamaño TOTAL del grupo de tooltips
            Vector2 totalSize = CalculateTotalSize();
            _rect.sizeDelta = totalSize;

            // 2. Mover el Rect al mouse (posición base)
            _rect.position = mousePos;

            // 3. Detectar bordes de pantalla
            // Usamos lossyScale para soportar Canvas Scaler
            float scaledWidth = totalSize.x * transform.lossyScale.x;
            float scaledHeight = totalSize.y * transform.lossyScale.y;

            bool flipX = (mousePos.x + scaledWidth + offsetTopRight.x > Screen.width);
            bool flipY = (mousePos.y - scaledHeight + offsetTopRight.y < 0);

            // 4. Configurar Offset y Pivote del CONTENEDOR PADRE
            ApplyContainerTransform(mousePos, flipX, flipY);

            // 5. ORDENAR LOS PANELES INTERNAMENTE (La Magia)
            // Aquí es donde reemplazamos a los LayoutGroups
            ArrangePanels(flipX, totalSize);
        }

        private void ArrangePanels(bool flipX, Vector2 containerSize)
        {
            // Posición cursor local para ir colocando paneles
            // Empezamos siempre desde la esquina superior izquierda del contenedor (0,0 local)
            // El Pivot del padre se encarga de mover el contenedor, no nosotros.
            float currentX = 0f;
            float currentY = 0f;

            // Recorremos los paneles activos
            // Importante: _panelsPool[0] es el PRIMARIO

            // CASO HORIZONTAL CON FLIP (La lógica especial)
            if (_currentOrientation == TooltipOrientation.Horizontal && flipX)
            {
                // Si hay Flip Horizontal (estamos pegados al borde derecho):
                // El padre tiene Pivot X=1 (Right). El Mouse está a la derecha del contenedor.
                // Queremos que el PANEL PRIMARIO [0] esté pegado al Mouse (a la derecha).
                // Queremos que el PANEL SECUNDARIO [1] esté a la izquierda del primario.

                // Estrategia: Llenar de Derecha a Izquierda.
                currentX = containerSize.x; // Empezamos al final

                for (int i = 0; i < _panelsPool.Count; i++)
                {
                    var panel = _panelsPool[i];
                    if (!panel.gameObject.activeSelf) continue;

                    ConfigurePanelAnchor(panel);

                    float w = panel.Rect.sizeDelta.x;

                    // Nos movemos a la izquierda para encontrar el punto de inicio de este panel
                    currentX -= w;

                    panel.Rect.anchoredPosition = new Vector2(currentX, 0);

                    // Espacio para el siguiente (hacia la izquierda)
                    currentX -= spacing;
                }
            }
            else
            {
                // CASO NORMAL (Izquierda a Derecha) o VERTICAL
                for (int i = 0; i < _panelsPool.Count; i++)
                {
                    var panel = _panelsPool[i];
                    if (!panel.gameObject.activeSelf) continue;

                    ConfigurePanelAnchor(panel);

                    float w = panel.Rect.sizeDelta.x;
                    float h = panel.Rect.sizeDelta.y;

                    panel.Rect.anchoredPosition = new Vector2(currentX, currentY);

                    if (_currentOrientation == TooltipOrientation.Horizontal)
                    {
                        currentX += w + spacing;
                    }
                    else // Vertical
                    {
                        currentY -= (h + spacing); // Hacia abajo
                    }
                }
            }
        }

        private void ConfigurePanelAnchor(STPanel panel)
        {
            // Aseguramos que los paneles hijos tengan anclaje Top-Left standard
            // para que nuestras matemáticas de posición (0,0) funcionen.
            panel.Rect.anchorMin = new Vector2(0, 1);
            panel.Rect.anchorMax = new Vector2(0, 1);
            panel.Rect.pivot = new Vector2(0, 1);
        }

        private Vector2 CalculateTotalSize()
        {
            float totalW = 0;
            float totalH = 0;
            float maxW = 0;
            float maxH = 0;
            int activeCount = 0;

            foreach (var p in _panelsPool)
            {
                if (!p.gameObject.activeSelf) continue;
                activeCount++;
                float w = p.Rect.sizeDelta.x;
                float h = p.Rect.sizeDelta.y;

                if (_currentOrientation == TooltipOrientation.Horizontal)
                {
                    totalW += w;
                    if (h > maxH) maxH = h;
                }
                else // Vertical
                {
                    totalH += h;
                    if (w > maxW) maxW = w;
                }
            }

            // Sumar espaciado
            if (activeCount > 1)
            {
                float totalSpacing = (activeCount - 1) * spacing;
                if (_currentOrientation == TooltipOrientation.Horizontal) totalW += totalSpacing;
                else totalH += totalSpacing;
            }

            // Si es horizontal, la altura es la del panel más alto. Si es vertical, el ancho es el del más ancho.
            return _currentOrientation == TooltipOrientation.Horizontal
                ? new Vector2(totalW, maxH)
                : new Vector2(maxW, totalH);
        }

        private void ApplyContainerTransform(Vector2 mousePos, bool flipX, bool flipY)
        {
            Vector2 finalOffset;
            Vector2 finalPivot;

            // Lógica de cuadrantes para offset y pivote
            // Nota: El pivote del Container ayuda a que "crezca" en la dirección correcta
            // pero el ArrangePanels se encarga de la posición interna.

            if (!flipX && !flipY) // Normal (Derecha Abajo)
            {
                finalPivot = new Vector2(0, 1);
                finalOffset = offsetBottomRight;
            }
            else if (flipX && !flipY) // Izquierda Abajo
            {
                finalPivot = new Vector2(1, 1);
                finalOffset = offsetBottomLeft;
            }
            else if (!flipX && flipY) // Derecha Arriba
            {
                finalPivot = new Vector2(0, 0);
                finalOffset = offsetTopRight;
            }
            else // Izquierda Arriba
            {
                finalPivot = new Vector2(1, 0);
                finalOffset = offsetTopLeft;
            }

            _rect.pivot = finalPivot;
            _rect.position = new Vector3(mousePos.x + finalOffset.x, mousePos.y + finalOffset.y, 0f);
        }

        // --- HELPERS INTERNOS ---

        private IEnumerator RebuildRoutine()
        {
            // 1. Reconstruir tamaño interno de cada panel (ContentSizeFitter)
            foreach (var p in _panelsPool)
                if (p.gameObject.activeSelf) yield return p.RebuildLayoutRoutine();

            yield return new WaitForEndOfFrame();

            // 2. Calcular posiciones una vez que tenemos los tamaños
            FollowMouseAndLayout();

            // 3. Mostrar
            _canvasGroup.alpha = 1f;
        }

        private void PreparePanels(int count)
        {
            while (_panelsPool.Count < count)
            {
                GameObject go = Instantiate(panelPrefab, transform);
                // Importante: Asegurar que los paneles no tengan anclajes extraños que rompan nuestro cálculo manual
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);

                _panelsPool.Add(go.GetComponent<STPanel>());
            }
        }

        private void SetupLayoutGroup(HorizontalOrVerticalLayoutGroup group)
        {
            group.childAlignment = TextAnchor.UpperLeft;
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = false;
        }

        private Vector2 GetMousePosition()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#elif ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Pointer.current != null)
                    return UnityEngine.InputSystem.Pointer.current.position.ReadValue();
                return Vector2.zero;
#else
                return Input.mousePosition;
#endif
        }
    }
}
