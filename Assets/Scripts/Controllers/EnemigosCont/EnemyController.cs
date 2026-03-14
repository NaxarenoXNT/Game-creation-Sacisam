using UnityEngine;
using Padres;
using Flags;
using Interfaces;
using Habilidades;
using Combate;
using System.Collections.Generic;
using Managers;
using World.ChunkSystem;
using IA.Roaming;

/// <summary>
/// Controlador de enemigo que conecta la lógica con Unity.
/// Versión simplificada enfocada en enemigos.
/// Implementa IPooleable para soporte de object pooling.
/// </summary>
public class EnemyController : MonoBehaviour, IEntidadCombate, IEntidadActuable, ICombatCandidate, IPooleable
{
    [Header("Configuración")]
    [SerializeField] private EnemigoData datosEnemigo;
    
    // Referencia al EnemigoData original (para re-inicializar desde pool)
    private EnemigoData datosEnemigoOriginales;
    
    // Referencia al spawn config (para tracking de muerte)
    private string spawnId;
    private Vector2Int chunkCoords;
    
    // Sistema de roaming (IA fuera de combate)
    private EnemyRoamingFSM roamingFSM;
    private EnemySpawnConfig spawnConfig;
    
    // Modelo visual instanciado dinámicamente
    private GameObject visualInstance;

    [Header("Referencias")]
    [SerializeField] private EntityStats entityStats;
    
    [Header("Combat Candidate Settings")]
    [Tooltip("Prioridad base para entrar en combate (mayor = más probable)")]
    [SerializeField] private float baseCombatPriority = 1f;
    
    [Tooltip("Si está en modo aggro (persiguiendo al jugador)")]
    [SerializeField] private bool isAggro = false;
    
    [Tooltip("Distancia máxima para iniciar combate (0 = usar reglas globales)")]
    [SerializeField] private float maxEngagementDistance = 0f;
    
    [Tooltip("Requiere aggro para entrar en combate")]
    [SerializeField] private bool requiresAggroToEngage = false;
    
    [Tooltip("Layer de enemigos para detección de alerta (usado por alertaAliados)")]
    [SerializeField] private LayerMask capaEnemigos = ~0;
    
    // Estado de combate
    private bool isInCombat = false;
    private string candidateId;
    
    // Instancia de la entidad 
    
    private Enemigos enemigoLogica;
    
    // Propiedades públicas
    public Enemigos EnemigoLogica => enemigoLogica;
    public EntityStats EntityStats => entityStats;
    public EnemigoData DatosEnemigo => datosEnemigoOriginales;
    public bool IsInCombat => isInCombat;
    public bool IsAggro => isAggro;
    public EnemyRoamingFSM RoamingFSM => roamingFSM;
    
    private void Awake()
    {
        // Generar ID único
        candidateId = $"{gameObject.name}_{GetInstanceID()}";
        
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
        datosEnemigoOriginales = datos; // Guardar referencia para pooling
        
        CrearEntidadLogica(datos);
    }
    
    /// <summary>
    /// Inicializa el controller con datos del chunk.
    /// </summary>
    public void InicializarDesdeChunk(EnemigoData datos, string spawnId, Vector2Int chunkCoords, EnemySpawnConfig config = null)
    {
        this.spawnId = spawnId;
        this.chunkCoords = chunkCoords;
        this.spawnConfig = config;
        
        Inicializar(datos);
        
        // Instanciar modelo visual si está configurado
        InstanciarModeloVisual(datos);
        
        // Inicializar FSM de roaming si tenemos configuración
        if (config != null)
        {
            roamingFSM = new EnemyRoamingFSM(this, config, debugLogs: false);
            Debug.Log($"[EnemyController] FSM inicializado para {datos.nombreEnemigo}");
        }
    }
    
