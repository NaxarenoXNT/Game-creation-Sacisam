using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI
{
    /// <summary>
    /// Sistema de UI reactiva con bindings automaticos.
    /// Los elementos de UI se actualizan automaticamente cuando cambian los datos.
    /// </summary>
    public class UIReactiva : MonoBehaviour
    {
        private static UIReactiva _instance;
        public static UIReactiva Instance => _instance;
        
        private Dictionary<string, IBindableUI> elementosRegistrados = new Dictionary<string, IBindableUI>();
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        
        /// <summary>
        /// Registra un elemento de UI para actualizaciones automaticas.
        /// </summary>
        public void Registrar(string id, IBindableUI elemento)
        {
            elementosRegistrados[id] = elemento;
        }
        
        /// <summary>
        /// Desregistra un elemento de UI.
        /// </summary>
        public void Desregistrar(string id)
        {
            elementosRegistrados.Remove(id);
        }
        
        /// <summary>
        /// Actualiza un elemento especifico.
        /// </summary>
        public void Actualizar(string id)
        {
            if (elementosRegistrados.TryGetValue(id, out var elemento))
            {
                elemento.Refrescar();
            }
        }
        
        /// <summary>
        /// Actualiza todos los elementos registrados.
        /// </summary>
        public void ActualizarTodo()
        {
            foreach (var elemento in elementosRegistrados.Values)
            {
                elemento.Refrescar();
            }
        }
    }
    
    /// <summary>
    /// Interfaz para elementos de UI que pueden ser actualizados.
    /// </summary>
    public interface IBindableUI
    {
        void Refrescar();
    }
    
    // =================================================================
    // =================== PROPIEDADES OBSERVABLES =====================
    // =================================================================
    
    /// <summary>
    /// Propiedad observable que notifica cambios automaticamente.
    /// </summary>
    public class Observable<T>
    {
        private T _valor;
        public event Action<T> OnCambiado;
        
        public T Valor
        {
            get => _valor;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_valor, value))
                {
                    _valor = value;
                    OnCambiado?.Invoke(_valor);
                }
            }
        }
        
        public Observable(T valorInicial = default)
        {
            _valor = valorInicial;
        }
        
        public static implicit operator T(Observable<T> obs) => obs.Valor;
    }
    
    /// <summary>
    /// Modelo de datos observable para una entidad de combate.
    /// </summary>
    [Serializable]
    public class EntidadUIModel
    {
        public Observable<string> Nombre = new Observable<string>("");
        public Observable<int> VidaActual = new Observable<int>(0);
        public Observable<int> VidaMaxima = new Observable<int>(100);
        public Observable<int> ManaActual = new Observable<int>(0);
        public Observable<int> ManaMaximo = new Observable<int>(50);
        public Observable<int> Nivel = new Observable<int>(1);
        public Observable<float> XPProgreso = new Observable<float>(0);
        public Observable<bool> EstaVivo = new Observable<bool>(true);
        public Observable<bool> EsSuTurno = new Observable<bool>(false);
        
        public float PorcentajeVida => VidaMaxima.Valor > 0 ? (float)VidaActual.Valor / VidaMaxima.Valor : 0;
        public float PorcentajeMana => ManaMaximo.Valor > 0 ? (float)ManaActual.Valor / ManaMaximo.Valor : 0;
    }
    
    // =================================================================
    // =================== COMPONENTES DE UI ===========================
    // =================================================================
    
    /// <summary>
    /// Barra de vida/mana reactiva.
    /// </summary>
    public class BarraReactiva : MonoBehaviour, IBindableUI
    {
        [Header("Referencias")]
        [SerializeField] private Image barraFill;
        [SerializeField] private TextMeshProUGUI textoValor;
        [SerializeField] private Image fondoBarra;
        
        [Header("Configuracion")]
        [SerializeField] private string idRegistro;
        [SerializeField] private float velocidadAnimacion = 5f;
        [SerializeField] private Gradient colorGradient;
        
        private Observable<int> valorActual;
        private Observable<int> valorMaximo;
        private float valorObjetivo;
        
        private void Start()
        {
            if (!string.IsNullOrEmpty(idRegistro) && UIReactiva.Instance != null)
            {
                UIReactiva.Instance.Registrar(idRegistro, this);
            }
        }
        
        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(idRegistro) && UIReactiva.Instance != null)
            {
                UIReactiva.Instance.Desregistrar(idRegistro);
            }
            
            // Desuscribirse de observables
            if (valorActual != null) valorActual.OnCambiado -= OnValorCambiado;
            if (valorMaximo != null) valorMaximo.OnCambiado -= OnMaximoCambiado;
        }
        
        /// <summary>
        /// Vincula la barra a observables.
        /// </summary>
        public void Vincular(Observable<int> actual, Observable<int> maximo)
        {
            // Desuscribir de anteriores
            if (valorActual != null) valorActual.OnCambiado -= OnValorCambiado;
            if (valorMaximo != null) valorMaximo.OnCambiado -= OnMaximoCambiado;
            
            valorActual = actual;
            valorMaximo = maximo;
            
            // Suscribir a nuevos
            valorActual.OnCambiado += OnValorCambiado;
            valorMaximo.OnCambiado += OnMaximoCambiado;
            
            // Actualizar inmediatamente
            Refrescar();
        }
        
        private void OnValorCambiado(int nuevoValor) => Refrescar();
        private void OnMaximoCambiado(int nuevoMaximo) => Refrescar();
        
        public void Refrescar()
        {
            if (valorActual == null || valorMaximo == null) return;
            
            valorObjetivo = valorMaximo.Valor > 0 ? (float)valorActual.Valor / valorMaximo.Valor : 0;
            
            if (textoValor != null)
            {
                textoValor.text = valorActual.Valor + " / " + valorMaximo.Valor;
            }
        }
        
        private void Update()
        {
            if (barraFill == null) return;
            
            // Animar hacia el valor objetivo
            barraFill.fillAmount = Mathf.Lerp(barraFill.fillAmount, valorObjetivo, Time.deltaTime * velocidadAnimacion);
            
            // Aplicar color del gradiente
            if (colorGradient != null)
            {
                barraFill.color = colorGradient.Evaluate(barraFill.fillAmount);
            }
        }
    }
    
    /// <summary>
    /// Panel de entidad reactivo (vida, mana, nombre).
    /// </summary>
    public class PanelEntidad : MonoBehaviour, IBindableUI
    {
        [Header("Referencias")]
        [SerializeField] private TextMeshProUGUI textoNombre;
        [SerializeField] private TextMeshProUGUI textoNivel;
        [SerializeField] private BarraReactiva barraVida;
        [SerializeField] private BarraReactiva barraMana;
        [SerializeField] private GameObject indicadorTurno;
        
        [Header("Configuracion")]
        [SerializeField] private string idRegistro;
        
        private EntidadUIModel modelo;
        
        private void Start()
        {
            if (!string.IsNullOrEmpty(idRegistro) && UIReactiva.Instance != null)
            {
                UIReactiva.Instance.Registrar(idRegistro, this);
            }
        }
        
        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(idRegistro) && UIReactiva.Instance != null)
            {
                UIReactiva.Instance.Desregistrar(idRegistro);
            }
            
            DesuscribirModelo();
        }
        
        /// <summary>
        /// Vincula el panel a un modelo de entidad.
        /// </summary>
        public void Vincular(EntidadUIModel modelo)
        {
            DesuscribirModelo();
            
            this.modelo = modelo;
            
            // Vincular barras
            if (barraVida != null)
                barraVida.Vincular(modelo.VidaActual, modelo.VidaMaxima);
            
            if (barraMana != null)
                barraMana.Vincular(modelo.ManaActual, modelo.ManaMaximo);
            
            // Suscribir a cambios
            modelo.Nombre.OnCambiado += ActualizarNombre;
            modelo.Nivel.OnCambiado += ActualizarNivel;
            modelo.EsSuTurno.OnCambiado += ActualizarIndicadorTurno;
            modelo.EstaVivo.OnCambiado += ActualizarEstadoVivo;
            
            Refrescar();
        }
        
        private void DesuscribirModelo()
        {
            if (modelo == null) return;
            
            modelo.Nombre.OnCambiado -= ActualizarNombre;
            modelo.Nivel.OnCambiado -= ActualizarNivel;
            modelo.EsSuTurno.OnCambiado -= ActualizarIndicadorTurno;
            modelo.EstaVivo.OnCambiado -= ActualizarEstadoVivo;
        }
        
        private void ActualizarNombre(string nombre)
        {
            if (textoNombre != null) textoNombre.text = nombre;
        }
        
        private void ActualizarNivel(int nivel)
        {
            if (textoNivel != null) textoNivel.text = "Nv. " + nivel;
        }
        
        private void ActualizarIndicadorTurno(bool esSuTurno)
        {
            if (indicadorTurno != null) indicadorTurno.SetActive(esSuTurno);
        }
        
        private void ActualizarEstadoVivo(bool vivo)
        {
            // Opcional: cambiar apariencia si esta muerto
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = vivo ? 1f : 0.5f;
            }
        }
        
        public void Refrescar()
        {
            if (modelo == null) return;
            
            ActualizarNombre(modelo.Nombre.Valor);
            ActualizarNivel(modelo.Nivel.Valor);
            ActualizarIndicadorTurno(modelo.EsSuTurno.Valor);
            ActualizarEstadoVivo(modelo.EstaVivo.Valor);
        }
    }
    
    /// <summary>
    /// Texto reactivo que se actualiza automaticamente.
    /// </summary>
    public class TextoReactivo : MonoBehaviour, IBindableUI
    {
        [Header("Referencias")]
        [SerializeField] private TextMeshProUGUI texto;
        
        [Header("Configuracion")]
        [SerializeField] private string idRegistro;
        [SerializeField] private string formato = "{0}";
        
        private Func<string> obtenerValor;
        
        private void Start()
        {
            if (!string.IsNullOrEmpty(idRegistro) && UIReactiva.Instance != null)
            {
                UIReactiva.Instance.Registrar(idRegistro, this);
            }
        }
        
        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(idRegistro) && UIReactiva.Instance != null)
            {
                UIReactiva.Instance.Desregistrar(idRegistro);
            }
        }
        
        /// <summary>
        /// Vincula el texto a una funcion que obtiene el valor.
        /// </summary>
        public void Vincular(Func<string> obtenerValor)
        {
            this.obtenerValor = obtenerValor;
            Refrescar();
        }
        
        /// <summary>
        /// Vincula el texto a un observable.
        /// </summary>
        public void Vincular<T>(Observable<T> observable)
        {
            observable.OnCambiado += _ => Refrescar();
            obtenerValor = () => observable.Valor.ToString();
            Refrescar();
        }
        
        public void Refrescar()
        {
            if (texto == null || obtenerValor == null) return;
            
            texto.text = string.Format(formato, obtenerValor());
        }
    }
    
    /// <summary>
    /// Boton de habilidad con cooldown visual.
    /// </summary>
    public class BotonHabilidad : MonoBehaviour, IBindableUI
    {
        [Header("Referencias")]
        [SerializeField] private Button boton;
        [SerializeField] private Image iconoHabilidad;
        [SerializeField] private Image overlayCooldown;
        [SerializeField] private TextMeshProUGUI textoCooldown;
        [SerializeField] private TextMeshProUGUI textoNombre;
        
        [Header("Configuracion")]
        [SerializeField] private string idRegistro;
        
        private HabilidadData habilidad;
        private Habilidades.GestorCooldowns gestorCooldowns;
        private Action<HabilidadData> onClickCallback;
        
        private void Start()
        {
            if (!string.IsNullOrEmpty(idRegistro) && UIReactiva.Instance != null)
            {
                UIReactiva.Instance.Registrar(idRegistro, this);
            }
            
            if (boton != null)
            {
                boton.onClick.AddListener(OnClick);
            }
        }
        
        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(idRegistro) && UIReactiva.Instance != null)
            {
                UIReactiva.Instance.Desregistrar(idRegistro);
            }
        }
        
        /// <summary>
        /// Configura el boton con una habilidad.
        /// </summary>
        public void Configurar(HabilidadData habilidad, Habilidades.GestorCooldowns cooldowns, Action<HabilidadData> onClick)
        {
            this.habilidad = habilidad;
            this.gestorCooldowns = cooldowns;
            this.onClickCallback = onClick;
            
            if (textoNombre != null && habilidad != null)
            {
                textoNombre.text = habilidad.nombreHabilidad;
            }
            
            Refrescar();
        }
        
        private void OnClick()
        {
            if (habilidad != null && gestorCooldowns != null && gestorCooldowns.EstaDisponible(habilidad))
            {
                onClickCallback?.Invoke(habilidad);
            }
        }
        
        public void Refrescar()
        {
            if (habilidad == null || gestorCooldowns == null) return;
            
            bool disponible = gestorCooldowns.EstaDisponible(habilidad);
            int turnosRestantes = gestorCooldowns.ObtenerCooldown(habilidad);
            
            if (boton != null)
            {
                boton.interactable = disponible;
            }
            
            if (overlayCooldown != null)
            {
                overlayCooldown.gameObject.SetActive(!disponible);
                if (!disponible && habilidad.cooldownTurnos > 0)
                {
                    overlayCooldown.fillAmount = (float)turnosRestantes / habilidad.cooldownTurnos;
                }
            }
            
            if (textoCooldown != null)
            {
                textoCooldown.gameObject.SetActive(!disponible);
                textoCooldown.text = turnosRestantes.ToString();
            }
        }
    }
}
