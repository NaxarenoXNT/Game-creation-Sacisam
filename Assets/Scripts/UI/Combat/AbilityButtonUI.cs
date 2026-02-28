using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using Habilidades;
using Flags;

namespace UI.Combat
{
    /// <summary>
    /// Botón de habilidad para el panel dinámico de habilidades.
    /// Muestra: nombre, elemento, daño estimado, tipo objetivo, efecto.
    /// </summary>
    public class AbilityButtonUI : MonoBehaviour
    {
        [Header("Referencias UI")]
        [SerializeField] private Button boton;
        [SerializeField] private TMP_Text textoNombre;
        [SerializeField] private TMP_Text textoDano;
        [SerializeField] private TMP_Text textoTipoObjetivo;
        [SerializeField] private TMP_Text textoEfecto;
        [SerializeField] private TMP_Text textoCosto;
        [SerializeField] private Image iconoElemento;
        [SerializeField] private Image iconoHabilidad;
        [SerializeField] private Image fondoBoton;
        
        [Header("Colores por Estado")]
        [SerializeField] private Color colorDisponible = Color.white;
        [SerializeField] private Color colorEnCooldown = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        [SerializeField] private Color colorSinRecurso = new Color(0.3f, 0.3f, 0.6f, 0.7f);
        
        [Header("Iconos de Elementos")]
        [SerializeField] private ElementIconMapping[] iconosElementos;
        
        // Estado interno
        private HabilidadData habilidadData;
        private Action<HabilidadData> onClickCallback;
        private bool estaDisponible = true;
        
        public HabilidadData Habilidad => habilidadData;
        
        private void Awake()
        {
            if (boton == null)
                boton = GetComponent<Button>();
                
            boton?.onClick.AddListener(OnClick);
        }
        
        private void OnDestroy()
        {
            boton?.onClick.RemoveListener(OnClick);
        }
        
        /// <summary>
        /// Configura el botón con los datos de la habilidad.
        /// </summary>
        /// <param name="habilidad">Datos de la habilidad</param>
        /// <param name="danoCalculado">Daño calculado según el personaje (puede ser 0 si no es de daño)</param>
        /// <param name="disponible">Si la habilidad está disponible (no en cooldown, tiene recursos)</param>
        /// <param name="onClick">Callback al hacer click</param>
        public void Configurar(HabilidadData habilidad, int danoCalculado, bool disponible, Action<HabilidadData> onClick)
        {
            habilidadData = habilidad;
            onClickCallback = onClick;
            estaDisponible = disponible;
            
            // Nombre
            if (textoNombre != null)
                textoNombre.text = habilidad.nombreHabilidad;
            
            // Icono de la habilidad
            if (iconoHabilidad != null && habilidad.icono != null)
            {
                iconoHabilidad.sprite = habilidad.icono;
                iconoHabilidad.gameObject.SetActive(true);
            }
            else if (iconoHabilidad != null)
            {
                iconoHabilidad.gameObject.SetActive(false);
            }
            
            // Daño estimado (solo si es habilidad de daño)
            if (textoDano != null)
            {
                if (danoCalculado > 0)
                    textoDano.text = $"Daño: {danoCalculado}";
                else
                    textoDano.text = ObtenerDescripcionCategoria(habilidad.categoria);
            }
            
            // Tipo de objetivo
            if (textoTipoObjetivo != null)
            {
                textoTipoObjetivo.text = ObtenerDescripcionObjetivo(habilidad.tipoObjetivo);
            }
            
            // Costo de recursos
            if (textoCosto != null)
            {
                textoCosto.text = habilidad.ObtenerDescripcionCostos();
            }
            
            // Efecto/descripción
            if (textoEfecto != null)
            {
                textoEfecto.text = ObtenerDescripcionEfectos(habilidad);
            }
            
            // Icono de elemento (obtener del primer DamageEffect si existe)
            var elementoHabilidad = ObtenerElementoPrincipal(habilidad);
            ActualizarIconoElemento(elementoHabilidad);
            
            // Estado visual
            ActualizarEstadoVisual(disponible);
        }
        
