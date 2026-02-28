using System;
using Flags;
using Interfaces;
using UnityEngine;
using Habilidades;
using Combate;


namespace Padres
{
    /// <summary>
    /// Datos de escalado por nivel para jugadores.
    /// Permite configurar el crecimiento de stats por clase.
    /// </summary>
    [System.Serializable]
    public class EscaladoJugador
    {
        public int vidaPorNivel = 50;
        public int ataquePorNivel = 5;
        public float defensaPorNivel = 2f;
        public int manaPorNivel = 10;
        public int velocidadPorNivel = 1;
        
        public EscaladoJugador() { }
        
        public EscaladoJugador(int vida, int ataque, float defensa, int mana, int velocidad)
        {
            vidaPorNivel = vida;
            ataquePorNivel = ataque;
            defensaPorNivel = defensa;
            manaPorNivel = mana;
            velocidadPorNivel = velocidad;
        }
    }

    public abstract class Jugador : Entidad, IJugadorProgresion, IRecursoProvider
    {
        public int Mana_jugador { get; protected set; }
        public int ManaActual_jugador { get; protected set; }

        public EntityStats entityStats;
        
        // Datos de escalado configurables
        protected EscaladoJugador escalado;
        
        // Sistema de habilidades activas
        public GestorHabilidades GestorHabilidades { get; protected set; }

        private ElementAttribute _atributos;
        private TipoEntidades _tipoDeJugador;
        private CombatStyle _estiloDeCombate;


        public event Action<int> OnNivelSubido;
        public event Action<float, float> OnXPGanada;
        public event Action<int, int> OnManaCambiado;
        public event Action<TipoRecurso, float, float> OnRecursoCambiado;

        public override TipoEntidades TipoEntidad => _tipoDeJugador;
        public override ElementAttribute AtributosEntidad => _atributos;


        public Jugador(string nombre, int vida, int ataque, float defensa, int nivel, int mana, int velocidad, ElementAttribute atributos, TipoEntidades tipoDeJugador, CombatStyle estiloDeCombate, EscaladoJugador escaladoStats = null)
        {
            Nombre_Entidad = nombre;
            Velocidad = velocidad;
            Vida_Entidad = vida;
            VidaActual_Entidad = vida;
            PuntosDeAtaque_Entidad = ataque;
            PuntosDeDefensa_Entidad = defensa;
            Nivel_Entidad = nivel;
            Experiencia_Progreso = EscaladoExperiencia(nivel + 1);
            Experiencia_Actual = 0;
            EsDerrotado = false;
            EstaMuerto = false;

            Mana_jugador = mana;
            ManaActual_jugador = mana;

            _atributos = atributos;
            _tipoDeJugador = tipoDeJugador;
            _estiloDeCombate = estiloDeCombate;
            
            // Usar escalado por defecto si no se proporciona
            escalado = escaladoStats ?? new EscaladoJugador();
            
            // Inicializar gestores
            InicializarGestorPasivas();
            GestorHabilidades = new GestorHabilidades(this);
            
            // Inicializar stats de combate con valores base
            CombatStats = new CombatStats
            {
                critChance = CombatConfig.Instance?.baseCritChance ?? 0.05f,
                critMultiplier = CombatConfig.Instance?.baseCritMultiplier ?? 1.5f,
                elementoAtaque = atributos
            };
        }

        /// <summary>
        /// Inicializa las habilidades desde ClaseData.
        /// Llamar después de la construcción en las clases derivadas.
        /// </summary>
        public void InicializarDesdeClaseData(ClaseData datos)
        {
            if (datos == null) return;
            
            // Configurar límites
            GestorHabilidades = new GestorHabilidades(this, datos.habilidadesIniciales, datos.limiteHabilidadesActivas);
            
            // Agregar pasivas iniciales
            if (datos.pasivasIniciales != null)
            {
                foreach (var pasiva in datos.pasivasIniciales)
                {
                    GestorPasivas?.AgregarPasiva(pasiva);
                }
            }
            
            // Hook para inicialización de comportamiento específico de clase
            InicializarComportamientoDeClase();
        }

