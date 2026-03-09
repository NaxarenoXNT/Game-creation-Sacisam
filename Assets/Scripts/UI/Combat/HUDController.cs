using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Managers;
using Interfaces;
using Habilidades;
using Flags;
using GameInput;

namespace UI.Combat
{
    /// <summary>
    /// Controlador del HUD de combate usando UI Toolkit.
    /// Usa AbilitySlotFactory para generar slots ricos de habilidades.
    ///
    /// Requiere un UIDocument en el mismo GameObject con HUD.uxml asignado.
    /// El USS referenciado desde el UXML provee todos los estilos y transiciones.
    ///
    /// Responsabilidades:
    ///   - Mostrar/ocultar el HUD al inicio/fin de combate.
    ///   - Actualizar nombre, nivel y barras de vida/mana del personaje activo.
    ///   - Generar dinámica mente los botones de habilidad en AbilitiesDinamicHolder.
    ///   - Habilitar/deshabilitar botones de acción según si es el turno del personaje.
    ///   - Poblar los slots del TurnOrder al iniciar combate.
    ///   - Publicar los eventos correctos al EventBus cuando el jugador acciona.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HUDController : MonoBehaviour
    {
        // ── Elementos UXML cacheados ──────────────────────────────────
        private VisualElement _root;
        private VisualElement _turnOrderPanel;
        private VisualElement _abilitiesHolder;

        // Panel de personaje
        private Label         _levelValue;
        private Label         _nameValue;
        private VisualElement _rellenoVida;
        private VisualElement _rellenoMana;

        // Botones de acción
        private Button _abilitiesBtn;
        private Button _itemUseBtn;
        private Button _defBtn;
        private Button _reinforcementBtn;

        // Botón menú principal (top-right, primer botón)
        // TODO: conectar al sistema de menú de inventario/stats cuando esté listo
        private Button _mainMenuBtn;

        // ── Estado interno ────────────────────────────────────────────
        private EntityController _personajeActual;
        private EntityController _personajeConTurno;

        // ─────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        // UI Toolkit crea el panel en OnEnable, NO en Awake.
        // Acceder a rootVisualElement en Awake devuelve un árbol vacío.
        private void OnEnable()
        {
            var doc = GetComponent<UIDocument>();
            if (doc == null)
            {
                Debug.LogError("[HUDController] No hay UIDocument en el GameObject.");
                return;
            }

            _root = doc.rootVisualElement.Q("hud-root");
            if (_root == null)
            {
                Debug.LogError("[HUDController] No se encontró el elemento 'hud-root' en el UXML.");
                return;
            }

            BindElements();
            BindButtons();

            // Comienza oculto; se revela cuando CombatFlowState.Enter() llama
            // a CombatUIController.MostrarHUD() → HUDController.MostrarHUD().
            // El USS ya define display:none en .hud-root como defensa adicional;
            // aquí reforzamos con inline style (máxima prioridad en UI Toolkit).
            _root.style.display = DisplayStyle.None;
        }

        private void Start()
        {
            SuscribirEventos();
        }

        private void OnDestroy()
        {
            DesuscribirEventos();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Binding de elementos

        private void BindElements()
        {
            _turnOrderPanel  = _root.Q("TurnOrder");
            _abilitiesHolder = _root.Q("AbilitiesDinamicHolder");
            _levelValue      = _root.Q<Label>("LevelValue");
            _nameValue       = _root.Q<Label>("NameValue");
            _rellenoVida     = _root.Q("RellenoVida");
            _rellenoMana     = _root.Q("RellenoMana");
        }

        private void BindButtons()
        {
            _abilitiesBtn     = _root.Q<Button>("AbilitiesButton");
            _itemUseBtn       = _root.Q<Button>("ItemUseButton");
            _defBtn           = _root.Q<Button>("DefButton");
            _reinforcementBtn = _root.Q<Button>("ReinforsmentButton");
            _mainMenuBtn      = _root.Q<Button>("MainMenuButton");

            _abilitiesBtn?.RegisterCallback<ClickEvent>(_ => OnClickHabilidades());
            _itemUseBtn?.RegisterCallback<ClickEvent>(_ => OnClickUsarItem());
            _defBtn?.RegisterCallback<ClickEvent>(_ => OnClickDefender());
            _reinforcementBtn?.RegisterCallback<ClickEvent>(_ => OnClickRefuerzos());
            _mainMenuBtn?.RegisterCallback<ClickEvent>(_ => OnClickMainMenu());
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Suscripcion al EventBus

        private void SuscribirEventos()
        {
            EventBus.Suscribir<EventoCombateIniciado>(OnCombateIniciado);
            EventBus.Suscribir<EventoCombateFinalizado>(OnCombateFinalizado);
            EventBus.Suscribir<EventoEsperandoAccionJugador>(OnEsperandoAccion);
            EventBus.Suscribir<EventoTurnoFinalizado>(OnTurnoFinalizado);
            EventBus.Suscribir<EventoHabilidadDesbloqueada>(OnHabilidadDesbloqueada);
            EventBus.Suscribir<EventoHabilidadRemovida>(OnHabilidadRemovida);

            if (GameInputManager.Instance != null)
                GameInputManager.Instance.OnEntityClick += OnEntidadClickeada;
        }

        private void DesuscribirEventos()
        {
            EventBus.Desuscribir<EventoCombateIniciado>(OnCombateIniciado);
            EventBus.Desuscribir<EventoCombateFinalizado>(OnCombateFinalizado);
            EventBus.Desuscribir<EventoEsperandoAccionJugador>(OnEsperandoAccion);
            EventBus.Desuscribir<EventoTurnoFinalizado>(OnTurnoFinalizado);
            EventBus.Desuscribir<EventoHabilidadDesbloqueada>(OnHabilidadDesbloqueada);
            EventBus.Desuscribir<EventoHabilidadRemovida>(OnHabilidadRemovida);

            if (GameInputManager.Instance != null)
                GameInputManager.Instance.OnEntityClick -= OnEntidadClickeada;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Visibilidad publica

        /// <summary>Muestra el HUD de combate.</summary>
        public void MostrarHUD()
        {
            if (_root == null) return;
            _root.style.display = DisplayStyle.Flex;
            Debug.Log("[HUDController] HUD mostrado.");
        }

        /// <summary>Oculta el HUD de combate.</summary>
        public void OcultarHUD()
        {
            if (_root == null) return;
            _root.style.display = DisplayStyle.None;
            MostrarInstruccionObjetivo(null);
            Debug.Log("[HUDController] HUD ocultado.");
        }

        /// <summary>
        /// Muestra u oculta una instrucción de selección de objetivo en el HUD.
        /// Pasa null para ocultar. Llamado por CombatUIController al entrar/salir
        /// del modo SelectingTarget. Reemplaza el instructionPanel uGUI del viejo TargetSelector.
        /// </summary>
        public void MostrarInstruccionObjetivo(string instruccion)
        {
            // Buscar o crear el label de instrucción en el root
            var lbl = _root?.Q<Label>("TargetInstruction");

            if (string.IsNullOrEmpty(instruccion))
            {
                if (lbl != null)
                    lbl.style.display = DisplayStyle.None;
                return;
            }

            if (lbl == null && _root != null)
            {
                lbl = new Label { name = "TargetInstruction" };
                lbl.AddToClassList("target-instruction");
                _root.Add(lbl);
            }

            if (lbl != null)
            {
                lbl.text = instruccion;
                lbl.style.display = DisplayStyle.Flex;
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Handlers del EventBus

        private void OnCombateIniciado(EventoCombateIniciado evento)
        {
            // Solo poblar datos. La visibilidad la controla exclusivamente
            // GameFlowController → CombatFlowState → CombatUIController → MostrarHUD().
            PopularTurnOrder(evento.Jugadores, evento.Enemigos);
        }

        private void OnCombateFinalizado(EventoCombateFinalizado evento)
        {
            // Solo limpiar estado interno. La visibilidad la controla
            // GameFlowController → Pop() → CombatFlowState.Exit() → CombatUIController.OcultarHUD().
            _personajeActual   = null;
            _personajeConTurno = null;
            LimpiarAbilities();
        }

        private void OnEsperandoAccion(EventoEsperandoAccionJugador evento)
        {
            _personajeConTurno = evento.Entidad;
            MostrarPersonaje(evento.Entidad);
        }

        private void OnTurnoFinalizado(EventoTurnoFinalizado evento)
        {
            _personajeConTurno = null;
            ActualizarEstadoBotones();
            LimpiarAbilities();
        }

        private void OnEntidadClickeada(EntityController entidad)
        {
            MostrarPersonaje(entidad);
        }

        // Si una evolución agrega/quita una habilidad a la entidad que el HUD está
        // mostrando en este momento, regeneramos los slots automáticamente.
        private void OnHabilidadDesbloqueada(EventoHabilidadDesbloqueada evt)
        {
            if (_personajeActual?.EntidadLogica == evt.Entidad)
                GenerarBotonesHabilidades();
        }

        private void OnHabilidadRemovida(EventoHabilidadRemovida evt)
        {
            if (_personajeActual?.EntidadLogica == evt.Entidad)
                GenerarBotonesHabilidades();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Mostrar / Actualizar datos del personaje

        private void MostrarPersonaje(EntityController personaje)
        {
            if (personaje == null) return;

            _personajeActual = personaje;

            ActualizarInfoBasica();
            ActualizarBarras();
            ActualizarEstadoBotones();
            ActualizarBotonRefuerzos();
            GenerarBotonesHabilidades();

            Debug.Log($"[HUDController] Mostrando: {personaje.Nombre_Entidad}");
        }

        private void ActualizarInfoBasica()
        {
            if (_personajeActual == null) return;

            if (_levelValue != null)
                _levelValue.text = _personajeActual.Nivel_Entidad.ToString();

            if (_nameValue != null)
                _nameValue.text = _personajeActual.Nombre_Entidad;
        }

        private void ActualizarBarras()
        {
            if (_personajeActual?.EntidadLogica == null) return;

            var entidad = _personajeActual.EntidadLogica;

            int vidaActual = entidad.VidaActual_Entidad;
            int vidaMax    = entidad.Vida_Entidad;
            float pctVida  = vidaMax > 0 ? (float)vidaActual / vidaMax : 0f;

            int manaActual = ObtenerManaActual(entidad);
            int manaMax    = ObtenerManaMaximo(entidad);
            float pctMana  = manaMax > 0 ? (float)manaActual / manaMax : 0f;

            // El USS define transition-duration: 0.3s en .bar-fill,
            // así que la animación la hace CSS; sólo asignamos el valor objetivo.
            if (_rellenoVida != null)
                _rellenoVida.style.width = Length.Percent(pctVida * 100f);

            if (_rellenoMana != null)
                _rellenoMana.style.width = Length.Percent(pctMana * 100f);
        }

        private void ActualizarEstadoBotones()
        {
            bool esSuTurno = _personajeActual != null &&
                             _personajeConTurno != null &&
                             _personajeActual == _personajeConTurno;

            _abilitiesBtn?.SetEnabled(esSuTurno);
            _itemUseBtn?.SetEnabled(esSuTurno);
            _defBtn?.SetEnabled(esSuTurno);
        }

        private void ActualizarBotonRefuerzos()
        {
            if (_reinforcementBtn == null) return;

            bool hay = PlayerPartyManager.Instance != null &&
                       PlayerPartyManager.Instance.HayRefuerzosDisponibles();

            if (hay) _reinforcementBtn.RemoveFromClassList("hidden");
            else     _reinforcementBtn.AddToClassList("hidden");
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Habilidades dinamicas

        private void GenerarBotonesHabilidades()
        {
            LimpiarAbilities();

            if (_personajeActual == null || _abilitiesHolder == null) return;

            var habilidades = _personajeActual.HabilidadesDisponibles;
            if (habilidades == null) return;

            foreach (var hab in habilidades)
            {
                if (hab == null) continue;

                bool disponible = VerificarDisponibilidadHabilidad(hab);
                int cdRestante = _personajeActual.Cooldowns?.ObtenerCooldown(hab) ?? 0;

                // Usar la factory para generar un slot rico con toda la info.
                var slot = AbilitySlotFactory.Crear(hab, disponible, OnHabilidadClickeada, cdRestante);
                _abilitiesHolder.Add(slot);
            }
        }

        private void LimpiarAbilities()
        {
            _abilitiesHolder?.Clear();
        }

        private void OnHabilidadClickeada(HabilidadData habilidad)
        {
            Debug.Log($"[HUDController] Habilidad: {habilidad.nombreHabilidad}");

            EventBus.Publicar(new EventoAccionSeleccionada
            {
                Entidad    = _personajeActual,
                TipoAccion = CombatActionType.Atacar,
                Habilidad  = habilidad
            });
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Botones de accion

        private void OnClickMainMenu()
        {
            // TODO: abrir panel de menú principal (inventario, stats, mapa, etc.)
            // El sistema de menú se implementará en la siguiente fase.
            Debug.Log("[HUDController] MainMenu — pendiente de implementación.");
        }

        private void OnClickHabilidades()
        {
            // Las habilidades ya están permanentemente visibles en AbilitiesDinamicHolder.
            // Este botón queda disponible para expandir a un submenu en el futuro.
        }

        private void OnClickUsarItem()
        {
            Debug.Log("[HUDController] Usar Item");

            EventBus.Publicar(new EventoAccionSeleccionada
            {
                Entidad    = _personajeActual,
                TipoAccion = CombatActionType.UsarItem,
                Habilidad  = null
            });
        }

        private void OnClickDefender()
        {
            Debug.Log("[HUDController] Defender");

            // Defender es auto-objetivo: publicamos directamente el objetivo
            EventBus.Publicar(new EventoObjetivoSeleccionado
            {
                Atacante  = _personajeActual,
                Objetivo  = _personajeActual?.EntidadLogica,
                Habilidad = null
            });
        }

        private void OnClickRefuerzos()
        {
            Debug.Log("[HUDController] Refuerzos");

            if (PlayerPartyManager.Instance == null || _personajeActual == null) return;

            var refuerzos = PlayerPartyManager.Instance
                .GetAvailableReinforcements(_personajeActual.transform.position);

            EventBus.Publicar(new EventoRefuerzosSolicitados
            {
                RefuerzosDisponibles = refuerzos.Select(r => r.Character).ToList(),
                CantidadSolicitada   = 1,
                PosicionCombate      = _personajeActual.transform.position
            });
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Turn Order

        /// <summary>
        /// Llena el panel TurnOrder con un slot por cada combatiente.
        /// El orden exacto de turnos puede actualizarse en el futuro
        /// cuando el GestorCombate exponga la lista ordenada.
        /// </summary>
        private void PopularTurnOrder(List<IEntidadCombate> jugadores, List<IEntidadCombate> enemigos)
        {
            if (_turnOrderPanel == null) return;

            _turnOrderPanel.Clear();

            var todos = new List<IEntidadCombate>();
            if (jugadores != null) todos.AddRange(jugadores);
            if (enemigos  != null) todos.AddRange(enemigos);

            foreach (var entidad in todos)
            {
                string abrev = entidad.Nombre_Entidad.Length > 4
                    ? entidad.Nombre_Entidad.Substring(0, 4)
                    : entidad.Nombre_Entidad;

                var slot = new VisualElement();
                slot.AddToClassList("turn-order-slot");

                var lbl = new Label(abrev);
                lbl.style.color          = new Color(1f, 1f, 1f, 0.9f);
                lbl.style.fontSize       = 8;
                lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                slot.Add(lbl);

                _turnOrderPanel.Add(slot);
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Helpers privados

        private bool VerificarDisponibilidadHabilidad(HabilidadData habilidad)
        {
            if (_personajeActual == null) return false;

            if (_personajeActual.Cooldowns != null &&
                !_personajeActual.Cooldowns.EstaDisponible(habilidad.nombreHabilidad))
                return false;

            if (!habilidad.VerificarCostosRecursos(_personajeActual.EntidadLogica))
                return false;

            return true;
        }

        private int ObtenerManaActual(IEntidadCombate entidad)
        {
            if (entidad is IRecursoProvider p)
                return (int)p.ObtenerRecursoActual(TipoRecurso.Mana);
            return 100;
        }

        private int ObtenerManaMaximo(IEntidadCombate entidad)
        {
            if (entidad is IRecursoProvider p)
                return (int)p.ObtenerRecursoMaximo(TipoRecurso.Mana);
            return 100;
        }

        #endregion
    }
}
