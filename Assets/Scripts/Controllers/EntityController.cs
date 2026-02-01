using UnityEngine;
using Padres;
using Flags;
using Interfaces;
using Habilidades;
using Combate;
using System.Collections.Generic;

/// <summary>
/// Controlador de entidad que conecta la logica con Unity
/// Funciona tanto para jugadores como para enemigos
/// </summary>
public class EntityController : MonoBehaviour, IEntidadCombate, IJugadorProgresion, IEntidadActuable, IGestorHabilidades
{
    [Header("Configuracion")]
    [SerializeField] private ClaseData datosClase;
    
    [Header("Ownership")]
    [Tooltip("Indica si este personaje pertenece al jugador")]
    [SerializeField] private bool isPlayerOwned = false;
    
    [Header("Habilidades")]
    [SerializeField] private HabilidadData habilidadPorDefecto;
    [SerializeField] private List<HabilidadData> habilidadesDisponibles = new List<HabilidadData>();
    
    [Header("Referencias")]
    [SerializeField] private EntityStats entityStats;
    
    // Instancia de la entidad (logica pura)
    private Entidad entidadLogica;
    
    // Sistema de cooldowns
    private GestorCooldowns gestorCooldowns = new GestorCooldowns();
    
    // Propiedades publicas
    public Entidad EntidadLogica => entidadLogica;
    public EntityStats EntityStats => entityStats;
    
    /// <summary>Indica si este personaje pertenece al jugador.</summary>
    public bool IsPlayerOwned => isPlayerOwned;
    
    /// <summary>
    /// Marca este personaje como propiedad del jugador o no.
    /// Llamado por PlayerPartyManager al registrar/desregistrar.
    /// </summary>
    public void SetPlayerOwned(bool owned)
    {
        isPlayerOwned = owned;
        Debug.Log($"[EntityController] {Nombre_Entidad} → IsPlayerOwned = {owned}");
    }
    
    // IGestorHabilidades
    public GestorCooldowns Cooldowns => gestorCooldowns;
    public List<HabilidadData> HabilidadesDisponibles => habilidadesDisponibles;
    
    /// <summary>Sprite/icono del personaje (desde ClaseData).</summary>
    public Sprite SpritePersonaje => datosClase?.iconoClase;