        // Metodos de vinculacion
        public void VincularEntityStats(EntityStats stats)
        {
            entityStats = stats;
        }


        // Metodos del jugador
        


        // temporales para probar
        
        public override bool EsTipoEntidad(TipoEntidades tipo)
        {
            return (_tipoDeJugador & tipo) == tipo;
        }
        public override bool UsaEstiloDeCombate(CombatStyle estilo)
        {
            return (_estiloDeCombate & estilo) == estilo;
        }
        

        // Metodos de progresion
        public void RecibirXP(float xp)
        {
            // Dividir experiencia
            float xpJugador = xp * 0.8f;
            float xpElementos = xp * 0.2f;

            // XP para el jugador
            Experiencia_Actual += xpJugador;

            while (Experiencia_Actual >= Experiencia_Progreso)
            {
                int xpRestante = (int)(Experiencia_Actual - Experiencia_Progreso);
                SubirNivel();
                Experiencia_Actual = xpRestante;
                Experiencia_Progreso = EscaladoExperiencia(Nivel_Entidad + 1);
                OnXPGanada?.Invoke(Experiencia_Actual, Experiencia_Progreso);
            }

            // Repartir XP a cada elemento activo (si existe el EntityStats)
            if (entityStats != null && entityStats.activeStatuses.Count > 0)
            {
                // Dividir la XP de elementos entre todos los elementos activos
                float xpPorElemento = xpElementos / entityStats.activeStatuses.Count;

                foreach (ElementStatus status in entityStats.activeStatuses)
                {
                    if (status != null && status.definition != null)
                    {
                        bool subioNivel = status.GainXP(xpPorElemento);
                        
                        if (subioNivel)
                        {
                            Debug.Log($"{Nombre_Entidad}: Elemento {status.definition.elementName} subió a nivel {status.level}!");
                            // Recalcular estadísticas cuando un elemento sube de nivel
                            entityStats.ApplyElementalModifiers();
                        }
                    }
                }
            }
        }

        public static int EscaladoExperiencia(int Nivel_Entidad)
        {
            if (Nivel_Entidad <= 1)
            {
                return 0;
            }
            const int baseXp = 100;
            const double TasaCrecimiento = 0.10;
            const int Limite = 60;

            double xpnecesaria;
            if (Nivel_Entidad <= Limite + 1)
            {
                xpnecesaria = baseXp * Math.Pow(1 + TasaCrecimiento, Nivel_Entidad - 2);
            }
            else
            {
                const double TasapostLimite = 0.025;
                double constoNivel60 = baseXp * Math.Pow(1 + TasaCrecimiento, Limite - 1);
                xpnecesaria = constoNivel60 * Math.Pow(1 + TasapostLimite, Nivel_Entidad - (Limite - 1));
            }
            return (int)Math.Round(xpnecesaria);
        }
        public virtual void SubirNivel()
        {
            Nivel_Entidad++;
            
            // Aplicar escalado de stats de forma segura
            AplicarEscaladoNivel();
            
            OnNivelSubido?.Invoke(Nivel_Entidad);
            
            // Hook post-level-up para comportamiento específico de clase
            OnNivelSubidoClase(Nivel_Entidad);
        }
        
        
        protected virtual void AplicarEscaladoNivel()
        {
            if (escalado == null) return;
            
            // Incrementar stats
            Vida_Entidad += escalado.vidaPorNivel;
            PuntosDeAtaque_Entidad += escalado.ataquePorNivel;
            PuntosDeDefensa_Entidad += escalado.defensaPorNivel;
            Mana_jugador += escalado.manaPorNivel;
            Velocidad += escalado.velocidadPorNivel;
            
            // Curar completamente y restaurar mana al subir de nivel
            VidaActual_Entidad = Vida_Entidad;
            ManaActual_jugador = Mana_jugador;
            
            // Notificar cambios
            NotificarVidaCambiada();
            OnManaCambiado?.Invoke(ManaActual_jugador, Mana_jugador);
        }

