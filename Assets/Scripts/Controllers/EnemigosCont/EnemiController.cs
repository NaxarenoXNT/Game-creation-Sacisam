using UnityEngine;
using Padres;
using Flasgs;
using Interfaces;
using Habilidades;
using System.Collections.Generic;

/// <summary>
/// Controlador de enemigo que conecta la lógica con Unity
/// Versión simplificada enfocada en enemigos
/// </summary>
public class EnemyController : MonoBehaviour, IEntidadCombate, IEntidadActuable
{
    [Header("Configuración")]
    [SerializeField] private EnemigoData datosEnemigo;

    [Header("Referencias")]
    [SerializeField] private EntityStats entityStats;
    
    // Instancia de la entidad 
    
    private Enemigos enemigoLogica;
    
    // Propiedades públicas
    public Enemigos EnemigoLogica => enemigoLogica;
    public EntityStats EntityStats => entityStats;
    
    private void Awake()
    {
        // Obtener o crear EntityStats
        if (entityStats == null)
        {
            entityStats = GetComponent<EntityStats>();
            if (entityStats == null)
            {
                entityStats = gameObject.AddComponent<EntityStats>();
                Debug.LogWarning($"{gameObject.name}: EntityStats no estaba asignado, se creó automáticamente");
            }
        }
        
        // Inicializar con datos si están asignados
        if (datosEnemigo != null)
        {
            Inicializar(datosEnemigo);
        }
    }
    
    public void Inicializar(EnemigoData datos)
    {
        datosEnemigo = datos;
        
        // 1. Crear la instancia lógica correcta según el tipo
        enemigoLogica = datos.CrearInstancia();
        
        // 2. Vincular EntityStats con el Enemigo (BIDIRECCIONAL)
        entityStats.VincularEntidad(enemigoLogica);
        
        // 3. Suscribirse a eventos del enemigo
        enemigoLogica.OnDañoRecibido += ManejarDañoRecibido;
        enemigoLogica.OnMuerte += ManejarMuerte;
        enemigoLogica.OnNivelSubido += ManejarSubidaNivel;
        
        // 4. Aplicar elementos iniciales si tiene
        AplicarElementosIniciales();
        
        Debug.Log($"👹 Enemigo inicializado: {enemigoLogica.Nombre_Entidad} [Nv.{enemigoLogica.Nivel_Entidad}]");
        Debug.Log($"   HP: {enemigoLogica.VidaActual_Entidad}/{enemigoLogica.Vida_Entidad} | ATK: {enemigoLogica.PuntosDeAtaque_Entidad} | DEF: {enemigoLogica.PuntosDeDefensa_Entidad} | VEL: {enemigoLogica.Velocidad}");
        
        // Mostrar elementos si tiene
        if (entityStats != null && entityStats.activeStatuses.Count > 0)
        {
            Debug.Log($"   🔥 Elementos activos: {entityStats.activeStatuses.Count}");
            foreach (var status in entityStats.activeStatuses)
            {
                Debug.Log($"      • {status.definition.elementName} [Nv.{status.level}]");
            }
        }
    }

    private void AplicarElementosIniciales()
    {
        if (datosEnemigo != null && datosEnemigo.atributos != ElementAttribute.None)
        {
            foreach (ElementAttribute flag in System.Enum.GetValues(typeof(ElementAttribute)))
            {
                if (flag != ElementAttribute.None && datosEnemigo.atributos.HasFlag(flag))
                {
                    entityStats.AplicarElemento(flag);
                }
            }
        }
    }
    
    public void AplicarEstado(StatusFlag status, int duracion)
    {
        if (enemigoLogica == null)
        {
            Debug.LogWarning("No se puede aplicar estado: enemigo no válido");
            return;
        }
        
        enemigoLogica.AplicarEstado(status, duracion);
    }



    // =================================================================
    // ============== IMPLEMENTACIÓN DE IENTIDADACTUABLE ===============
    // =================================================================

    public (IHabilidadesCommad comando, Interfaces.IEntidadCombate objetivo) ObtenerAccionElegida(
        List<Interfaces.IEntidadCombate> aliados, 
        List<Interfaces.IEntidadCombate> enemigos
    )
    {
        // El método DecidirObjetivo en Goblin.cs recibe List<IEntidadCombate>
        Interfaces.IEntidadCombate objetivo = EnemigoLogica.DecidirObjetivo(enemigos); 
        
        // Decisión de Habilidad (Temporal: Usaremos la habilidad por defecto del ScriptableObject)
        // Necesitas una propiedad 'HabilidadPorDefecto' en tu EnemigoData para que esto funcione.
        HabilidadData habilidad = datosEnemigo.HabilidadPorDefecto; 
        
        // Si el enemigo no tiene objetivo o la habilidad no es viable, devuelve null.
        if (objetivo == null || habilidad == null || !habilidad.EsViable(this, objetivo, aliados, enemigos))
        {
            return (null, null); 
        }

        // Devolver la acción y el objetivo (ambos a través de interfaces/clases que implementan interfaces)
        return (habilidad, objetivo);
    }