    private void CrearEntidadLogica(EnemigoData datos)
    {
        // Limpiar entidad anterior si existe
        if (enemigoLogica != null)
        {
            DesuscribirEventos();
        }
        
        // 1. Crear la instancia lógica correcta según el tipo
        enemigoLogica = datos.CrearInstancia();
        
        // 2. Vincular EntityStats con el Enemigo (BIDIRECCIONAL)
        entityStats.VincularEntidad(enemigoLogica);
        
        // 3. Suscribirse a eventos del enemigo
        SuscribirEventos();
        
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
    
    /// <summary>
    /// Instancia el modelo visual del enemigo como hijo del controller.
    /// </summary>
    private void InstanciarModeloVisual(EnemigoData datos)
    {
        // Limpiar modelo anterior si existe
        if (visualInstance != null)
        {
            Destroy(visualInstance);
            visualInstance = null;
        }
        
        // Instanciar nuevo modelo si está configurado
        if (datos.modeloPrefab != null)
        {
            visualInstance = Instantiate(datos.modeloPrefab, transform);
            visualInstance.transform.localPosition = Vector3.zero;
            visualInstance.transform.localRotation = Quaternion.identity;
            
            // Aplicar animator override si existe
            if (datos.animatorOverride != null)
            {
                var animator = visualInstance.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.runtimeAnimatorController = datos.animatorOverride;
                    Debug.Log($"🎭 Animator aplicado a {datos.nombreEnemigo}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ AnimatorOverride configurado pero el modelo no tiene Animator: {datos.nombreEnemigo}");
                }
            }
            
            Debug.Log($"✨ Modelo visual instanciado: {datos.nombreEnemigo} → {datos.modeloPrefab.name}");
        }
        else
        {
            Debug.LogWarning($"⚠️ EnemigoData '{datos.nombreEnemigo}' no tiene modeloPrefab asignado. El enemigo será invisible.");
        }
    }
    
    // === Unity Lifecycle ===
    
    private void Update()
    {
        // Solo actualizar FSM si no está en combate
        if (!isInCombat && roamingFSM != null)
        {
            roamingFSM.Update();
        }
    }
    
    private void FixedUpdate()
    {
        // Solo actualizar física del FSM si no está en combate
        if (!isInCombat && roamingFSM != null)
        {
            roamingFSM.FixedUpdate();
        }
    }
    