    // Aplicar eventos de IProgresion
    public event System.Action<int> OnNivelSubido
    {
        add
        {
            if (entidadLogica is Jugador jugador)
            {
                jugador.OnNivelSubido += value;
            }
        }
        remove
        {
            if (entidadLogica is Jugador jugador)
            {
                jugador.OnNivelSubido -= value;
            }
        }
    }
    public event System.Action<float, float> OnXPGanada
    {
        add
        {
            if (entidadLogica is Jugador jugador)
            {
                jugador.OnXPGanada += value;
            }
        }
        remove
        {
            if (entidadLogica is Jugador jugador)
            {
                jugador.OnXPGanada -= value;
            }
        }
    }
    public event System.Action<int, int> OnManaCambiado
    {
        add
        {
            if (entidadLogica is Jugador jugador)
            {
                jugador.OnManaCambiado += value;
            }
        }
        remove
        {
            if (entidadLogica is Jugador jugador)
            {
                jugador.OnManaCambiado -= value;
            }
        }
    }
    



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
        if (datosClase != null)
        {
            Inicializar(datosClase);
        }
    }
    
    public void Inicializar(ClaseData datos)
    {
        datosClase = datos;
        
        // 1. Crear la instancia lógica correcta
        entidadLogica = datos.CrearInstancia();
        
        // 2. Vincular EntityStats con la Entidad (BIDIRECCIONAL)
        entityStats.VincularEntidad(entidadLogica);
        
        // 3. Si es un jugador, vincularlo con EntityStats para XP
        if (entidadLogica is Jugador jugador)
        {
            jugador.VincularEntityStats(entityStats);
            
            // Suscribirse a eventos específicos del jugador
            jugador.OnNivelSubido += ManejarSubidaNivel;
            jugador.OnXPGanada += ManejarXPGanada;
            
            Debug.Log($"{gameObject.name}: Jugador vinculado con EntityStats");
        }
        
        // 4. Suscribirse a eventos generales de entidad
        entidadLogica.OnDañoRecibido += ManejarDañoRecibido;
        entidadLogica.OnMuerte += ManejarMuerte;
        
        AplicarElementosIniciales();
        
        Debug.Log($"Entidad inicializada: {entidadLogica.Nombre_Entidad} (Nivel {entidadLogica.Nivel_Entidad})");
    }

    private void AplicarElementosIniciales()
    {
        if (datosClase != null && datosClase.atributos != ElementAttribute.None)
        {
            foreach (ElementAttribute flag in System.Enum.GetValues(typeof(ElementAttribute)))
            {
                if (flag != ElementAttribute.None && datosClase.atributos.HasFlag(flag))
                {
                    entityStats.AplicarElemento(flag);
                }
            }
        }
    }
    




    // =================================================================
    // =========== IMPLEMENTACIÓN DE IENTIDADCOMBATE (Fachada) =========
    // =================================================================
    
    // Redirección de propiedades (Getters)
    public string Nombre_Entidad => entidadLogica.Nombre_Entidad;
    public int Nivel_Entidad => entidadLogica.Nivel_Entidad;
    public int Vida_Entidad => entidadLogica.Vida_Entidad;
    public int VidaActual_Entidad => entidadLogica.VidaActual_Entidad;
    public int PuntosDeAtaque_Entidad => entidadLogica.PuntosDeAtaque_Entidad;
    public float PuntosDeDefensa_Entidad => entidadLogica.PuntosDeDefensa_Entidad;
    public int Velocidad => entidadLogica.Velocidad;
    public bool EsDerrotado => entidadLogica.EsDerrotado;
    public bool EstaMuerto => entidadLogica.EstaMuerto;
    public TipoEntidades TipoEntidad => entidadLogica.TipoEntidad;
    public ElementAttribute AtributosEntidad => entidadLogica.AtributosEntidad;

    public bool EstaVivo() => entidadLogica.EstaVivo();
    public bool PuedeActuar() => entidadLogica.PuedeActuar();
    
    public bool EsTipoEntidad(TipoEntidades tipo) => entidadLogica.EsTipoEntidad(tipo);
    public bool UsaEstiloDeCombate(CombatStyle estilo) => entidadLogica.UsaEstiloDeCombate(estilo);
    public int CalcularDanoContra(IEntidadCombate objetivo) => entidadLogica.CalcularDanoContra(objetivo);
    public DamageResult CalcularDanoContraConResultado(IEntidadCombate objetivo) => entidadLogica.CalcularDanoContraConResultado(objetivo);
    public CombatStats CombatStats => entidadLogica.CombatStats;

    // === Sistema de estados ===
    public void AplicarEstado(StatusFlag status, int duracion, int danoPorTurno = 0, float modificador = 0f)
    {
        entidadLogica.AplicarEstado(status, duracion, danoPorTurno, modificador);
    }
    
    public bool TieneEstado(StatusFlag status) => entidadLogica.TieneEstado(status);
    
    public void RemoverEstado(StatusFlag status) => entidadLogica.RemoverEstado(status);
    
    public void RecibirDano(int danoBruto, ElementAttribute tipo)
    {
        if (entidadLogica == null)
        {
            Debug.LogWarning("No se puede recibir dano: entidad no valida");
            return;
        }
        
        entidadLogica.RecibirDano(danoBruto, tipo);
    }
    
    public int Curar(int cantidad)
    {
        if (entidadLogica == null) return 0;
        return entidadLogica.Curar(cantidad);
    }


    // =================================================================
    // ============== IMPLEMENTACION DE IGESTORHABILIDADES =============
    // =================================================================
    
    public bool PuedeUsarHabilidad(HabilidadData habilidad)
    {
        if (habilidad == null) return false;
        
        // Verificar cooldown
        if (!gestorCooldowns.EstaDisponible(habilidad))
            return false;
        
        // Verificar costos de recursos usando el nuevo sistema
        // El método internamente verifica si la entidad implementa IRecursoProvider
        return habilidad.VerificarCostosRecursos(entidadLogica);
    }
    
    public void IniciarCooldown(HabilidadData habilidad)
    {
        gestorCooldowns.IniciarCooldown(habilidad);
    }
    
    public void ProcesarInicioTurno()
    {
        gestorCooldowns.ProcesarInicioTurno();
    }


    // =================================================================
    // ============== IMPLEMENTACION DE IENTIDADACTUABLE ===============
    // =================================================================
    
    /// <summary>
    /// Obtiene la accion elegida por el jugador. 
    /// Por ahora usa la habilidad por defecto y ataca al primer enemigo vivo.
    /// TODO: Integrar con sistema de UI para seleccion manual.
    /// </summary>
    public (IHabilidadesCommand comando, IEntidadCombate objetivo) ObtenerAccionElegida(
        List<IEntidadCombate> aliados, 
        List<IEntidadCombate> enemigos
    )
    {
        // Obtener la habilidad a usar (por defecto o la primera disponible que no este en cooldown)
        HabilidadData habilidad = null;
        
        if (habilidadPorDefecto != null && PuedeUsarHabilidad(habilidadPorDefecto))
        {
            habilidad = habilidadPorDefecto;
        }
        else
        {
            // Buscar primera habilidad disponible
            foreach (var hab in habilidadesDisponibles)
            {
                if (PuedeUsarHabilidad(hab))
                {
                    habilidad = hab;
                    break;
                }
            }
        }
        
        if (habilidad == null)
        {
            Debug.LogWarning(gameObject.name + ": No tiene habilidad disponible (todas en cooldown o sin mana).");
            return (null, null);
        }
        
        // Determinar el objetivo segun el tipo de habilidad
        IEntidadCombate objetivo = null;
        
        switch (habilidad.tipoObjetivo)
        {
            case TargetType.EnemigoUnico:
            case TargetType.EnemigoTodos:
                // Seleccionar el primer enemigo vivo (TODO: UI para seleccion)
                objetivo = enemigos.Find(e => e.EstaVivo());
                break;
                
            case TargetType.AliadoUnico:
            case TargetType.AliadoTodos:
                // Seleccionar el primer aliado vivo
                objetivo = aliados.Find(a => a.EstaVivo());
                break;
                
            case TargetType.Self:
                objetivo = this;
                break;
        }
        
        // Verificar viabilidad
        if (objetivo == null || !habilidad.EsViable(this, objetivo, aliados, enemigos))
        {
            Debug.Log($"⏭️ {Nombre_Entidad}: Habilidad no viable o sin objetivo válido.");
            return (null, null);
        }
        
        return (habilidad, objetivo);
    }


    // =================================================================
    // ============= IMPLEMENTACIÓN DE IJUGADORPROGRESION ==============
    // =================================================================

    // Redirección de propiedades (Solo aplica si la entidad es Jugador)
    public float Experiencia_Actual => (entidadLogica as Jugador)?.Experiencia_Actual ?? 0f;
    public float Experiencia_Progreso => (entidadLogica as Jugador)?.Experiencia_Progreso ?? 0f;
    public int Mana_jugador => (entidadLogica as Jugador)?.Mana_jugador ?? 0;
    public int ManaActual_jugador => (entidadLogica as Jugador)?.ManaActual_jugador ?? 0;
    public int Nivel_jugador => (entidadLogica as Jugador)?.Nivel_Entidad ?? 0;
    
    public void RecibirXP(float xp)
    {
        if (entidadLogica is Jugador jugador)
        {
            // El Manager ahora sólo le pasará la XP de nivel (80%).
            jugador.RecibirXP(xp);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} no es un jugador, no puede recibir XP de nivel.");
        }
    }
    

    // ========== MANEJADORES DE EVENTOS ==========
    
    private void ManejarDañoRecibido(int cantidad)
    {
        Debug.Log($"{entidadLogica.Nombre_Entidad} recibió {cantidad} de daño. Vida: {entidadLogica.VidaActual_Entidad}/{entidadLogica.Vida_Entidad}");
        // Aquí irían animaciones, efectos visuales, etc.
    }
    
    private void ManejarMuerte()
    {
        Debug.Log($"{entidadLogica.Nombre_Entidad} ha muerto!");
        // Aquí irían animaciones de muerte, drops, etc.
        
        // Por ahora solo desactivamos el objeto
        StartCoroutine(DesactivarDespuesDeMorir());
    }
    
    private void ManejarSubidaNivel(int nuevoNivel)
    {
        Debug.Log($"¡{entidadLogica.Nombre_Entidad} subió al nivel {nuevoNivel}!");
        // Aquí irían efectos visuales, sonidos, UI, etc.
    }
    
    private void ManejarXPGanada(float xpActual, float xpNecesaria)
    {
        float progreso = xpActual / xpNecesaria;
        Debug.Log($"{entidadLogica.Nombre_Entidad} - Progreso XP: {progreso * 100:F1}%");
        // Aquí actualizarías la barra de XP en la UI
    }

    private System.Collections.IEnumerator DesactivarDespuesDeMorir()
    {
        yield return new WaitForSeconds(2f);
        gameObject.SetActive(false);
    }
    
    
    // ========== LIMPIEZA ==========
    
    private void OnDestroy()
    {
        if (entidadLogica != null)
        {
            entidadLogica.OnDañoRecibido -= ManejarDañoRecibido;
            entidadLogica.OnMuerte -= ManejarMuerte;
            
            if (entidadLogica is Jugador jugador)
            {
                jugador.OnNivelSubido -= ManejarSubidaNivel;
                jugador.OnXPGanada -= ManejarXPGanada;
            }
        }
    }
    
    
    
}