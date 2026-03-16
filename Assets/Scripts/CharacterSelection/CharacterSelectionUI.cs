using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CharacterSelection
{
    /// <summary>
    /// Controlador de la UI de selección de personaje (UI Toolkit).
    /// Conecta los elementos del UXML con CharacterSelectionManager.
    /// 
    /// Requiere UIDocument con CharacterSelection.uxml en el mismo GameObject,
    /// y un CharacterSelectionManager en la escena.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CharacterSelectionUI : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private CharacterSelectionManager manager;

        // ── Elementos cacheados ──
        private VisualElement _root;
        private ScrollView _classList;
        private VisualElement _classIcon;
        private Label _className;
        private Label _classDescription;
        private VisualElement _abilitiesList;
        private VisualElement _pasivasList;
        private TextField _characterName;
        private Button _btnCrear;
        private ScrollView _partyList;
        private Label _partyCount;
        private Button _btnIniciar;

        // Stats
        private VisualElement _barVida, _barAtaque, _barDefensa, _barMana, _barVelocidad;
        private Label _valVida, _valAtaque, _valDefensa, _valMana, _valVelocidad;

        // Estado interno
        private ClaseData _selectedClass;

        // Constantes para normalización de barras de stats.
        // Representan el valor máximo teórico de cada stat (techo, no el máximo actual).
        // Ajustá estos valores si alguna clase supera el techo.
        private const float MAX_VIDA = 2000f;
        private const float MAX_ATAQUE = 150f;
        private const float MAX_DEFENSA = 100f;
        private const float MAX_MANA = 200f;
        private const float MAX_VELOCIDAD = 150f;

        private void OnEnable()
        {
            var doc = GetComponent<UIDocument>();
            _root = doc.rootVisualElement;

            if (_root == null)
            {
                Debug.LogError("[CharacterSelectionUI] rootVisualElement es null.");
                return;
            }

            CacheElements();
            SetupCallbacks();

            if (manager == null)
            {
                manager = FindFirstObjectByType<CharacterSelectionManager>();
                if (manager == null)
                {
                    Debug.LogError("[CharacterSelectionUI] No se encontró CharacterSelectionManager en la escena.");
                    return;
                }
            }

            // Suscribirse a eventos del manager
            manager.OnPersonajeCreado += OnPersonajeCreado;
            manager.OnPersonajeEliminado += OnPersonajeEliminado;

            PopulateClassList();
            RefreshPartyList();
            RefreshButtons();
        }

        private void OnDisable()
        {
            if (manager != null)
            {
                manager.OnPersonajeCreado -= OnPersonajeCreado;
                manager.OnPersonajeEliminado -= OnPersonajeEliminado;
            }
        }

        private void CacheElements()
        {
            _classList = _root.Q<ScrollView>("ClassList");
            _classIcon = _root.Q("ClassIcon");
            _className = _root.Q<Label>("ClassName");
            _classDescription = _root.Q<Label>("ClassDescription");
            _abilitiesList = _root.Q("AbilitiesList");
            _pasivasList = _root.Q("PasivasList");
            _characterName = _root.Q<TextField>("CharacterName");
            _btnCrear = _root.Q<Button>("BtnCrear");
            _partyList = _root.Q<ScrollView>("PartyList");
            _partyCount = _root.Q<Label>("PartyCount");
            _btnIniciar = _root.Q<Button>("BtnIniciar");

            _barVida = _root.Q("BarVida");
            _barAtaque = _root.Q("BarAtaque");
            _barDefensa = _root.Q("BarDefensa");
            _barMana = _root.Q("BarMana");
            _barVelocidad = _root.Q("BarVelocidad");

            _valVida = _root.Q<Label>("ValVida");
            _valAtaque = _root.Q<Label>("ValAtaque");
            _valDefensa = _root.Q<Label>("ValDefensa");
            _valMana = _root.Q<Label>("ValMana");
            _valVelocidad = _root.Q<Label>("ValVelocidad");
        }

        private void SetupCallbacks()
        {
            _btnCrear.clicked += OnCrearClicked;
            _btnIniciar.clicked += OnIniciarClicked;
        }

        // ════════════════════════════════════════════
        // Lista de Clases
        // ════════════════════════════════════════════

        private void PopulateClassList()
        {
            _classList.Clear();

            if (manager?.Config?.clasesDisponibles == null) return;

            foreach (var clase in manager.Config.clasesDisponibles)
            {
                if (clase == null) continue;

                var btn = new VisualElement();
                btn.AddToClassList("class-button");

                var icon = new VisualElement();
                icon.AddToClassList("class-icon-small");
                if (clase.iconoClase != null)
                    icon.style.backgroundImage = new StyleBackground(clase.iconoClase);

                var label = new Label(clase.nombreClase);
                label.AddToClassList("class-btn-name");

                btn.Add(icon);
                btn.Add(label);

                // Captura local para el closure
                var claseLocal = clase;
                var btnLocal = btn;
                btn.RegisterCallback<ClickEvent>(_ => SelectClass(claseLocal, btnLocal));

                _classList.Add(btn);

                // Usar Button wrapper no necesario — VisualElement con ClickEvent funciona
                // pero guardamos referencia para marcar selección
                // (reusamos _classButtons como lista de VisualElements via cast)
            }
        }

        private void SelectClass(ClaseData clase, VisualElement clickedBtn)
        {
            _selectedClass = clase;

            // Actualizar selección visual
            foreach (var child in _classList.contentContainer.Children())
            {
                child.RemoveFromClassList("class-button--selected");
            }
            clickedBtn.AddToClassList("class-button--selected");

            // Actualizar preview
            UpdatePreview(clase);
            RefreshButtons();
        }

        // ════════════════════════════════════════════
        // Preview de Clase
        // ════════════════════════════════════════════

        private void UpdatePreview(ClaseData clase)
        {
            _className.text = clase.nombreClase;
            _classDescription.text = clase.descripcionClase;

            if (clase.iconoClase != null)
                _classIcon.style.backgroundImage = new StyleBackground(clase.iconoClase);
            else
                _classIcon.style.backgroundImage = StyleKeyword.None;

            // Stats bars (porcentaje relativo al máximo)
            SetStatBar(_barVida, _valVida, clase.vidaBase, MAX_VIDA);
            SetStatBar(_barAtaque, _valAtaque, clase.ataqueBase, MAX_ATAQUE);
            SetStatBar(_barDefensa, _valDefensa, clase.defensaBase, MAX_DEFENSA);
            SetStatBar(_barMana, _valMana, clase.manaBase, MAX_MANA);
            SetStatBar(_barVelocidad, _valVelocidad, clase.velocidadBase, MAX_VELOCIDAD);

            // Habilidades iniciales
            _abilitiesList.Clear();
            if (clase.habilidadesIniciales != null)
            {
                foreach (var hab in clase.habilidadesIniciales)
                {
                    if (hab == null) continue;
                    var tag = new Label(hab.nombreHabilidad);
                    tag.AddToClassList("ability-chip");
                    _abilitiesList.Add(tag);
                }
            }

            // Pasivas iniciales
            _pasivasList.Clear();
            if (clase.pasivasIniciales != null)
            {
                foreach (var pasiva in clase.pasivasIniciales)
                {
                    if (pasiva == null) continue;
                    var tag = new Label(pasiva.nombrePasiva);
                    tag.AddToClassList("passive-chip");
                    _pasivasList.Add(tag);
                }
            }

            // Nombre sugerido
            if (string.IsNullOrEmpty(_characterName.value))
            {
                _characterName.value = clase.nombreClase;
            }
        }

        private void SetStatBar(VisualElement bar, Label valLabel, float value, float max)
        {
            float pct = Mathf.Clamp01(value / max) * 100f;
            bar.style.width = new Length(pct, LengthUnit.Percent);
            valLabel.text = Mathf.RoundToInt(value).ToString();
        }

        // ════════════════════════════════════════════
        // Crear Personaje
        // ════════════════════════════════════════════

        private void OnCrearClicked()
        {
            if (_selectedClass == null || manager == null) return;

            string nombre = _characterName.value?.Trim();
            if (string.IsNullOrEmpty(nombre))
            {
                nombre = _selectedClass.nombreClase;
            }

            if (manager.CrearPersonaje(_selectedClass, nombre))
            {
                // Limpiar campo de nombre
                _characterName.value = "";
            }
        }

        // ════════════════════════════════════════════
        // Iniciar Juego
        // ════════════════════════════════════════════

        private void OnIniciarClicked()
        {
            manager?.IniciarJuego();
        }

        // ════════════════════════════════════════════
        // Party List
        // ════════════════════════════════════════════

        private void RefreshPartyList()
        {
            _partyList.Clear();

            if (manager == null) return;

            var personajes = manager.PersonajesCreados;
            _partyCount.text = $"{personajes.Count} / {manager.Config.maxPersonajesInicial}";

            for (int i = 0; i < personajes.Count; i++)
            {
                var data = personajes[i];
                var slot = CreatePartySlot(data, i);
                _partyList.Add(slot);
            }
        }

        private VisualElement CreatePartySlot(CharacterCreationData data, int index)
        {
            var slot = new VisualElement();
            slot.AddToClassList("party-card");
            if (data.esMain) slot.AddToClassList("party-slot-main");

            // Icono
            var icon = new VisualElement();
            icon.AddToClassList("party-avatar");
            if (data.clase?.iconoClase != null)
                icon.style.backgroundImage = new StyleBackground(data.clase.iconoClase);
            slot.Add(icon);

            // Info
            var info = new VisualElement();
            info.AddToClassList("party-slot-info");

            var nombre = new Label(data.nombre);
            nombre.AddToClassList("party-char-name");
            info.Add(nombre);

            var clase = new Label(data.clase?.nombreClase ?? "???");
            clase.AddToClassList("party-char-class");
            info.Add(clase);

            if (data.esMain)
            {
                var badge = new Label("★ MAIN");
                badge.AddToClassList("party-slot-main-badge");
                info.Add(badge);
            }

            slot.Add(info);

            // Botón eliminar
            var btnRemove = new Button { text = "✕" };
            btnRemove.AddToClassList("btn-remove");
            int idx = index; // captura local
            btnRemove.clicked += () => manager?.EliminarPersonaje(idx);
            slot.Add(btnRemove);

            // Click para establecer como main
            int mainIdx = index;
            slot.RegisterCallback<ClickEvent>(evt =>
            {
                // No hacer nada si se hizo click en el botón de eliminar
                if (evt.target is Button) return;
                manager?.EstablecerMain(mainIdx);
                RefreshPartyList();
            });

            return slot;
        }

        // ════════════════════════════════════════════
        // Refresh (reacción a eventos)
        // ════════════════════════════════════════════

        private void RefreshButtons()
        {
            bool canCreate = _selectedClass != null && manager != null && manager.PuedeCrearMas;
            _btnCrear.SetEnabled(canCreate);
            _btnIniciar.SetEnabled(manager != null && manager.PuedeIniciar);
        }

        private void OnPersonajeCreado(CharacterCreationData _)
        {
            RefreshPartyList();
            RefreshButtons();
        }

        private void OnPersonajeEliminado(int _)
        {
            RefreshPartyList();
            RefreshButtons();
        }
    }
}