        private string ObtenerDescripcionCategoria(CategoriaHabilidad categoria)
        {
            return categoria switch
            {
                CategoriaHabilidad.Ataque => "Ataque",
                CategoriaHabilidad.Curacion => "Curación",
                CategoriaHabilidad.Buff => "Buff",
                CategoriaHabilidad.Debuff => "Debuff",
                CategoriaHabilidad.Control => "Control",
                CategoriaHabilidad.Utilidad => "Utilidad",
                _ => "-"
            };
        }
        
        private string ObtenerDescripcionObjetivo(TargetType tipo)
        {
            return tipo switch
            {
                TargetType.EnemigoUnico => "Single",
                TargetType.EnemigoTodos => "Multi (Enemigos)",
                TargetType.AliadoUnico => "Aliado",
                TargetType.AliadoTodos => "Multi (Aliados)",
                TargetType.Self => "Self",
                _ => "?"
            };
        }
        
        private string ObtenerDescripcionEfectos(HabilidadData habilidad)
        {
            if (habilidad.efectos == null || habilidad.efectos.Count == 0)
                return "-";
            
            // Mostrar el tipo del primer efecto como resumen
            var primerEfecto = habilidad.efectos[0];
            if (primerEfecto == null) return "-";
            
            string nombreTipo = primerEfecto.GetType().Name;
            // Limpiar el nombre (quitar "Effect" del final)
            if (nombreTipo.EndsWith("Effect"))
                nombreTipo = nombreTipo.Substring(0, nombreTipo.Length - 6);
            
            if (habilidad.efectos.Count > 1)
                return $"{nombreTipo} +{habilidad.efectos.Count - 1}";
            
            return nombreTipo;
        }
        
        private ElementAttribute ObtenerElementoPrincipal(HabilidadData habilidad)
        {
            if (habilidad.efectos == null) return ElementAttribute.None;
            
            // Buscar el primer DamageEffect y obtener su elemento
            var damageEffect = habilidad.efectos.FirstOrDefault(e => e is DamageEffect) as DamageEffect;
            return damageEffect?.tipoDano ?? ElementAttribute.None;
        }
        
        /// <summary>
        /// Actualiza solo el estado de disponibilidad (para refrescar cooldowns).
        /// </summary>
        public void ActualizarDisponibilidad(bool disponible, bool enCooldown = false, bool sinRecurso = false)
        {
            estaDisponible = disponible;
            
            if (boton != null)
                boton.interactable = disponible;
            
            if (fondoBoton != null)
            {
                if (!disponible)
                {
                    fondoBoton.color = enCooldown ? colorEnCooldown : 
                                       sinRecurso ? colorSinRecurso : colorEnCooldown;
                }
                else
                {
                    fondoBoton.color = colorDisponible;
                }
            }
        }
        
        private void ActualizarEstadoVisual(bool disponible)
        {
            if (boton != null)
                boton.interactable = disponible;
            
            if (fondoBoton != null)
                fondoBoton.color = disponible ? colorDisponible : colorEnCooldown;
        }
        
        private void ActualizarIconoElemento(ElementAttribute elemento)
        {
            if (iconoElemento == null) return;
            
            Sprite icono = ObtenerIconoElemento(elemento);
            if (icono != null)
            {
                iconoElemento.sprite = icono;
                iconoElemento.gameObject.SetActive(true);
            }
            else
            {
                iconoElemento.gameObject.SetActive(false);
            }
        }
        
        private Sprite ObtenerIconoElemento(ElementAttribute elemento)
        {
            if (iconosElementos == null) return null;
            
            foreach (var mapping in iconosElementos)
            {
                if (mapping.elemento == elemento)
                    return mapping.icono;
            }
            return null;
        }
        
        private void OnClick()
        {
            if (estaDisponible && habilidadData != null)
            {
                onClickCallback?.Invoke(habilidadData);
            }
        }
    }
    
    /// <summary>
    /// Mapeo de elemento a icono.
    /// </summary>
    [System.Serializable]
    public struct ElementIconMapping
    {
        public ElementAttribute elemento;
        public Sprite icono;
    }
}