                internal void AjustarMana(int delta)
        {
            Mana_jugador = Math.Max(0, Mana_jugador + delta);
            ManaActual_jugador = Math.Min(Mana_jugador, Math.Max(0, ManaActual_jugador + delta));
            OnManaCambiado?.Invoke(ManaActual_jugador, Mana_jugador);
        }

        internal void AjustarManaPercent(float multiplicador)
        {
            Mana_jugador = Math.Max(0, (int)(Mana_jugador * multiplicador));
            ManaActual_jugador = Math.Min(ManaActual_jugador, Mana_jugador);
            OnManaCambiado?.Invoke(ManaActual_jugador, Mana_jugador);
        }

        #region B1: Sistema de Recursos (IRecursoProvider)

        /// <summary>
        /// Recurso principal de esta clase. Subclases overridean para cambiar
        /// (ej: Paladín => Fe, Hematómante => Sangre).
        /// </summary>
        protected virtual TipoRecurso RecursoPrincipal => TipoRecurso.Mana;

        /// <summary>
        /// Consume el recurso principal. Subclases overridean para comportamiento especial
        /// (ej: Hematómante consume HP si no tiene Sangre).
        /// </summary>
        protected virtual bool ConsumoRecursoPrincipal(float cantidad)
        {
            if (ManaActual_jugador < cantidad) return false;
            ManaActual_jugador -= (int)cantidad;
            if (ManaActual_jugador < 0) ManaActual_jugador = 0;
            OnManaCambiado?.Invoke(ManaActual_jugador, Mana_jugador);
            OnRecursoCambiado?.Invoke(RecursoPrincipal, ManaActual_jugador, Mana_jugador);
            return true;
        }

        /// <summary>
        /// Regeneración de recurso por turno. Subclases overridean para lógica específica.
        /// Se invoca automáticamente al inicio de cada turno (ver ProcesarEstadosInicioTurno).
        /// </summary>
        protected virtual void RegenerarRecursoPorTurno() { }

        /// <summary>
        /// Callback cuando un recurso se agota. Subclases overridean para efectos especiales
        /// (ej: Hematómante consume HP, Berserker entra en furia).
        /// </summary>
        protected virtual void OnRecursoAgotado(TipoRecurso tipo, float deficit) { }

        // === Implementación de IRecursoProvider ===

        public float ObtenerRecursoActual(TipoRecurso tipo)
        {
            return tipo == RecursoPrincipal ? ManaActual_jugador : 0f;
        }

        public float ObtenerRecursoMaximo(TipoRecurso tipo)
        {
            return tipo == RecursoPrincipal ? Mana_jugador : 0f;
        }

        public bool TieneRecursoSuficiente(TipoRecurso tipo, float cantidad)
        {
            return tipo == RecursoPrincipal && ObtenerRecursoActual(tipo) >= cantidad;
        }

        public bool ConsumirRecurso(TipoRecurso tipo, float cantidad)
        {
            if (tipo != RecursoPrincipal) return false;
            bool exito = ConsumoRecursoPrincipal(cantidad);
            if (!exito)
            {
                OnRecursoAgotado(tipo, cantidad - ManaActual_jugador);
            }
            return exito;
        }

        public void RestaurarRecurso(TipoRecurso tipo, float cantidad)
        {
            if (tipo != RecursoPrincipal) return;
            ManaActual_jugador = Math.Min(Mana_jugador, ManaActual_jugador + (int)cantidad);
            OnManaCambiado?.Invoke(ManaActual_jugador, Mana_jugador);
            OnRecursoCambiado?.Invoke(tipo, ManaActual_jugador, Mana_jugador);
        }

        public bool PoseeRecurso(TipoRecurso tipo)
        {
            return tipo == RecursoPrincipal;
        }

        #endregion

        #region B2: Modificador de Curación Otorgada

        /// <summary>
        /// Modifica la cantidad de curación que esta clase otorga a otros.
        /// Subclases overridean para bonus de clase (ej: Paladín +% curación).
        /// Se conectará al HealEffect pipeline cuando se verifique su funcionamiento.
        /// </summary>
        public virtual int ModificarCuracionOtorgada(int cantidadBase, IEntidadCombate objetivo)
        {
            return cantidadBase;
        }

