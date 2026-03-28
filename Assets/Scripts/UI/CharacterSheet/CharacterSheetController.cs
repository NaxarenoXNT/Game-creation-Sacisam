using System;
using System.Collections.Generic;
using System.Linq;
using Evolution;
using Flags;
using GameFlow;
using Habilidades;
using Managers;
using Missions;
using Padres;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.CharacterSheet
{
    /// <summary>
    /// Controlador de la ficha de personaje en UI Toolkit.
    ///
    /// Responsabilidades:
    /// - Abrir/cerrar la ficha y bloquear el input del juego mientras está visible.
    /// - Permitir inspeccionar a los miembros de la party activa.
    /// - Mostrar stats runtime, afinidades elementales, habilidades y pasivas.
    /// - Mostrar progreso de traits/cadenas usando EvolutionState por characterId.
    ///
    /// Restricciones actuales del proyecto:
    /// - Equipo y concurrencias se dejan como placeholders funcionales sin backend real.
    /// - El EvolutionState se consulta al MissionManager, que hoy es la fuente accesible.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CharacterSheetController : MonoBehaviour
    {
        private enum TraitViewMode
        {
            Obtained,
            Chains
        }

        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.C;
        [SerializeField] private bool openOnStart;

        private UIDocument _document;

        // Root
        private VisualElement _root;
        private Label _runLabel;

        // Party
        private ScrollView _partyButtons;

        // Summary
        private Label _lvBadge;
        private VisualElement _portraitImage;
        private Label _charName;
        private Label _charClass;
        private Button _btnTraits;

        // Left column
        private VisualElement _statsList;
        private ScrollView _elementsList;
        private Label _elementsEmptyState;
        private VisualElement _equipList;
        private Label _equipEmptyState;

        // Right column
        private ScrollView _concList;
        private Label _concEmptyState;
        private Button _tabActivas;
        private Button _tabPasivas;
        private ScrollView _skillsList;
        private Label _skillsEmptyState;

        // Traits overlay
        private VisualElement _traitsOverlay;
        private Label _traitsTitle;
        private Label _traitsSub;
        private Button _btnCloseTraits;
        private ScrollView _traitsSidebar;
        private Label _traitsSidebarEmptyState;
        private ScrollView _traitsContent;
        private Label _traitsContentEmptyState;

        private bool _isOpen;
        private bool _showPassives;
        private bool _staticCallbacksBound;
        private bool _initialized;
        private TraitViewMode _traitViewMode = TraitViewMode.Obtained;

        private EntityController _selectedCharacter;
        private MissionManager _missionManager;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (_document == null)
            {
                Debug.LogError("[CharacterSheetController] No hay UIDocument en el GameObject.");
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (!_initialized) return; // Start() maneja la primera inicialización

            SubscribeGlobalEvents();
            EnsureSelectedCharacter();
        }

        private void Start()
        {
            // UIDocument garantiza su panel para Start(), no para OnEnable()
            _root = _document.rootVisualElement.Q<VisualElement>("root");
            if (_root == null)
            {
                Debug.LogError("[CharacterSheetController] No se encontró el elemento 'root' en CharacterSheet.uxml.");
                enabled = false;
                return;
            }

            CacheElements();
            BindStaticCallbacks();
            SetSheetVisible(false);
            TryRefreshExternalRefs();
            SubscribeGlobalEvents();
            EnsureSelectedCharacter();
            _initialized = true;

            RefreshAll();

            if (openOnStart)
                ShowSheet();
        }

        private void OnDisable()
        {
            if (_isOpen)
                HideSheet();

            UnsubscribeGlobalEvents();
            UnsubscribeSelectedCharacter();
        }

        private void Update()
        {
            // Solo procesar input cuando el contexto lo permite
            var inputManager = GameInput.GameInputManager.Instance;
            if (inputManager == null) return;

            var context = inputManager.CurrentContext;

            if (Input.GetKeyDown(toggleKey))
            {
                if (_isOpen && context == GameInput.InputContext.Menu)
                {
                    HideSheet();
                }
                else if (!_isOpen && context == GameInput.InputContext.Exploration)
                {
                    var flow = GameFlowController.Instance;
                    if (flow != null && flow.IsInState<ExplorationFlowState>())
                        ShowSheet();
                }
                return;
            }

            if (!_isOpen) return;

            if (context == GameInput.InputContext.Menu && Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsTraitsOverlayOpen())
                    CloseTraitsOverlay();
                else
                    HideSheet();
            }
        }

        public void ToggleSheet()
        {
            if (_isOpen) HideSheet();
            else ShowSheet();
        }

        public void ShowSheet()
        {
            if (_isOpen) return;

            EnsureSelectedCharacter();
            if (_selectedCharacter == null)
            {
                Debug.LogWarning("[CharacterSheetController] No hay personaje seleccionable en la party activa.");
                return;
            }

            var flow = GameFlowController.Instance;
            if (flow != null)
            {
                flow.Push(new CharacterSheetFlowState());
                if (!flow.IsInState<CharacterSheetFlowState>())
                    return; // Push rechazado por el estado actual
            }

            SetSheetVisible(true);
            RefreshAll();
        }

        public void HideSheet()
        {
            if (!_isOpen) return;

            CloseTraitsOverlay();
            SetSheetVisible(false);

            var flow = GameFlowController.Instance;
            if (flow != null && flow.IsInState<CharacterSheetFlowState>())
                flow.Pop();
        }

        private void SetSheetVisible(bool visible)
        {
            _isOpen = visible;

            if (_root != null)
                _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void CacheElements()
        {
            _runLabel = _root.Q<Label>("RunLabel");

            _partyButtons = _root.Q<ScrollView>("PartyButtons");

            _lvBadge = _root.Q<Label>("LvBadge");
            _portraitImage = _root.Q<VisualElement>("PortraitImage");
            _charName = _root.Q<Label>("CharName");
            _charClass = _root.Q<Label>("CharClass");
            _btnTraits = _root.Q<Button>("BtnTraits");

            _statsList = _root.Q<VisualElement>("StatsList");
            _elementsList = _root.Q<ScrollView>("ItemsList");
            _elementsEmptyState = _root.Q<Label>("ElementsEmptyState");
            _equipList = _root.Q<VisualElement>("EquipList");
            _equipEmptyState = _root.Q<Label>("EquipEmptyState");

            _concList = _root.Q<ScrollView>("ConcList");
            _concEmptyState = _root.Q<Label>("ConcEmptyState");
            _tabActivas = _root.Q<Button>("TabActivas");
            _tabPasivas = _root.Q<Button>("TabPasivas");
            _skillsList = _root.Q<ScrollView>("SkillsList");
            _skillsEmptyState = _root.Q<Label>("SkillsEmptyState");

            _traitsOverlay = _root.Q<VisualElement>("TraitsOverlay");
            _traitsTitle = _root.Q<Label>("TraitsTitle");
            _traitsSub = _root.Q<Label>("TraitsSub");
            _btnCloseTraits = _root.Q<Button>("BtnCloseTraits");
            _traitsSidebar = _root.Q<ScrollView>("TraitsSidebar");
            _traitsSidebarEmptyState = _root.Q<Label>("TraitsSidebarEmptyState");
            _traitsContent = _root.Q<ScrollView>("TraitsContent");
            _traitsContentEmptyState = _root.Q<Label>("TraitsContentEmptyState");
        }

        private void BindStaticCallbacks()
        {
            if (_staticCallbacksBound) return;

            if (_btnTraits != null)
                _btnTraits.clicked += OpenTraitsOverlay;

            if (_btnCloseTraits != null)
                _btnCloseTraits.clicked += CloseTraitsOverlay;

            if (_tabActivas != null)
                _tabActivas.clicked += () => SetSkillMode(showPassives: false);

            if (_tabPasivas != null)
                _tabPasivas.clicked += () => SetSkillMode(showPassives: true);

            _staticCallbacksBound = true;
        }

        private void SubscribeGlobalEvents()
        {
            var partyManager = PlayerPartyManager.Instance;
            if (partyManager != null)
            {
                partyManager.OnMainChanged += OnPartyCompositionChanged;
                partyManager.OnCharacterJoinedParty += OnPartyCompositionChanged;
                partyManager.OnCharacterLeftParty += OnPartyCompositionChanged;
            }

            EventBus.Suscribir<EventoPersonajeRegistrado>(OnPersonajeRegistrado);
            EventBus.Suscribir<EventoTraitObtenido>(OnTraitObtenido);
            EventBus.Suscribir<EventoEvolucionAplicada>(OnEvolucionAplicada);
            EventBus.Suscribir<EventoGameFlowChanged>(OnGameFlowChanged);
        }

        private void UnsubscribeGlobalEvents()
        {
            var partyManager = PlayerPartyManager.Instance;
            if (partyManager != null)
            {
                partyManager.OnMainChanged -= OnPartyCompositionChanged;
                partyManager.OnCharacterJoinedParty -= OnPartyCompositionChanged;
                partyManager.OnCharacterLeftParty -= OnPartyCompositionChanged;
            }

            EventBus.Desuscribir<EventoPersonajeRegistrado>(OnPersonajeRegistrado);
            EventBus.Desuscribir<EventoTraitObtenido>(OnTraitObtenido);
            EventBus.Desuscribir<EventoEvolucionAplicada>(OnEvolucionAplicada);
            EventBus.Desuscribir<EventoGameFlowChanged>(OnGameFlowChanged);
        }

        private void SubscribeSelectedCharacter()
        {
            if (_selectedCharacter == null) return;

            if (_selectedCharacter.EntidadLogica != null)
                _selectedCharacter.EntidadLogica.OnVidaCambiada += OnSelectedVitalsChanged;

            _selectedCharacter.OnNivelSubido += OnSelectedLevelChanged;
            _selectedCharacter.OnManaCambiado += OnSelectedManaChanged;

            if (_selectedCharacter.EntidadLogica is Jugador jugador)
            {
                jugador.GestorHabilidades.OnHabilidadesCambiadas += OnSelectedSkillsChanged;

                if (jugador.GestorPasivas != null)
                {
                    jugador.GestorPasivas.OnPasivaAgregada += OnSelectedPassiveChanged;
                    jugador.GestorPasivas.OnPasivaRemovida += OnSelectedPassiveChanged;
                }
            }
        }

        private void UnsubscribeSelectedCharacter()
        {
            if (_selectedCharacter == null) return;

            if (_selectedCharacter.EntidadLogica != null)
                _selectedCharacter.EntidadLogica.OnVidaCambiada -= OnSelectedVitalsChanged;

            _selectedCharacter.OnNivelSubido -= OnSelectedLevelChanged;
            _selectedCharacter.OnManaCambiado -= OnSelectedManaChanged;

            if (_selectedCharacter.EntidadLogica is Jugador jugador)
            {
                jugador.GestorHabilidades.OnHabilidadesCambiadas -= OnSelectedSkillsChanged;

                if (jugador.GestorPasivas != null)
                {
                    jugador.GestorPasivas.OnPasivaAgregada -= OnSelectedPassiveChanged;
                    jugador.GestorPasivas.OnPasivaRemovida -= OnSelectedPassiveChanged;
                }
            }
        }

        private void OnPersonajeRegistrado(EventoPersonajeRegistrado _)
        {
            RefreshAll();
        }

        private void OnPartyCompositionChanged(EntityController _, EntityController __)
        {
            HandlePartyCompositionChanged();
        }

        private void OnPartyCompositionChanged(EntityController _)
        {
            HandlePartyCompositionChanged();
        }

        private void HandlePartyCompositionChanged()
        {
            EnsureSelectedCharacter();
            RefreshAll();
        }

        private void OnTraitObtenido(EventoTraitObtenido evt)
        {
            if (_selectedCharacter == null || evt.CharacterId != _selectedCharacter.CharacterId) return;
            RefreshTraitsOverlay();
        }

        private void OnEvolucionAplicada(EventoEvolucionAplicada evt)
        {
            if (_selectedCharacter == null || evt.CharacterId != _selectedCharacter.CharacterId) return;
            RefreshTraitsOverlay();
        }

        private void OnGameFlowChanged(EventoGameFlowChanged evt)
        {
            if (!_isOpen) return;
            if (evt.NuevoEstado is CharacterSheetFlowState) return;

            // Otro estado tomó el control (ej: combate). Cerrar solo la visual
            // sin hacer Pop (el estado ya fue reemplazado/sacado por el flow).
            CloseTraitsOverlay();
            SetSheetVisible(false);
        }

        private void OnSelectedVitalsChanged(int _, int __)
        {
            RefreshSummary();
            RefreshStats();
        }

        private void OnSelectedLevelChanged(int _)
        {
            RefreshSummary();
            RefreshStats();
        }

        private void OnSelectedManaChanged(int _, int __)
        {
            RefreshStats();
        }

        private void OnSelectedSkillsChanged()
        {
            RefreshSkills();
        }

        private void OnSelectedPassiveChanged(PasivaData _)
        {
            RefreshSkills();
        }

        private void TryRefreshExternalRefs()
        {
            if (_missionManager == null)
                _missionManager = FindFirstObjectByType<MissionManager>();
        }

        private void EnsureSelectedCharacter()
        {
            var party = GetPartyCharacters();
            if (party.Count == 0)
            {
                SelectCharacter(null);
                return;
            }

            if (_selectedCharacter != null && party.Contains(_selectedCharacter))
                return;

            SelectCharacter(party[0]);
        }

        private void SelectCharacter(EntityController character)
        {
            if (_selectedCharacter == character) return;

            UnsubscribeSelectedCharacter();
            _selectedCharacter = character;
            SubscribeSelectedCharacter();
        }

        private List<EntityController> GetPartyCharacters()
        {
            var result = new List<EntityController>();
            var partyManager = PlayerPartyManager.Instance;
            if (partyManager == null) return result;

            if (partyManager.MainCharacter != null)
                result.Add(partyManager.MainCharacter);

            foreach (var member in partyManager.ActiveParty)
            {
                if (member != null && !result.Contains(member))
                    result.Add(member);
            }

            return result;
        }

        private void RefreshAll()
        {
            if (_root == null) return;

            TryRefreshExternalRefs();
            RefreshPartyButtons();
            RefreshRunLabel();
            RefreshSummary();
            RefreshStats();
            RefreshElements();
            RefreshEquipmentPlaceholder();
            RefreshConcurrenciasPlaceholder();
            RefreshSkills();
            RefreshTraitsOverlay();
        }

        private void RefreshPartyButtons()
        {
            if (_partyButtons == null) return;

            _partyButtons.Clear();

            foreach (var character in GetPartyCharacters())
            {
                var button = new Button(() =>
                {
                    SelectCharacter(character);
                    RefreshAll();
                })
                {
                    text = character.DisplayName
                };

                button.AddToClassList("cs-party-btn");
                if (character == _selectedCharacter)
                    button.AddToClassList("cs-party-btn--active");

                _partyButtons.Add(button);
            }
        }

        private void RefreshRunLabel()
        {
            if (_runLabel == null) return;
            _runLabel.text = $"Activos: {GetPartyCharacters().Count}";
        }

        private void RefreshSummary()
        {
            if (_charName == null || _charClass == null || _lvBadge == null) return;

            if (_selectedCharacter == null)
            {
                _charName.text = "—";
                _charClass.text = "—";
                _lvBadge.text = "Lv. —";
                if (_portraitImage != null)
                    _portraitImage.style.backgroundImage = StyleKeyword.None;
                return;
            }

            _charName.text = _selectedCharacter.DisplayName;
            _charClass.text = _selectedCharacter.DatosClase?.nombreClase ?? "—";
            _lvBadge.text = $"Lv. {_selectedCharacter.Nivel_Entidad}";

            if (_portraitImage != null)
            {
                if (_selectedCharacter.SpritePersonaje != null)
                    _portraitImage.style.backgroundImage = new StyleBackground(_selectedCharacter.SpritePersonaje);
                else
                    _portraitImage.style.backgroundImage = StyleKeyword.None;
            }
        }

        private void RefreshStats()
        {
            if (_statsList == null) return;

            _statsList.Clear();

            if (_selectedCharacter == null)
                return;

            AddStatRow("VIDA", "vida",
                _selectedCharacter.VidaActual_Entidad,
                Mathf.Max(_selectedCharacter.Vida_Entidad, 1),
                $"{_selectedCharacter.VidaActual_Entidad}/{_selectedCharacter.Vida_Entidad}");

            AddStatRow("ATK", "ataque",
                _selectedCharacter.PuntosDeAtaque_Entidad,
                Mathf.Max(CalculateProjectedAttack(_selectedCharacter), 1f),
                _selectedCharacter.PuntosDeAtaque_Entidad.ToString());

            AddStatRow("DEF", "defensa",
                _selectedCharacter.PuntosDeDefensa_Entidad,
                Mathf.Max(CalculateProjectedDefense(_selectedCharacter), 1f),
                _selectedCharacter.PuntosDeDefensa_Entidad.ToString("0.#"));

            if (_selectedCharacter.EntidadLogica is Jugador jugador)
            {
                AddStatRow("RECURSO", "mana",
                    jugador.ManaActual_jugador,
                    Mathf.Max(jugador.Mana_jugador, 1),
                    $"{jugador.ManaActual_jugador}/{jugador.Mana_jugador}");
            }

            AddStatRow("SPD", "velocidad",
                _selectedCharacter.Velocidad,
                Mathf.Max(CalculateProjectedSpeed(_selectedCharacter), 1f),
                _selectedCharacter.Velocidad.ToString());
        }

        private void AddStatRow(string statName, string modifierClass, float currentValue, float maxValue, string valueText)
        {
            var row = new VisualElement();
            row.AddToClassList("cs-stat-row");

            var label = new Label(statName);
            label.AddToClassList("cs-stat-name");

            var barBg = new VisualElement();
            barBg.AddToClassList("cs-bar-bg");

            var barFill = new VisualElement();
            barFill.AddToClassList("cs-bar-fill");
            barFill.AddToClassList($"cs-bar-fill--{modifierClass}");
            barFill.style.width = Length.Percent(Mathf.Clamp01(currentValue / Mathf.Max(maxValue, 1f)) * 100f);
            barBg.Add(barFill);

            var value = new Label(valueText);
            value.AddToClassList("cs-stat-val");

            row.Add(label);
            row.Add(barBg);
            row.Add(value);
            _statsList.Add(row);
        }

        private void RefreshElements()
        {
            if (_elementsList == null) return;

            _elementsList.Clear();

            var statuses = _selectedCharacter?.EntityStats?.activeStatuses;
            if (statuses == null || statuses.Count == 0)
            {
                if (_elementsEmptyState != null)
                    _elementsList.Add(_elementsEmptyState);
                return;
            }

            foreach (var status in statuses.Where(s => s?.definition != null).OrderBy(s => s.definition.elementName))
            {
                var row = new VisualElement();
                row.AddToClassList("cs-item-row");

                var icon = new VisualElement();
                icon.AddToClassList("cs-item-icon");
                icon.style.backgroundColor = new StyleColor(status.definition.elementColor);

                var info = new VisualElement();
                var name = new Label(status.definition.elementName);
                name.AddToClassList("cs-item-name");

                var detail = new Label($"Lv. {status.level} · XP {Mathf.RoundToInt(status.GetXPProgress() * 100f)}%");
                detail.AddToClassList("cs-item-type");

                info.Add(name);
                info.Add(detail);
                row.Add(icon);
                row.Add(info);
                _elementsList.Add(row);
            }
        }

        private void RefreshEquipmentPlaceholder()
        {
            if (_equipList == null || _equipEmptyState == null) return;

            _equipList.Clear();
            _equipList.Add(_equipEmptyState);
            _equipEmptyState.style.display = DisplayStyle.Flex;
        }

        private void RefreshConcurrenciasPlaceholder()
        {
            if (_concList == null || _concEmptyState == null) return;

            _concList.Clear();
            _concList.Add(_concEmptyState);
            _concEmptyState.style.display = DisplayStyle.Flex;
        }

        private void SetSkillMode(bool showPassives)
        {
            _showPassives = showPassives;
            _tabActivas?.EnableInClassList("cs-skill-tab--active", !_showPassives);
            _tabPasivas?.EnableInClassList("cs-skill-tab--active", _showPassives);
            RefreshSkills();
        }

        private void RefreshSkills()
        {
            if (_skillsList == null) return;

            _skillsList.Clear();

            if (_selectedCharacter == null)
            {
                AddSkillsEmptyState();
                return;
            }

            if (_showPassives)
                BuildPassiveCards();
            else
                BuildActiveSkillCards();
        }

        private void BuildActiveSkillCards()
        {
            var skills = _selectedCharacter.HabilidadesDisponibles;
            if (skills == null || skills.Count == 0)
            {
                AddSkillsEmptyState();
                return;
            }

            foreach (var skill in skills.Where(s => s != null))
            {
                string meta = $"{PrettyEnum(skill.categoria)} · {PrettyEnum(skill.tipoObjetivo)}";
                var tags = new List<VisualElement>
                {
                    CreateTag(skill.TieneCosto() ? skill.ObtenerDescripcionCostos() : "Sin costo", "cs-skill-tag--info")
                };

                tags.Add(CreateTag(skill.cooldownTurnos > 0 ? $"CD {skill.cooldownTurnos}" : "Sin CD",
                    skill.cooldownTurnos > 0 ? "cs-skill-tag--cd" : "cs-skill-tag--ok"));

                tags.Add(CreateTag(PrettyEnum(skill.categoria), "cs-skill-tag--lv"));

                _skillsList.Add(CreateSkillCard(
                    skill.nombreHabilidad,
                    meta,
                    skill.descripcion,
                    isPassive: false,
                    tags));
            }
        }

        private void BuildPassiveSkillCards(IEnumerable<PasivaData> passives)
        {
            foreach (var passive in passives)
            {
                bool active = _selectedCharacter.EntidadLogica?.GestorPasivas?.EstaPasivaActiva(passive) ?? false;
                string meta = $"{PrettyEnum(passive.categoria)} · {(passive.siempreActiva ? "Siempre activa" : "Condicional")}";

                var tags = new List<VisualElement>
                {
                    CreateTag(PrettyEnum(passive.categoria), "cs-skill-tag--lv"),
                    CreateTag(active ? "Activa" : "Inactiva", active ? "cs-skill-tag--ok" : "cs-skill-tag--warn")
                };

                if (!passive.siempreActiva)
                    tags.Add(CreateTag($"Cond: {PrettyEnum(passive.condicion)}", "cs-skill-tag--info"));

                _skillsList.Add(CreateSkillCard(
                    passive.nombrePasiva,
                    meta,
                    passive.descripcion,
                    isPassive: true,
                    tags));
            }
        }

        private void BuildPassiveCards()
        {
            var passives = _selectedCharacter.EntidadLogica?.GestorPasivas?.ObtenerTodasLasPasivas();
            if (passives == null || passives.Count == 0)
            {
                AddSkillsEmptyState();
                return;
            }

            BuildPassiveSkillCards(passives.Where(p => p != null));
        }

        private VisualElement CreateSkillCard(string titleText, string metaText, string description, bool isPassive,
            IEnumerable<VisualElement> tags)
        {
            var card = new VisualElement();
            card.AddToClassList("cs-skill-card");

            var header = new VisualElement();
            header.AddToClassList("cs-skill-header");

            var badge = new VisualElement();
            badge.AddToClassList("cs-skill-badge");
            if (isPassive)
                badge.AddToClassList("cs-skill-badge--passive");

            var titleColumn = new VisualElement();
            titleColumn.style.flexGrow = 1f;

            var title = new Label(titleText);
            title.AddToClassList("cs-skill-title");

            var meta = new Label(metaText);
            meta.AddToClassList("cs-skill-meta");

            titleColumn.Add(title);
            titleColumn.Add(meta);

            var arrow = new Label("▼");
            arrow.AddToClassList("cs-skill-arrow");

            header.Add(badge);
            header.Add(titleColumn);
            header.Add(arrow);

            var body = new VisualElement();
            body.AddToClassList("cs-skill-body");
            body.style.display = DisplayStyle.None;

            var desc = new Label(description ?? string.Empty);
            desc.AddToClassList("cs-skill-desc");
            body.Add(desc);

            if (tags != null)
            {
                var tagContainer = new VisualElement();
                tagContainer.AddToClassList("cs-skill-tags");
                foreach (var tag in tags)
                    tagContainer.Add(tag);
                body.Add(tagContainer);
            }

            void ToggleExpanded()
            {
                bool expanded = card.ClassListContains("cs-skill-card--expanded");
                card.EnableInClassList("cs-skill-card--expanded", !expanded);
                body.style.display = expanded ? DisplayStyle.None : DisplayStyle.Flex;
                arrow.text = expanded ? "▼" : "▲";
            }

            header.RegisterCallback<ClickEvent>(_ => ToggleExpanded());
            card.RegisterCallback<ClickEvent>(_ =>
            {
                if (_.target == card)
                    ToggleExpanded();
            });

            card.Add(header);
            card.Add(body);
            return card;
        }

        private VisualElement CreateTag(string text, string modifierClass)
        {
            var tag = new Label(text);
            tag.AddToClassList("cs-skill-tag");
            if (!string.IsNullOrWhiteSpace(modifierClass))
                tag.AddToClassList(modifierClass);
            return tag;
        }

        private void AddSkillsEmptyState()
        {
            if (_skillsEmptyState != null)
                _skillsList.Add(_skillsEmptyState);
        }

        private void OpenTraitsOverlay()
        {
            if (_traitsOverlay == null || _selectedCharacter == null) return;
            _traitsOverlay.style.display = DisplayStyle.Flex;
            RefreshTraitsOverlay();
        }

        private void CloseTraitsOverlay()
        {
            if (_traitsOverlay != null)
                _traitsOverlay.style.display = DisplayStyle.None;
        }

        private bool IsTraitsOverlayOpen()
        {
            return _traitsOverlay != null && _traitsOverlay.resolvedStyle.display != DisplayStyle.None;
        }

        private void RefreshTraitsOverlay()
        {
            if (_traitsOverlay == null || _traitsTitle == null || _traitsSub == null) return;

            if (_selectedCharacter == null)
            {
                _traitsTitle.text = "— Traits";
                _traitsSub.text = string.Empty;
                return;
            }

            EvolutionState state = GetSelectedEvolutionState();
            var traitManager = TraitProgressionManager.Instance;
            var obtainedTraits = GetObtainedStandaloneTraits(traitManager, state);
            var relevantChains = GetRelevantChains(traitManager, state);

            _traitsTitle.text = $"{_selectedCharacter.DisplayName} · Traits";
            _traitsSub.text = state == null
                ? "Sin EvolutionState registrado para este personaje."
                : $"{obtainedTraits.Count} individuales · {relevantChains.Count} cadenas activas o disponibles";

            BuildTraitsSidebar(obtainedTraits.Count, relevantChains.Count);

            if (_traitViewMode == TraitViewMode.Obtained)
                BuildObtainedTraitsContent(state, obtainedTraits);
            else
                BuildChainsContent(state, relevantChains);
        }

        private EvolutionState GetSelectedEvolutionState()
        {
            if (_selectedCharacter == null) return null;
            TryRefreshExternalRefs();
            return _missionManager?.GetEstadoPersonaje(_selectedCharacter.CharacterId);
        }

        private List<TraitDefinition> GetObtainedStandaloneTraits(TraitProgressionManager manager, EvolutionState state)
        {
            if (manager == null || state == null) return new List<TraitDefinition>();

            return manager.StandaloneTraits
                .Where(t => t != null && state.traitStacks.ContainsKey(t.id))
                .OrderBy(t => t.nombreMostrar)
                .ToList();
        }

        private List<ChainProgressInfo> GetRelevantChains(TraitProgressionManager manager, EvolutionState state)
        {
            if (manager == null || state == null) return new List<ChainProgressInfo>();

            var clase = _selectedCharacter?.DatosClase;
            return manager.GetProgresoTodasLasCadenas(state)
                .Where(info => info != null && info.chain != null)
                .Where(info =>
                    info.nodosCompletados > 0 ||
                    manager.GetNodoDisponibleDeCadena(info.chain, state, clase) >= 0)
                .OrderByDescending(info => info.nodosCompletados)
                .ThenBy(info => info.chain.nombreBase)
                .ToList();
        }

        private void BuildTraitsSidebar(int obtainedCount, int chainCount)
        {
            if (_traitsSidebar == null) return;

            _traitsSidebar.Clear();

            var groupLabel = new Label("PROGRESION");
            groupLabel.AddToClassList("cs-sidebar-group-label");
            _traitsSidebar.Add(groupLabel);

            _traitsSidebar.Add(CreateTraitCategoryButton(
                $"Obtenidos ({obtainedCount})",
                TraitViewMode.Obtained,
                _traitViewMode == TraitViewMode.Obtained));

            _traitsSidebar.Add(CreateTraitCategoryButton(
                $"Cadenas ({chainCount})",
                TraitViewMode.Chains,
                _traitViewMode == TraitViewMode.Chains));

            if (obtainedCount == 0 && chainCount == 0 && _traitsSidebarEmptyState != null)
                _traitsSidebar.Add(_traitsSidebarEmptyState);
        }

        private Button CreateTraitCategoryButton(string text, TraitViewMode mode, bool active)
        {
            var button = new Button(() =>
            {
                _traitViewMode = mode;
                RefreshTraitsOverlay();
            })
            {
                text = text
            };

            button.AddToClassList("cs-tcat-btn");
            if (active)
                button.AddToClassList("cs-tcat-btn--active");

            return button;
        }

        private void BuildObtainedTraitsContent(EvolutionState state, List<TraitDefinition> obtainedTraits)
        {
            if (_traitsContent == null) return;

            _traitsContent.Clear();

            if (state == null || obtainedTraits.Count == 0)
            {
                if (_traitsContentEmptyState != null)
                    _traitsContent.Add(_traitsContentEmptyState);
                return;
            }

            foreach (var trait in obtainedTraits)
                _traitsContent.Add(CreateTraitCard(trait, state));
        }

        private VisualElement CreateTraitCard(TraitDefinition trait, EvolutionState state)
        {
            var card = new VisualElement();
            card.AddToClassList("cs-trait-card");

            var header = new VisualElement();
            header.AddToClassList("cs-trait-card-header");

            var icon = new VisualElement();
            icon.AddToClassList("cs-trait-icon");
            if (trait.icono != null)
                icon.style.backgroundImage = new StyleBackground(trait.icono);

            var info = new VisualElement();
            var name = new Label(trait.nombreMostrar ?? trait.id);
            name.AddToClassList("cs-trait-name");

            var statusRow = new VisualElement();
            statusRow.AddToClassList("cs-trait-status-row");
            statusRow.Add(CreateBadge("Obtenido", "cs-badge-unlocked"));
            statusRow.Add(CreateBadge($"x{GetTraitStacks(state, trait.id)}", "cs-badge-type"));

            switch (trait.rareza)
            {
                case EvolutionRarity.Rare:
                case EvolutionRarity.Epic:
                    statusRow.Add(CreateBadge(PrettyEnum(trait.rareza), "cs-badge-rare"));
                    break;
                case EvolutionRarity.Legendary:
                    statusRow.Add(CreateBadge("Legendary", "cs-badge-legendary"));
                    break;
            }

            info.Add(name);
            info.Add(statusRow);
            header.Add(icon);
            header.Add(info);

            var desc = new Label(trait.descripcion ?? string.Empty);
            desc.AddToClassList("cs-trait-desc");

            var conds = new VisualElement();
            conds.AddToClassList("cs-trait-conds");
            foreach (var cond in trait.condiciones.Where(c => c != null))
                conds.Add(CreateConditionRow(cond, state));

            card.Add(header);
            card.Add(desc);
            card.Add(conds);
            return card;
        }

        private VisualElement CreateBadge(string text, string className)
        {
            var badge = new Label(text);
            badge.AddToClassList(className);
            return badge;
        }

        private VisualElement CreateConditionRow(EvolutionConditionSO condition, EvolutionState state)
        {
            bool met = condition.Evaluar(state);
            float progress = Mathf.Clamp01(condition.GetProgreso(state));

            var row = new VisualElement();
            row.AddToClassList("cs-cond-row");

            var dot = new VisualElement();
            dot.AddToClassList("cs-cond-dot");
            if (met)
                dot.AddToClassList("cs-cond-dot--met");

            var text = new Label(condition.GetDescripcion());
            text.AddToClassList("cs-cond-text");

            var barBg = new VisualElement();
            barBg.AddToClassList("cs-cond-bar-bg");

            var barFill = new VisualElement();
            barFill.AddToClassList("cs-cond-bar-fill");
            if (met)
                barFill.AddToClassList("cs-cond-bar-fill--met");
            barFill.style.width = Length.Percent(progress * 100f);
            barBg.Add(barFill);

            var progText = new Label($"{Mathf.RoundToInt(progress * 100f)}%");
            progText.AddToClassList("cs-cond-prog-txt");

            row.Add(dot);
            row.Add(text);
            row.Add(barBg);
            row.Add(progText);
            return row;
        }

        private void BuildChainsContent(EvolutionState state, List<ChainProgressInfo> chains)
        {
            if (_traitsContent == null) return;

            _traitsContent.Clear();

            if (state == null || chains.Count == 0)
            {
                if (_traitsContentEmptyState != null)
                    _traitsContent.Add(_traitsContentEmptyState);
                return;
            }

            var manager = TraitProgressionManager.Instance;
            foreach (var info in chains)
            {
                int currentNode = manager.GetNodoDisponibleDeCadena(info.chain, state, _selectedCharacter?.DatosClase);
                _traitsContent.Add(CreateChainCard(info, state, currentNode));
            }
        }

        private VisualElement CreateChainCard(ChainProgressInfo info, EvolutionState state, int currentNode)
        {
            var chain = info.chain;

            var card = new VisualElement();
            card.AddToClassList("cs-chain-card");

            var header = new VisualElement();
            header.AddToClassList("cs-chain-header");

            var icon = new VisualElement();
            icon.AddToClassList("cs-chain-icon");
            if (chain.iconoBase != null)
                icon.style.backgroundImage = new StyleBackground(chain.iconoBase);

            var infoColumn = new VisualElement();
            var name = new Label(chain.nombreBase);
            name.AddToClassList("cs-chain-name");

            var sub = new Label($"{info.nodosCompletados}/{info.totalNodos} nodos completados");
            sub.AddToClassList("cs-chain-sublabel");

            infoColumn.Add(name);
            infoColumn.Add(sub);
            header.Add(icon);
            header.Add(infoColumn);
            card.Add(header);

            var nodes = new VisualElement();
            nodes.AddToClassList("cs-chain-nodes");

            for (int i = 0; i < chain.nodos.Count; i++)
            {
                string traitId = chain.GetTraitId(i);
                bool done = state.traitStacks.ContainsKey(traitId);

                if (done || i == currentNode)
                {
                    nodes.Add(CreateVisibleChainNode(chain, i, state, done, isCurrent: i == currentNode));
                }
                else
                {
                    nodes.Add(CreateHiddenChainNode(i + 1));
                }
            }

            card.Add(nodes);

            if (chain.evolucionFinal != null)
                card.Add(CreateEvolutionCard(chain.evolucionFinal, info.puedeDesbloquearEvolucion));

            return card;
        }

        private VisualElement CreateVisibleChainNode(TraitChainDefinition chain, int nodeIndex, EvolutionState state,
            bool done, bool isCurrent)
        {
            var node = new VisualElement();
            node.AddToClassList("cs-chain-node");

            var connector = new VisualElement();
            connector.AddToClassList("cs-node-connector");

            var dot = new VisualElement();
            dot.AddToClassList("cs-node-dot");
            if (done) dot.AddToClassList("cs-node-dot--done");
            else if (isCurrent) dot.AddToClassList("cs-node-dot--current");

            var line = new VisualElement();
            line.AddToClassList("cs-node-line");
            if (done) line.AddToClassList("cs-node-line--done");

            connector.Add(dot);
            connector.Add(line);

            var body = new VisualElement();
            body.AddToClassList("cs-node-body");

            var title = new Label(chain.GetTraitNombre(nodeIndex));
            title.AddToClassList(done ? "cs-node-title-done" : "cs-node-title-current");

            string reqText = done
                ? chain.nodos[nodeIndex].descripcion
                : string.Join(" • ", chain.GetDescripcionesCondiciones(nodeIndex));

            var reqs = new Label(string.IsNullOrWhiteSpace(reqText) ? "Sin requisitos adicionales." : reqText);
            reqs.AddToClassList("cs-node-reqs");

            body.Add(title);
            body.Add(reqs);

            if (chain.nodos[nodeIndex].efectos != null && chain.nodos[nodeIndex].efectos.Count > 0)
            {
                var effects = new VisualElement();
                effects.AddToClassList("cs-node-effects");
                foreach (var effect in chain.nodos[nodeIndex].efectos.Where(e => e != null))
                    effects.Add(CreateNodeEffectChip(DescribeEvolutionEffect(effect)));
                body.Add(effects);
            }

            node.Add(connector);
            node.Add(body);
            return node;
        }

        private VisualElement CreateNodeEffectChip(string text)
        {
            var chip = new Label(text);
            chip.AddToClassList("cs-node-effect");
            return chip;
        }

        private VisualElement CreateHiddenChainNode(int index)
        {
            var hidden = new VisualElement();
            hidden.AddToClassList("cs-chain-hidden");

            var line = new VisualElement();
            line.AddToClassList("cs-chain-hidden-line");

            var label = new Label($"Nodo {index} oculto hasta progresar en la cadena.");
            label.AddToClassList("cs-chain-hidden-label");

            hidden.Add(line);
            hidden.Add(label);
            return hidden;
        }

        private VisualElement CreateEvolutionCard(ClassEvolutionDefinition evolution, bool unlocked)
        {
            var card = new VisualElement();
            card.AddToClassList("cs-chain-evo");
            if (!unlocked)
                card.AddToClassList("cs-chain-evo--locked");

            var icon = new VisualElement();
            icon.AddToClassList("cs-evo-icon");
            if (!unlocked)
                icon.AddToClassList("cs-evo-icon--locked");
            if (evolution.icono != null)
                icon.style.backgroundImage = new StyleBackground(evolution.icono);

            var info = new VisualElement();
            var name = new Label(evolution.nombreMostrar ?? evolution.id);
            name.AddToClassList("cs-evo-name");
            if (!unlocked)
                name.AddToClassList("cs-evo-name--locked");

            var sub = new Label(unlocked
                ? "Evolución final disponible para esta cadena."
                : "Completa la cadena y sus requisitos finales para desbloquearla.");
            sub.AddToClassList("cs-evo-sub");

            info.Add(name);
            info.Add(sub);
            card.Add(icon);
            card.Add(info);
            return card;
        }

        private float CalculateProjectedAttack(EntityController character)
        {
            var data = character.DatosClase;
            if (data == null) return character.PuntosDeAtaque_Entidad;
            return data.ataqueBase + Mathf.Max(0, character.Nivel_Entidad - 1) * data.escalado.ataquePorNivel;
        }

        private float CalculateProjectedDefense(EntityController character)
        {
            var data = character.DatosClase;
            if (data == null) return character.PuntosDeDefensa_Entidad;
            return data.defensaBase + Mathf.Max(0, character.Nivel_Entidad - 1) * data.escalado.defensaPorNivel;
        }

        private float CalculateProjectedSpeed(EntityController character)
        {
            var data = character.DatosClase;
            if (data == null) return character.Velocidad;
            return data.velocidadBase + Mathf.Max(0, character.Nivel_Entidad - 1) * data.escalado.velocidadPorNivel;
        }

        private int GetTraitStacks(EvolutionState state, string traitId)
        {
            return state != null && !string.IsNullOrEmpty(traitId) && state.traitStacks.TryGetValue(traitId, out int stacks)
                ? stacks
                : 0;
        }

        private string PrettyEnum(Enum value)
        {
            return value.ToString().Replace("_", " ").Replace("Unico", "Único");
        }

        private string DescribeEvolutionEffect(EvolutionEffect effect)
        {
            return effect.tipo switch
            {
                EvolutionEffectType.AddStatFlat => $"+{effect.valor:0.#} {PrettyEnum(effect.stat)}",
                EvolutionEffectType.AddStatPercent => $"+{effect.valor:0.#}% {PrettyEnum(effect.stat)}",
                EvolutionEffectType.AddAbility when effect.habilidad != null => effect.habilidad.nombreHabilidad,
                EvolutionEffectType.AddPassive when effect.pasiva != null => effect.pasiva.nombrePasiva,
                EvolutionEffectType.AddElement => PrettyEnum(effect.elemento),
                EvolutionEffectType.AgregarModulo when effect.moduloSO != null => effect.moduloSO.name,
                _ => PrettyEnum(effect.tipo)
            };
        }
    }
}