    // =================================================================
    // =========== IMPLEMENTACIÓN DE IENTIDADCOMBATE (Fachada) =========
    // =================================================================

    // Redirección de propiedades (Getters)
    public string Nombre_Entidad => enemigoLogica.Nombre_Entidad;
    public int Nivel_Entidad => enemigoLogica.Nivel_Entidad;
    public int Vida_Entidad => enemigoLogica.Vida_Entidad;
    public int VidaActual_Entidad => enemigoLogica.VidaActual_Entidad;
    public int PuntosDeAtaque_Entidad => enemigoLogica.PuntosDeAtaque_Entidad;
    public float PuntosDeDefensa_Entidad => enemigoLogica.PuntosDeDefensa_Entidad;
    public int Velocidad => enemigoLogica.Velocidad;
    public bool EsDerrotado => enemigoLogica.EsDerrotado;
    public bool EstaMuerto => enemigoLogica.EstaMuerto;
    public TipoEntidades TipoEntidad => enemigoLogica.TipoEntidad;
    public ElementAttribute AtributosEntidad => enemigoLogica.AtributosEntidad;

    public bool EstaVivo() => enemigoLogica.EstaVivo();
    public bool PuedeActuar() => enemigoLogica.PuedeActuar();
    
    public bool EsTipoEntidad(TipoEntidades tipo) => enemigoLogica.EsTipoEntidad(tipo);
    public bool UsaEstiloDeCombate(CombatStyle estilo) => enemigoLogica.UsaEstiloDeCombate(estilo);
    public int CalcularDañoContra(IEntidadCombate objetivo) => enemigoLogica.CalcularDañoContra(objetivo);
    public void RecibirDaño(int dañoBruto, ElementAttribute tipo)
    {
        if (enemigoLogica == null)
        {
            Debug.LogWarning("No se puede recibir daño: enemigo no válido");
            return;
        }
        
        enemigoLogica.RecibirDaño(dañoBruto, tipo);
    }
    
    
    
    // ========== MANEJADORES DE EVENTOS ==========
    
    private void ManejarDañoRecibido(int cantidad)
    {
        Debug.Log($"💢 {enemigoLogica.Nombre_Entidad} recibió {cantidad} de daño. Vida: {enemigoLogica.VidaActual_Entidad}/{enemigoLogica.Vida_Entidad}");
        // Aquí irían animaciones, efectos visuales, etc.
        // Por ahora solo cambiar color temporalmente
        StartCoroutine(FlashDamage());
    }
    
    private void ManejarMuerte()
    {
        Debug.Log($"☠️ {enemigoLogica.Nombre_Entidad} ha muerto!");
        // Aquí irían animaciones de muerte, drops, etc.
        
        StartCoroutine(DestruirDespuesDeMorir());
    }
    
    private void ManejarSubidaNivel(int nuevoNivel)
    {
        Debug.Log($"⬆️ {enemigoLogica.Nombre_Entidad} subió al nivel {nuevoNivel}!");
        Debug.Log($"   Nueva vida: {enemigoLogica.Vida_Entidad} | Ataque: {enemigoLogica.PuntosDeAtaque_Entidad} | Defensa: {enemigoLogica.PuntosDeDefensa_Entidad}");
        // Aquí irían efectos visuales de level up
    }
    
    private System.Collections.IEnumerator FlashDamage()
    {
        // Efecto visual temporal - cambiar a rojo
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Color original = renderer.material.color;
            renderer.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            renderer.material.color = original;
        }
    }
    
    private System.Collections.IEnumerator DestruirDespuesDeMorir()
    {
        // Efecto visual de muerte
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.black;
        }
        
        yield return new WaitForSeconds(1f);
        
        Debug.Log($"🗑️ Destruyendo GameObject: {gameObject.name}");
        Destroy(gameObject);
    }


    // ========== LIMPIEZA ==========

    private void OnDestroy()
    {
        if (enemigoLogica != null)
        {
            enemigoLogica.OnDañoRecibido -= ManejarDañoRecibido;
            enemigoLogica.OnMuerte -= ManejarMuerte;
            enemigoLogica.OnNivelSubido -= ManejarSubidaNivel;
        }
    }
    
    
}