using System;
using System.Collections.Generic;
using SimpleTooltip.Scripts.Core;
using SimpleTooltip.Scripts.Models;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleTooltip.Scripts
{
    [DisallowMultipleComponent]
    public class SimpleTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Styling")]
        public SimpleTooltipStyle SimpleTooltipStyle;

        [Header("Tooltip Content")]
        [SerializeReference] public List<TooltipBlock> TooltipBlocks = new();

        // Referencia al controlador visual (Singleton o buscado)
        private STController _tooltipController;

        private bool _isUIObject;         // ¿Soy un botón/imagen de UI?
        private bool _hoveringCollider;   // ¿El mouse está físicamente sobre mi collider?
        private bool _tooltipVisible;     // ¿El tooltip está activo actualmente?

        private void Awake()
        {
            _isUIObject = GetComponent<RectTransform>() != null;

            // Inicialización robusta para encontrar el controlador
            _tooltipController = FindFirstObjectByType<STController>(FindObjectsInactive.Include);

            // Auto-reparación: Cargar prefab si no existe
            if (!_tooltipController)
            {
                GameObject prefab = Resources.Load<GameObject>("SimpleTooltip");
                if (prefab)
                {
                    GameObject instance = Instantiate(prefab);
                    instance.name = "SimpleTooltip";
                    _tooltipController = instance.GetComponentInChildren<STController>();
                    DontDestroyOnLoad(instance);
                }
            }

            // Cargar estilo por defecto si está vacío
            if (!SimpleTooltipStyle)
                SimpleTooltipStyle = Resources.Load<SimpleTooltipStyle>("STDefault");
        }

        private void Update()
        {
            if (_isUIObject) return;

            // SI SOY UI:
            // El EventSystem se encarga de todo. No necesitamos lógica en Update.
            if (_isUIObject) return;

            // SI SOY UN OBJETO 3D/2D (COLLIDER):
            // Necesitamos arbitrar cada frame porque la situación "UI bloqueando" cambia dinámicamente

            if (_hoveringCollider)
            {
                bool cursorBlockedByUI = IsPointerOverUI();

                if (cursorBlockedByUI && _tooltipVisible)
                {
                    // Estoy sobre el objeto, pero entré a un botón UI. Ocultar.
                    Hide();
                }
                else if (!cursorBlockedByUI && !_tooltipVisible)
                {
                    // Estoy sobre el objeto y ya no hay UI estorbando. Mostrar.
                    Show();
                }
            }
        }

        // =================================================================================
        // SISTEMA DE EVENTOS
        // =================================================================================

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isUIObject) Show();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isUIObject) Hide();
        }

        // Soporte para objetos físicos (3D/2D Colliders)
        private void OnMouseEnter()
        {
            if (_isUIObject) return; // Ignorar si es UI (usa OnPointerEnter)

            _hoveringCollider = true;

            // Intentamos mostrar inmediatamente para respuesta rápida,
            // pero el Update corregirá si hay UI encima.
            if (!IsPointerOverUI()) Show();
        }

        private void OnMouseExit()
        {
            if (_isUIObject) return;

            _hoveringCollider = false;
            Hide(); // Si salimos del collider, siempre ocultamos, haya UI o no.
        }

        // =================================================================================
        // API PÚBLICA (Aquí ocurre la magia)
        // =================================================================================

        /// <summary>
        /// Muestra el tooltip. Si se pasa 'data', usa eso.
        /// Si 'data' es null, construye los datos basándose en el Inspector.
        /// </summary>
        public void Show(List<TooltipBlock> blocks = null)
        {
            // GATEKEEPER: Si ya está visible, no regeneramos nada. Ahorro masivo de CPU.
            if (_tooltipVisible) return;
            if (_tooltipController == null) return;

            _tooltipVisible = true;

            List<TooltipBlock> finalBlocks = blocks is { Count: > 0 } ? blocks : TooltipBlocks;
            _tooltipController.ShowTooltip(finalBlocks, SimpleTooltipStyle, this);
        }

        public void Hide()
        {
            // GATEKEEPER: Si ya está oculto, no hacemos nada.
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
            // Configuración automática al añadir el script en el editor
            if (!SimpleTooltipStyle)
                SimpleTooltipStyle = Resources.Load<SimpleTooltipStyle>("STDefault");

            if (!GetComponent<RectTransform>() && !GetComponent<Collider>() && !GetComponent<Collider2D>())
                gameObject.AddComponent<BoxCollider>();
        }
    }
}
