using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Console.Core;
using GameInput;

namespace Console.UI
{
    /// <summary>
    /// ConsoleUI — implementada sobre UI Toolkit.
    /// Requiere un UIDocument en el mismo GameObject con Console.uxml asignado.
    /// Toggle: BackQuote (`) por defecto, configurable desde el Inspector.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ConsoleUI : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int maxOutputLines = 100;
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;

        private CommandRegistry _registry;

        // Elementos UI Toolkit
        private VisualElement _root;
        private VisualElement _outputContainer;
        private ScrollView _scrollView;
        private TextField _inputField;

        private bool _isOpen;
        private InputContext _previousContext;
        private readonly List<string> _history = new List<string>();
        private int _historyIndex = -1;

        // ──────────────────────────────────────────────
        //  Inicialización
        // ──────────────────────────────────────────────

        private void Awake()
        {
            var doc = GetComponent<UIDocument>();
            _root            = doc.rootVisualElement.Q<VisualElement>("console-root");
            _scrollView      = doc.rootVisualElement.Q<ScrollView>("console-scroll");
            _outputContainer = doc.rootVisualElement.Q<VisualElement>("console-output");
            _inputField      = doc.rootVisualElement.Q<TextField>("console-input");

            // Evitar que el TextField capture las teclas de navegación como texto
            _inputField.RegisterCallback<KeyDownEvent>(OnInputKeyDown, TrickleDown.TrickleDown);

            SetVisible(false);
        }

        public void Initialize(CommandRegistry registry)
        {
            _registry = registry;
        }

        // ──────────────────────────────────────────────
        //  Toggle por teclado
        // ──────────────────────────────────────────────

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                SetVisible(!_isOpen);
        }

        private void SetVisible(bool visible)
        {
            _isOpen = visible;
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            var inputManager = GameInputManager.Instance;
            if (visible)
            {
                // Guardar contexto actual y bloquear input del juego
                if (inputManager != null)
                {
                    _previousContext = inputManager.CurrentContext;
                    inputManager.SetContext(InputContext.Menu);
                }

                _inputField.value = string.Empty;
                _inputField.Focus();
                _inputField.schedule.Execute(() => _inputField.Focus()).ExecuteLater(1);
            }
            else
            {
                // Restaurar el contexto anterior al cerrar
                inputManager?.SetContext(_previousContext);
            }
        }

        // ──────────────────────────────────────────────
        //  Manejo de teclado dentro del TextField
        // ──────────────────────────────────────────────

        private void OnInputKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    SubmitInput();
                    evt.StopPropagation();
                    break;

                case KeyCode.UpArrow:
                    NavigateHistory(-1);
                    evt.StopPropagation();
                    break;

                case KeyCode.DownArrow:
                    NavigateHistory(1);
                    evt.StopPropagation();
                    break;
            }
        }

        // ──────────────────────────────────────────────
        //  Submit
        // ──────────────────────────────────────────────

        private void SubmitInput()
        {
            string input = _inputField.value;
            if (string.IsNullOrWhiteSpace(input))
                return;

            _history.Add(input);
            _historyIndex = _history.Count;

            AppendLine($"> {input}", "console-line--echo");

            CommandResult result = _registry.ExecuteRaw(input);
            string lineClass = result.Success ? "console-line--ok" : "console-line--error";
            string prefix    = result.Success ? "" : "[Error] ";
            AppendLine($"{prefix}{result.Message}", lineClass);

            _inputField.value = string.Empty;
            _inputField.Focus();
        }

        // ──────────────────────────────────────────────
        //  Historial de comandos
        // ──────────────────────────────────────────────

        private void NavigateHistory(int direction)
        {
            if (_history.Count == 0)
                return;

            _historyIndex = Mathf.Clamp(_historyIndex + direction, 0, _history.Count);

            _inputField.value = _historyIndex < _history.Count
                ? _history[_historyIndex]
                : string.Empty;

            // Mover cursor al final
            _inputField.SelectRange(_inputField.value.Length, _inputField.value.Length);
        }

        // ──────────────────────────────────────────────
        //  Output
        // ──────────────────────────────────────────────

        private void AppendLine(string text, string extraClass = null)
        {
            // Limitar líneas máximas
            while (_outputContainer.childCount >= maxOutputLines)
                _outputContainer.RemoveAt(0);

            var label = new Label(text);
            label.AddToClassList("console-line");
            if (!string.IsNullOrEmpty(extraClass))
                label.AddToClassList(extraClass);

            _outputContainer.Add(label);

            // Scroll al fondo en el próximo frame (el layout debe recalcularse primero)
            _scrollView.schedule.Execute(() =>
                _scrollView.scrollOffset = new Vector2(0f, float.MaxValue)
            ).ExecuteLater(1);
        }
    }
}
