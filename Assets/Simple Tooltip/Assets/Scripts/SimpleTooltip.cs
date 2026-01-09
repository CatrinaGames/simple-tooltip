using System.Collections.Generic;
using SimpleTooltip.Scripts.Definitions;
using SimpleTooltip.Scripts.Enums;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleTooltip.Scripts
{
    [DisallowMultipleComponent]
    public class SimpleTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// The visual style to apply to the tooltip.
        /// </summary>
        [Header("Styling")]
        public SimpleTooltipStyle SimpleTooltipStyle;

        /// <summary>
        /// Defines how tooltips are organized if there is more than one.
        /// </summary>
        [Tooltip("Defines how tooltips are organized if there is more than one.")]
        public TooltipOrientation Orientation = TooltipOrientation.Horizontal;

        /// <summary>
        /// List of tooltip data to display.
        /// </summary>
        [Header("Tooltip Content")]
        [SerializeReference] public List<TooltipData> TooltipDatas = new();

        // Reference to the visual controller (Singleton or searched)
        private STController _tooltipController;

        // Am I a UI button/image?
        private bool _isUIObject;

        // Is the mouse physically over my collider?
        private bool _hoveringCollider;

        // Is the tooltip currently active?
        private bool _tooltipVisible;

        private void Awake()
        {
            _isUIObject = GetComponent<RectTransform>() != null;

            // Robust initialization to find the controller
            _tooltipController = FindFirstObjectByType<STController>(FindObjectsInactive.Include);

            // Auto-repair: Load prefab if it doesn't exist
            if (!_tooltipController)
            {
                GameObject prefab = Resources.Load<GameObject>("SimpleTooltipManager");
                if (prefab)
                {
                    GameObject instance = Instantiate(prefab);
                    instance.name = "SimpleTooltipManager";
                    _tooltipController = instance.GetComponentInChildren<STController>();
                    DontDestroyOnLoad(instance);
                }
            }

            // Load default style if empty
            if (!SimpleTooltipStyle)
                SimpleTooltipStyle = Resources.Load<SimpleTooltipStyle>("STDefault");
        }

        private void Update()
        {
            if (_isUIObject) return;

            // IF I AM UI:
            // The EventSystem handles everything. We don't need logic in Update.
            if (_isUIObject) return;

            // IF I AM A 3D/2D OBJECT (COLLIDER):
            // We need to arbitrate every frame because the "UI blocking" situation changes dynamically

            if (_hoveringCollider)
            {
                bool cursorBlockedByUI = IsPointerOverUI();

                if (cursorBlockedByUI && _tooltipVisible)
                {
                    // I am over the object, but entered a UI button. Hide.
                    Hide();
                }
                else if (!cursorBlockedByUI && !_tooltipVisible)
                {
                    // I am over the object and there is no UI blocking anymore. Show.
                    Show();
                }
            }
        }

        // =================================================================================
        // EVENT SYSTEM
        // =================================================================================

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isUIObject) Show();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isUIObject) Hide();
        }

        // Support for physical objects (3D/2D Colliders)
        private void OnMouseEnter()
        {
            if (_isUIObject) return; // Ignore if it is UI (uses OnPointerEnter)

            _hoveringCollider = true;

            // We try to show immediately for quick response,
            // but Update will correct if there is UI on top.
            if (!IsPointerOverUI()) Show();
        }

        private void OnMouseExit()
        {
            if (_isUIObject) return;

            _hoveringCollider = false;
            Hide(); // If we exit the collider, always hide, whether there is UI or not.
        }

        // =================================================================================
        // PUBLIC API
        // =================================================================================

        /// <summary>
        /// Shows n tooltips. The first one (index 0) is the main one.
        /// </summary>
        /// <param name="dataList">Optional list of data to override the default content.</param>
        public void Show(List<TooltipData> dataList = null)
        {
            // GATEKEEPER: If already visible, do not regenerate anything. Massive CPU saving.
            if (_tooltipVisible) return;
            if (_tooltipController == null) return;

            _tooltipVisible = true;

            List<TooltipData> finalData = dataList is { Count: > 0 } ? dataList : TooltipDatas;
            _tooltipController.ShowTooltip(finalData, SimpleTooltipStyle, Orientation, this);
        }

        /// <summary>
        /// Hides the tooltip.
        /// </summary>
        public void Hide()
        {
            // GATEKEEPER: If already hidden, do nothing.
            if (!_tooltipVisible) return;

            _tooltipVisible = false;

            if (_tooltipController != null)
                _tooltipController.HideTooltip(this);
        }

        // =================================================================================
        // HELPERS
        // =================================================================================

        private bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private void Reset()
        {
            // Automatic configuration when adding the script in the editor
            if (!SimpleTooltipStyle)
                SimpleTooltipStyle = Resources.Load<SimpleTooltipStyle>("STDefault");

            if (!GetComponent<RectTransform>() && !GetComponent<Collider>() && !GetComponent<Collider2D>())
                gameObject.AddComponent<BoxCollider>();
        }
    }
}
