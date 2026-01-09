using System.Collections;
using System.Collections.Generic;
using SimpleTooltip.Scripts.Definitions;
using SimpleTooltip.Scripts.Enums;
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
        [Header("Main Configuration")]
        [Tooltip("The STPanel Prefab")]
        public GameObject panelPrefab;

        [Header("Offset Settings (Relative to Mouse)")]
        public Vector2 offsetTopRight = new Vector2(25f, 0f);
        public Vector2 offsetTopLeft = new Vector2(-5f, 5f);
        public Vector2 offsetBottomRight = new Vector2(30f, -25f);
        public Vector2 offsetBottomLeft = new Vector2(-15f, -15f);

        [Header("Spacing")]
        public float spacing = 10f;

        // Layout Components (Must be added to the STController object in the editor or they will be added automatically)
        private CanvasGroup _canvasGroup;
        private RectTransform _rect;

        // State
        private List<STPanel> _panelsPool = new();
        private object _currentOwner;
        private TooltipOrientation _currentOrientation;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();

            // Critical initial configuration for manual layout to work properly
            _rect.pivot = new Vector2(0, 1);
            _rect.anchorMin = new Vector2(0.5f, 0.5f); // Free anchor to move it
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

        /// <summary>
        /// Displays the tooltip with the specified data and style.
        /// </summary>
        /// <param name="dataList">List of data blocks to display.</param>
        /// <param name="style">The visual style to apply.</param>
        /// <param name="orientation">Layout orientation (Horizontal/Vertical).</param>
        /// <param name="owner">The object requesting the tooltip (for ownership checks).</param>
        public void ShowTooltip(List<TooltipData> dataList, SimpleTooltipStyle style, TooltipOrientation orientation, object owner)
        {
            if (dataList == null || dataList.Count == 0) return;

            _currentOwner = owner;
            _currentOrientation = orientation;
            _canvasGroup.alpha = 0f; // Hide while building
            gameObject.SetActive(true);

            // 1. Prepare panels (Pooling)
            PreparePanels(dataList.Count);

            // 2. Fill panels with data
            for (int i = 0; i < dataList.Count; i++)
            {
                _panelsPool[i].Setup(dataList[i], style);
                _panelsPool[i].gameObject.SetActive(true);
            }

            // 3. Start rebuild process and show
            StartCoroutine(RebuildRoutine());
        }

        /// <summary>
        /// Hides the tooltip if the requester is the current owner.
        /// </summary>
        /// <param name="requester">The object requesting to hide the tooltip.</param>
        public void HideTooltip(object requester)
        {
            if (_currentOwner != null && _currentOwner != requester) return;

            _currentOwner = null;
            gameObject.SetActive(false);
            // Disable all panels for next time
            foreach (var p in _panelsPool) p.gameObject.SetActive(false);
        }

        // --- POSITIONING LOGIC AND MANUAL LAYOUT ---

        private void FollowMouseAndLayout()
        {
            Vector2 mousePos = GetMousePosition();

            // 1. Calculate TOTAL size of the tooltip group
            Vector2 totalSize = CalculateTotalSize();
            _rect.sizeDelta = totalSize;

            // 2. Move Rect to mouse (base position)
            _rect.position = mousePos;

            // 3. Detect screen edges
            // Use lossyScale to support Canvas Scaler
            float scaledWidth = totalSize.x * transform.lossyScale.x;
            float scaledHeight = totalSize.y * transform.lossyScale.y;

            bool flipX = (mousePos.x + scaledWidth + offsetTopRight.x > Screen.width);
            bool flipY = (mousePos.y - scaledHeight + offsetTopRight.y < 0);

            // 4. Configure Offset and Pivot of the PARENT CONTAINER
            ApplyContainerTransform(mousePos, flipX, flipY);

            // 5. ARRANGE PANELS INTERNALLY (The Magic)
            // This is where we replace LayoutGroups
            ArrangePanels(flipX, totalSize);
        }

        private void ArrangePanels(bool flipX, Vector2 containerSize)
        {
            // Local cursor position for placing panels
            // Always start from the top-left corner of the container (local 0,0)
            // The parent's Pivot handles moving the container, not us.
            float currentX = 0f;
            float currentY = 0f;

            // Iterate through active panels
            // Important: _panelsPool[0] is the PRIMARY one

            // HORIZONTAL CASE WITH FLIP (Special logic)
            if (_currentOrientation == TooltipOrientation.Horizontal && flipX)
            {
                // If Horizontal Flip (we are stuck to the right edge):
                // Parent has Pivot X=1 (Right). Mouse is to the right of the container.
                // We want the PRIMARY PANEL [0] stuck to the Mouse (on the right).
                // We want the SECONDARY PANEL [1] to the left of the primary.

                // Strategy: Fill from Right to Left.
                currentX = containerSize.x; // Start at the end

                for (int i = 0; i < _panelsPool.Count; i++)
                {
                    var panel = _panelsPool[i];
                    if (!panel.gameObject.activeSelf) continue;

                    ConfigurePanelAnchor(panel);

                    float w = panel.Rect.sizeDelta.x;

                    // Move left to find the start point of this panel
                    currentX -= w;

                    panel.Rect.anchoredPosition = new Vector2(currentX, 0);

                    // Space for the next one (to the left)
                    currentX -= spacing;
                }
            }
            else
            {
                // NORMAL CASE (Left to Right) or VERTICAL
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
                    else // Vertical (Downwards)
                    {
                        currentY -= (h + spacing);
                    }
                }
            }
        }

        private void ConfigurePanelAnchor(STPanel panel)
        {
            // Ensure child panels have standard Top-Left anchor
            // so our position math (0,0) works.
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

            // Add spacing
            if (activeCount > 1)
            {
                float totalSpacing = (activeCount - 1) * spacing;
                if (_currentOrientation == TooltipOrientation.Horizontal) totalW += totalSpacing;
                else totalH += totalSpacing;
            }

            // If horizontal, height is that of the tallest panel. If vertical, width is that of the widest.
            return _currentOrientation == TooltipOrientation.Horizontal
                ? new Vector2(totalW, maxH)
                : new Vector2(maxW, totalH);
        }

        private void ApplyContainerTransform(Vector2 mousePos, bool flipX, bool flipY)
        {
            Vector2 finalOffset;
            Vector2 finalPivot;

            // Quadrant logic for offset and pivot
            // Note: The Container pivot helps it "grow" in the correct direction
            // but ArrangePanels handles internal positioning.

            if (!flipX && !flipY) // Normal (Right Bottom)
            {
                finalPivot = new Vector2(0, 1);
                finalOffset = offsetBottomRight;
            }
            else if (flipX && !flipY) // Left Bottom
            {
                finalPivot = new Vector2(1, 1);
                finalOffset = offsetBottomLeft;
            }
            else if (!flipX && flipY) // Right Top
            {
                finalPivot = new Vector2(0, 0);
                finalOffset = offsetTopRight;
            }
            else // Left Top
            {
                finalPivot = new Vector2(1, 0);
                finalOffset = offsetTopLeft;
            }

            _rect.pivot = finalPivot;
            _rect.position = new Vector3(mousePos.x + finalOffset.x, mousePos.y + finalOffset.y, 0f);
        }

        // --- INTERNAL HELPERS ---

        private IEnumerator RebuildRoutine()
        {
            // 1. Rebuild internal size of each panel (ContentSizeFitter)
            foreach (var p in _panelsPool)
                if (p.gameObject.activeSelf) yield return p.RebuildLayoutRoutine();

            yield return new WaitForEndOfFrame();

            // 2. Calculate positions once we have sizes
            FollowMouseAndLayout();

            // 3. Show
            _canvasGroup.alpha = 1f;
        }

        private void PreparePanels(int count)
        {
            while (_panelsPool.Count < count)
            {
                GameObject go = Instantiate(panelPrefab, transform);
                // Important: Ensure panels don't have strange anchors that break our manual calculation
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