    private void OnDrawGizmos()
    {
        // Dibujar gizmos del detector si está activo
        if (roamingFSM != null && roamingFSM.PlayerDetector != null)
        {
            roamingFSM.PlayerDetector.DrawGizmos();
        }

        // Dibujar radio de alerta si este enemigo es alertador
        if (datosEnemigo != null && datosEnemigo.alertaAliados)
        {
            float radio = datosEnemigo.rangoAliados > 0f ? datosEnemigo.rangoAliados : 20f;
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f); // naranja translúcido
            Gizmos.DrawSphere(transform.position, radio);
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, radio);
        }
    }
    
    // === Sistema de estados ===
    public void AplicarEstado(StatusFlag status, int duracion, int danoPorTurno = 0, float modificador = 0f)
    {
        if (enemigoLogica == null)
        {
            Debug.LogWarning("No se puede aplicar estado: enemigo no valido");
            return;
        }
        
        enemigoLogica.AplicarEstado(status, duracion, danoPorTurno, modificador);
    }
    
    public bool TieneEstado(StatusFlag status) => enemigoLogica?.TieneEstado(status) ?? false;
    
    public void RemoverEstado(StatusFlag status) => enemigoLogica?.RemoverEstado(status);



    // =================================================================
    // ============== IMPLEMENTACIÓN DE IENTIDADACTUABLE ===============
    // =================================================================

    public (IHabilidadesCommand comando, IEntidadCombate objetivo) ObtenerAccionElegida(
        List<IEntidadCombate> aliados, 
        List<IEntidadCombate> enemigos
    )
    {
        if (enemigoLogica == null) return (null, null);
        // Toda la lógica de decisión vive en Enemigos (base class) + CerebroIA.
        return enemigoLogica.ObtenerAccionElegida(aliados, enemigos);
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
    public int CalcularDanoContra(IEntidadCombate objetivo) => enemigoLogica.CalcularDanoContra(objetivo);
    public DamageResult CalcularDanoContraConResultado(IEntidadCombate objetivo) => enemigoLogica.CalcularDanoContraConResultado(objetivo);
    public CombatStats CombatStats => enemigoLogica.CombatStats;
    
    public void RecibirDano(int danoBruto, ElementAttribute tipo)
    {
        if (enemigoLogica == null)
        {
            Debug.LogWarning("No se puede recibir dano: enemigo no valido");
            return;
        }
        
        enemigoLogica.RecibirDano(danoBruto, tipo);
    }
    
    public int Curar(int cantidad)
    {
        if (enemigoLogica == null) return 0;
        return enemigoLogica.Curar(cantidad);
    }
    
    
    
    // ========== MANEJADORES DE EVENTOS ==========
    
    private void ManejarDanoRecibido(int cantidad)
    {
        Debug.Log(enemigoLogica.Nombre_Entidad + " recibio " + cantidad + " de dano. Vida: " + enemigoLogica.VidaActual_Entidad + "/" + enemigoLogica.Vida_Entidad);
        // Aqui irian animaciones, efectos visuales, etc.
        // Por ahora solo cambiar color temporalmente
        StartCoroutine(FlashDamage());
    }
    
    private void ManejarMuerte()
    {
        Debug.Log(enemigoLogica.Nombre_Entidad + " ha muerto!");
        
        // IMPORTANTE: Copiar datos ANTES de cualquier operación
        // (el controller puede ser devuelto al pool y reciclado)
        var evento = new EventoEnemigoDerrotado
        {
            IDInstanciaEnemigo = candidateId,
            TipoEnemigo = enemigoLogica.TipoEntidad,
            NombreEnemigo = enemigoLogica.Nombre_Entidad,
            NivelEnemigo = enemigoLogica.Nivel_Entidad,
            XPOtorgada = (enemigoLogica as Enemigos)?.XPOtorgada ?? 0f,
            PosicionMuerte = transform.position,
            Asesino = null, // TODO: Pasar atacante si está disponible
            Timestamp = Time.time
        };
        
        // Publicar evento con datos copiados
        EventBus.Publicar(evento);
        
        // Notificar al WorldChunkManager que este enemigo murió
        if (WorldChunkManager.Instance != null && !string.IsNullOrEmpty(spawnId))
        {
            WorldChunkManager.Instance.NotificarEnemigoDerrotado(spawnId, chunkCoords, this);
        }
        
        // Animación de muerte y devolverlo al pool lo maneja el WorldChunkManager
        StartCoroutine(EfectoMuerteYDesactivar());
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
    
    private System.Collections.IEnumerator EfectoMuerteYDesactivar()
    {
        // Efecto visual de muerte
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.black;
        }
        
        // Esperar animación de muerte
        yield return new WaitForSeconds(2f);
        
        // El WorldChunkManager se encargará de devolver al pool
        // Solo desactivamos aquí para evitar double-return
        gameObject.SetActive(false);
    }


    // ========== LIMPIEZA ==========

    private void SuscribirEventos()
    {
        if (enemigoLogica == null) return;
        
        enemigoLogica.OnDañoRecibido += ManejarDanoRecibido;
        enemigoLogica.OnMuerte += ManejarMuerte;
        enemigoLogica.OnNivelSubido += ManejarSubidaNivel;
    }
    
    private void DesuscribirEventos()
    {
        if (enemigoLogica == null) return;
        
        enemigoLogica.OnDañoRecibido -= ManejarDanoRecibido;
        enemigoLogica.OnMuerte -= ManejarMuerte;
        enemigoLogica.OnNivelSubido -= ManejarSubidaNivel;
    }
    
    private void OnDestroy()
    {
        DesuscribirEventos();
    }
    
    // =================================================================
    // ============== IMPLEMENTACIÓN DE IPOOLEABLE ====================
    // =================================================================
    
    /// <summary>
    /// Llamado cuando el controller sale del pool (se activa).
    /// Re-crea la entidad lógica para evitar memory leaks.
    /// </summary>
    public void OnObtenidoDelPool()
    {
        // Re-crear entidad lógica con los datos originales.
        // Si datosEnemigoOriginales es null, el objeto es nuevo y será inicializado
        // por InicializarDesdeChunk() inmediatamente después de Obtener().
        if (datosEnemigoOriginales != null)
        {
            CrearEntidadLogica(datosEnemigoOriginales);
            
            Debug.Log($"♻️ EnemyController obtenido del pool: {enemigoLogica.Nombre_Entidad}");
        }
        // else: objeto recién creado por el pool, InicializarDesdeChunk lo inicializará.
        
        // Resetear estado de combate
        isInCombat = false;
        isAggro = false;
        
        // Resetear FSM si existe
        if (roamingFSM != null)
        {
            roamingFSM.Reset();
        }
        
        // Regenerar ID único
        candidateId = $"{gameObject.name}_{GetInstanceID()}_{Time.frameCount}";
        
        // Resetear animador si existe
        var animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }
    
    /// <summary>
    /// Llamado cuando el controller vuelve al pool (se desactiva).
    /// Limpia todas las referencias y eventos.
    /// </summary>
    public void OnDevueltoAlPool()
    {
        Debug.Log($"♻️ EnemyController devuelto al pool: {enemigoLogica?.Nombre_Entidad ?? "Unknown"}");
        
        // Destruir modelo visual instanciado
        if (visualInstance != null)
        {
            Destroy(visualInstance);
            visualInstance = null;
            Debug.Log("🗑️ Modelo visual destruido");
        }
        
        // Desuscribir eventos (esto ya limpia todas las referencias)
        DesuscribirEventos();
        
        // Limpiar visuales
        entityStats?.LimpiarVisuales();
        
        // Resetear renderer si existe (por si hay render en el controller mismo)
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.white;
        }
        
        // Resetear estado de combate
        isInCombat = false;
        isAggro = false;
        
        // Limpiar referencias de chunk
        spawnId = null;
        chunkCoords = Vector2Int.zero;
        spawnConfig = null;
        roamingFSM = null;
    }
    
    
    // =================================================================
    // ============== IMPLEMENTACIÓN DE ICOMBATCANDIDATE ===============
    // =================================================================
    
    /// <summary>ID único de este candidato.</summary>
    public string CandidateId => candidateId;
    
    /// <summary>Transform para cálculos de distancia.</summary>
    public Transform CandidateTransform => transform;
    
    /// <summary>Prioridad de combate (aggro aumenta prioridad).</summary>
    public float CombatPriority => isAggro ? baseCombatPriority + 10f : baseCombatPriority;
    
    /// <summary>
    /// Evalúa si este enemigo puede unirse al combate.
    /// </summary>
    public bool CanJoinCombat(CombatContext context)
    {
        // Ya está en combate
        if (isInCombat)
            return false;
        
        // No está inicializado
        if (enemigoLogica == null)
            return false;
        
        // Está muerto
        if (!EstaVivo())
            return false;
        
        // Requiere aggro pero no lo tiene
        if (requiresAggroToEngage && !isAggro)
            return false;
        
        // Verificar distancia personalizada
        if (maxEngagementDistance > 0)
        {
            float distance = Vector3.Distance(transform.position, context.PlayerPosition);
            if (distance > maxEngagementDistance)
                return false;
        }
        
        // Verificar límite de enemigos
        // (El EncounterManager ya lo verifica, pero podemos tener lógica adicional)
        
        // TODO: Agregar más condiciones específicas del enemigo
        // - Verificar si está en cooldown de combate
        // - Verificar estado de misión
        // - Verificar hora del día / bioma
        // - Verificar si hay otros enemigos del mismo grupo
        
        return true;
    }
    
    /// <summary>
    /// Llamado cuando el enemigo es seleccionado para combate.
    /// </summary>
    public void OnSelectedForCombat()
    {
        isInCombat = true;
        isAggro = true; // Entrar en combate activa aggro
        
        // Pausar FSM de roaming
        if (roamingFSM != null)
        {
            roamingFSM.Pause();
        }

        // Si este tipo de enemigo alerta a aliados, notificarlos
        if (datosEnemigo != null && datosEnemigo.alertaAliados)
        {
            AlertarEnemigosCercanos();
        }
        
        Debug.Log($"⚔️ {Nombre_Entidad} entró en combate!");
    }
    
    /// <summary>
    /// Llamado cuando el enemigo es removido del combate.
    /// </summary>
    public void OnRemovedFromCombat()
    {
        isInCombat = false;
        
        // Reanudar FSM de roaming si está vivo
        if (EstaVivo() && roamingFSM != null)
        {
            roamingFSM.Resume();
        }
        
        // Mantener aggro si sigue vivo (para perseguir al jugador)
        // o resetearlo si murió
        if (!EstaVivo())
        {
            isAggro = false;
        }
        
        Debug.Log($"🏃 {Nombre_Entidad} salió del combate");
    }
    
    // === Métodos de control de Aggro ===
    
    /// <summary>
    /// Activa el modo aggro manualmente.
    /// </summary>
    public void SetAggro(bool aggro)
    {
        isAggro = aggro;
        
        if (aggro)
        {
            Debug.Log($"😠 {Nombre_Entidad} está en aggro!");
        }
    }
    
    /// <summary>
    /// Establece la prioridad de combate.
    /// </summary>
    public void SetCombatPriority(float priority)
    {
        baseCombatPriority = priority;
    }

    // ============================================================
    // === Sistema de Alerta a Aliados ============================
    // ============================================================

    /// <summary>
    /// Alerta a los enemigos cercanos para que entren en estado Alerted.
    /// Solo se llama si datosEnemigo.alertaAliados es true.
    /// El radio se toma de datosEnemigo.rangoAliados (default 20m).
    /// Incluye verificación de línea de visión.
    /// </summary>
    private void AlertarEnemigosCercanos()
    {
        float radio = (datosEnemigo.rangoAliados > 0f) ? datosEnemigo.rangoAliados : 20f;

        Collider[] cercanos = Physics.OverlapSphere(transform.position, radio, capaEnemigos);

        // Evitar alertar al mismo enemigo dos veces si tiene múltiples colliders
        var yaAlertados = new System.Collections.Generic.HashSet<EnemyController>();

        foreach (var col in cercanos)
        {
            EnemyController aliado = col.GetComponentInParent<EnemyController>();

            if (aliado == null || aliado == this) continue;
            if (yaAlertados.Contains(aliado)) continue;
            if (aliado.IsInCombat || aliado.IsAggro) continue;
            if (aliado.RoamingFSM == null || !aliado.RoamingFSM.IsActive) continue;

            // Verificar línea de visión
            Vector3 origen  = transform.position + Vector3.up;
            Vector3 destino = aliado.transform.position + Vector3.up;
            Vector3 dir     = destino - origen;

            if (Physics.Raycast(origen, dir.normalized, out RaycastHit hit, dir.magnitude,
                                 ~0, QueryTriggerInteraction.Ignore))
            {
                // Si lo primero que golpea el rayo no es el aliado (ni parte de él)
                // hay un obstáculo sólido bloqueando la visión
                if (!hit.collider.transform.IsChildOf(aliado.transform) &&
                     hit.collider.gameObject != aliado.gameObject)
                {
                    continue;
                }
            }

            yaAlertados.Add(aliado);
            aliado.RoamingFSM.ForceState(EnemyAIState.Alerted);
            Debug.Log($"🔔 {Nombre_Entidad} alertó a {aliado.Nombre_Entidad}");
        }

        if (yaAlertados.Count > 0)
            Debug.Log($"🔔 {Nombre_Entidad} alertó a {yaAlertados.Count} aliado(s) en un radio de {radio}m");
    }
}