        #endregion

        #region B3: Hooks de Muerte con Contexto

        /// <summary>
        /// Callback ejecutado cuando este jugador muere, ANTES de marcar EstaMuerto.
        /// Subclases overridean para comportamiento de clase al morir
        /// (ej: Lich intenta convertir, No Muerto tiene chance de revivir).
        /// </summary>
        protected virtual void AlMorir(IEntidadCombate asesino) { }

        /// <summary>
        /// Callback ejecutado cuando este jugador elimina a una víctima.
        /// Invocado desde AplicarDanoDesdeContexto cuando el defensor muere.
        /// Subclases overridean para efectos on-kill (ej: Lich convierte víctima).
        /// </summary>
        public virtual void AlEliminar(Entidad victima) { }

        /// <summary>
        /// Override de Morir para ejecutar AlMorir antes de marcar muerte.
        /// </summary>
        protected override void Morir(IEntidadCombate asesino = null)
        {
            AlMorir(asesino);
            base.Morir(asesino);
        }

        #endregion

        #region B4: Efecto Ambiental

        /// <summary>
        /// Indica si esta clase tiene un efecto ambiental persistente.
        /// Subclases overridean (ej: No Muerto => true para aura de terror).
        /// </summary>
        public virtual bool TieneEfectoAmbiental => false;

        /// <summary>
        /// Procesa el tick de efecto ambiental. Se invocará desde el sistema de
        /// tick de mundo cuando exista (no implementado aún).
        /// Subclases overridean con lógica de aura/efecto.
        /// </summary>
        protected virtual void ProcesarTickAmbiental(float deltaTime) { }

        #endregion

        #region B5: Hooks de Casteo

        /// <summary>
        /// Callback ejecutado antes de castear una habilidad.
        /// Retorna false para cancelar el casteo.
        /// Se conectará al CombateManager/TurnManager cuando se integre.
        /// Subclases overridean para modificadores condicionales pre-cast.
        /// </summary>
        protected virtual bool AntesDeCastear(HabilidadData habilidad, IEntidadCombate objetivo)
        {
            return true;
        }

        /// <summary>
        /// Callback ejecutado después de castear una habilidad.
        /// Se conectará al CombateManager/TurnManager cuando se integre.
        /// Subclases overridean para efectos post-cast (ej: ganar carga, cooldown reducido).
        /// </summary>
        protected virtual void DespuesDeCastear(HabilidadData habilidad, IEntidadCombate objetivo) { }

        #endregion

        #region B6: Inicialización de Comportamiento de Clase

        /// <summary>
        /// Hook para inicializar comportamiento específico de clase.
        /// Se invoca al final de InicializarDesdeClaseData().
        /// Subclases overridean para suscripciones a eventos, configuración de auras, etc.
        /// </summary>
        protected virtual void InicializarComportamientoDeClase() { }

        /// <summary>
        /// Hook para limpiar comportamiento específico de clase.
        /// Llamar al destruir/reciclar la entidad.
        /// Subclases overridean para desuscribirse de eventos, limpiar auras, etc.
        /// </summary>
        protected virtual void LimpiarComportamientoDeClase() { }

        #endregion

        #region B7: Hook Post-Level-Up

        /// <summary>
        /// Callback específico de clase al subir de nivel.
        /// Se invoca al final de SubirNivel() después de AplicarEscaladoNivel.
        /// Subclases overridean para desbloquear habilidades por nivel, etc.
        /// </summary>
        protected virtual void OnNivelSubidoClase(int nuevoNivel) { }

        #endregion

        #region C1: Override de ProcesarEstadosInicioTurno

        /// <summary>
        /// Override para conectar RegenerarRecursoPorTurno al ciclo de turno.
        /// </summary>
        public override bool ProcesarEstadosInicioTurno()
        {
            bool puedeActuar = base.ProcesarEstadosInicioTurno();
            RegenerarRecursoPorTurno();
            return puedeActuar;
        }

        #endregion
    }
}