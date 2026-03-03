using System;
using System.Collections.Generic;
using Interfaces;
using Flags;
using UnityEngine;
using Habilidades;
using Combate;
using Managers;




namespace Padres
{
    public abstract class Entidad : IEntidadCombate
    {
        // Campo temporal para propagar contexto de atacante a Morir()
        // Se setea en AplicarDanoDesdeContexto antes de evaluar muerte.
        private IEntidadCombate _ultimoAtacante;

        public int Vida_Entidad { get; protected set; }
        public int VidaActual_Entidad { get; protected set; }
        public int PuntosDeAtaque_Entidad { get; protected set; }
        public int Nivel_Entidad { get; protected set; }
        
        // Sistema de estados activos
        public GestorEstados GestorEstados { get; protected set; } = new GestorEstados();
        
        // Sistema de pasivas
        public GestorPasivas GestorPasivas { get; protected set; }
        
        // Estadísticas de combate (crítico, elemental, resistencias)
        public CombatStats CombatStats { get; protected set; } = new CombatStats();

        // Modificadores de daño por entidad (pasivas, traits)
        public List<Combate.IDamageModifier> EntityDamageModifiers { get; protected set; } = new List<Combate.IDamageModifier>();

        // Handler de efectos activos (lazy init para poder pasar 'this')
        private Efectos.EffectHandler _effectHandler;
        public Efectos.EffectHandler EffectHandler => _effectHandler ??= new Efectos.EffectHandler(this);

        public float PuntosDeDefensa_Entidad { get; protected set; }
        public float Experiencia_Progreso { get; protected set; }
        public float Experiencia_Actual { get; protected set; }

        public int Velocidad { get; protected set; }

        public string Nombre_Entidad { get; protected set; }

        public bool EsDerrotado { get; protected set; }
        public bool EstaMuerto { get; protected set; }

        public abstract TipoEntidades TipoEntidad { get; }
        public abstract ElementAttribute AtributosEntidad { get; }



        public event Action<int, int> OnVidaCambiada;
        public event Action<int> OnDañoRecibido;
        public event Action OnMuerte;
        
        // Métodos protegidos para invocar eventos desde clases derivadas
        protected void NotificarVidaCambiada() => OnVidaCambiada?.Invoke(VidaActual_Entidad, Vida_Entidad);
        protected void NotificarDañoRecibido(int cantidad) => OnDañoRecibido?.Invoke(cantidad);
        protected void NotificarMuerte() => OnMuerte?.Invoke();

        /// <summary>
        /// Inicializa el gestor de pasivas. Llamar desde el constructor de clases derivadas.
        /// </summary>
        protected void InicializarGestorPasivas()
        {
            GestorPasivas = new GestorPasivas(this);
        }

        #region Modificadores de Stats (para pasivas y buffs)
        
        /// <summary>
        /// Modifica la vida máxima. Usado por pasivas y buffs.
        /// </summary>
        public void ModificarVidaMaxima(int cantidad)
        {
            Vida_Entidad += cantidad;
            if (Vida_Entidad < 1) Vida_Entidad = 1;
            
            // Ajustar vida actual si excede el máximo
            if (VidaActual_Entidad > Vida_Entidad)
                VidaActual_Entidad = Vida_Entidad;
                
            OnVidaCambiada?.Invoke(VidaActual_Entidad, Vida_Entidad);
        }

        /// <summary>
        /// Modifica el ataque. Usado por pasivas y buffs.
        /// </summary>
        public void ModificarAtaque(int cantidad)
        {
            PuntosDeAtaque_Entidad += cantidad;
            if (PuntosDeAtaque_Entidad < 0) PuntosDeAtaque_Entidad = 0;
        }

        /// <summary>
        /// Modifica la defensa. Usado por pasivas y buffs.
        /// Virtual para que subclases apliquen modificadores (ej: Guerrero +15%).
        /// </summary>
        public virtual void ModificarDefensa(float cantidad)
        {
            PuntosDeDefensa_Entidad += cantidad;
            if (PuntosDeDefensa_Entidad < 0) PuntosDeDefensa_Entidad = 0;
        }

        /// <summary>
        /// Modifica la velocidad. Usado por pasivas y buffs.
        /// </summary>
        public void ModificarVelocidad(int cantidad)
        {
            Velocidad += cantidad;
            if (Velocidad < 1) Velocidad = 1;
        }

        /// <summary>
        /// Hook para modificar la curación recibida antes de aplicarla.
        /// Jugador lo override para iterar los módulos de evolución (+% curación).
        /// </summary>
        protected virtual int ModificarCuracionRecibida(int cantidad) => cantidad;

        #endregion
    
        /// <summary>
        /// Verifica si la entidad puede actuar este turno.
        /// Considera vida, derrota y estados incapacitantes.
        /// </summary>
        public virtual bool PuedeActuar()
        {
            return EstaVivo() && !EsDerrotado && !GestorEstados.EstaIncapacitado;
        }
        
        public bool EstaVivo()
        {
            return VidaActual_Entidad > 0 && !EstaMuerto;
        }


        public virtual void RecibirDano(int danoBruto, ElementAttribute tipo)
        {
            // 1. Mitigación por Facciones (Sobrescribir en NoMuerto.cs, Elemental.cs)
            int danoDespuesFaccion = AplicarMitigacionPorFaccion(danoBruto, tipo);

            // 2. Mitigación por Defensa (fórmula logarítmica nueva)
            // DEF_MULT = 1 / (1 + ln(1 + DEF) / K)
            float k = CombatConfig.Instance?.defenseConstantK ?? 5f;
            float multiplicadorDefensa = DamageCalculator.CalculateDefenseMultiplier(PuntosDeDefensa_Entidad, k);
            int danoMitigado = Mathf.Max(1, (int)(danoDespuesFaccion * multiplicadorDefensa));
            
            // 3. Mitigación por Resistencia Elemental
            if (tipo != ElementAttribute.None && CombatStats?.resistencias != null)
            {
                float resistencia = CombatStats.resistencias.GetResistance(tipo);
                float elemMult = Mathf.Clamp(1f - resistencia, 0.1f, 1.5f);
                danoMitigado = Mathf.Max(1, (int)(danoMitigado * elemMult));
            }

            // 4. Aplicar daño y actualizar vida
            VidaActual_Entidad -= danoMitigado;

            if (VidaActual_Entidad < 0)
            {
                VidaActual_Entidad = 0;
            }

            OnDañoRecibido?.Invoke(danoMitigado);
            OnVidaCambiada?.Invoke(VidaActual_Entidad, Vida_Entidad);

            if (!EstaVivo())
            {
                Morir(_ultimoAtacante);
            }
        }

        /// <summary>
        /// Recibe daño ignorando la defensa del objetivo.
        /// Útil para habilidades de daño verdadero o efectos de estado.
        /// </summary>
        public virtual void RecibirDanoPuro(int danoBruto, ElementAttribute tipo)
        {
            // Solo aplica mitigación por facción, NO por defensa
            int danoDespuesFaccion = AplicarMitigacionPorFaccion(danoBruto, tipo);
            
            VidaActual_Entidad -= danoDespuesFaccion;

            if (VidaActual_Entidad < 0)
            {
                VidaActual_Entidad = 0;
            }

            OnDañoRecibido?.Invoke(danoDespuesFaccion);
            OnVidaCambiada?.Invoke(VidaActual_Entidad, Vida_Entidad);

            if (!EstaVivo())
            {
                Morir(_ultimoAtacante);
            }
        }

        protected virtual int AplicarMitigacionPorFaccion(int danoBruto, ElementAttribute tipo)
        {
            // Logica base: no hay modificacion (se usa el dano bruto)
            return danoBruto;
        }
        
        /// <summary>
        /// Calcula el daño contra un objetivo usando la fórmula completa.
        /// BASE_OFFENSE = (ATK + ELEM_ATK) * RACE_ATK
        /// OFFENSE = BASE_OFFENSE * (isCrit ? CRIT_MULT : 1)
        /// DEF_MULT = 1 / (1 + ln(1 + DEF * RACE_DEF) / K)
        /// PHYSICAL_DAMAGE = OFFENSE * DEF_MULT
        /// ELEMENTAL_DAMAGE = ELEM_ATK * clamp(1 - RES_e, 0.1, 1.5)
        /// FINAL_DAMAGE = PHYSICAL_DAMAGE + ELEMENTAL_DAMAGE
        /// </summary>
        public virtual int CalcularDanoContra(IEntidadCombate objetivo)
        {
            return CalcularDanoContraConResultado(objetivo).finalDamage;
        }
        
        /// <summary>
        /// Calcula el daño con resultado detallado (incluye si fue crítico, daño elemental, etc).
        /// </summary>
        public virtual DamageResult CalcularDanoContraConResultado(IEntidadCombate objetivo)
        {
            var config = CombatConfig.Instance;
            
            // Preparar datos del atacante
            var attackerData = new AttackerData
            {
                attack = PuntosDeAtaque_Entidad,
                elementalAttack = CombatStats?.elementalAttack ?? 0,
                attackElement = CombatStats?.elementoAtaque ?? AtributosEntidad,
                critChance = CombatStats?.critChance ?? config?.baseCritChance ?? 0.05f,
                critMultiplier = CombatStats?.critMultiplier ?? config?.baseCritMultiplier ?? 1.5f,
                critAppliesToElemental = CombatStats?.critAppliesToElemental ?? false,
                entityType = TipoEntidad
            };

            // Hook de mecánica de clase: crítico garantizado (ej: Arquero en sigilo)
            if (this is Jugador jugCrit && jugCrit.ForzarCritico())
                attackerData.critChance = 1f;

            // Hook de módulos: override sustitutivo de elemento de ataque (ej: Paladín → Light)
            if (this is Jugador jugElem)
                attackerData.attackElement = jugElem.ModificarElementoAtaqueDeModulos(attackerData.attackElement);

            // Preparar datos del defensor
            CombatStats defenderStats = null;
            if (objetivo is Entidad entidadObjetivo)
            {
                defenderStats = entidadObjetivo.CombatStats;
            }
            
            var defenderData = new DefenderData
            {
                defense = objetivo.PuntosDeDefensa_Entidad,
                resistances = defenderStats?.resistencias,
                entityType = objetivo.TipoEntidad
            };
            
            // Calcular daño usando el sistema central
            float k = config?.defenseConstantK ?? 5f;
            var raceModifiers = config?.raceModifiers;
            
            return DamageCalculator.CalculateDamage(attackerData, defenderData, raceModifiers, k);
        }
        public virtual int Curar(int cantidad)
        {
            if (cantidad <= 0 || !EstaVivo()) return 0;

            // Hook de módulos/clase: permite +% curación recibida (ej: Paladín)
            cantidad = ModificarCuracionRecibida(cantidad);

            int vidaAntes = VidaActual_Entidad;
            VidaActual_Entidad += cantidad;

            if (VidaActual_Entidad > Vida_Entidad)
            {
                VidaActual_Entidad = Vida_Entidad;
            }

            int vidaCurada = VidaActual_Entidad - vidaAntes;
            OnVidaCambiada?.Invoke(VidaActual_Entidad, Vida_Entidad);
            return vidaCurada;
        }

        protected virtual void Morir(IEntidadCombate asesino = null)
        {
            if (EstaMuerto) return;
            EstaMuerto = true;
            EsDerrotado = true;
            OnMuerte?.Invoke();
            
            // Publicar evento de muerte al EventBus con contexto de asesino
            EventBus.Publicar(new EventoMuerte
            {
                Entidad = this,
                Asesino = asesino
            });
        }

        /// <summary>
        /// Aplica daño desde un DamageContext procesado por el DamagePipeline.
        /// Este es el método preferido para aplicar daño en código nuevo.
        /// Propaga el atacante al flujo de muerte si la entidad muere.
        /// </summary>
        public virtual void AplicarDanoDesdeContexto(DamageContext context)
        {
            if (!EstaVivo()) return;
            
            int dano = context.FinalDamage;
            _ultimoAtacante = context.Attacker;
            
            VidaActual_Entidad -= dano;
            if (VidaActual_Entidad < 0) VidaActual_Entidad = 0;
            
            OnDañoRecibido?.Invoke(dano);
            OnVidaCambiada?.Invoke(VidaActual_Entidad, Vida_Entidad);
            
            // Notificar efectos de crit al EffectHandler del atacante
            if (context.IsCritical && context.Attacker is Entidad atacanteEnt)
            {
                atacanteEnt.EffectHandler.NotifyCriticalHit(context);
            }
            
            bool objetivoMurio = !EstaVivo();
            if (objetivoMurio)
            {
                Morir(context.Attacker);
                
                // Notificar al atacante que eliminó a esta entidad
                if (context.Attacker is Jugador jugadorAtacante)
                {
                    jugadorAtacante.AlEliminar(this);
                }
            }

            // Hook post-ataque para mecánicas de clase del atacante (ej: salir de sigilo)
            if (context.Attacker is Jugador atacanteJugador)
                atacanteJugador.PostAtaqueConContexto(context, objetivoMurio);
            
            _ultimoAtacante = null;
        }


        

        public abstract bool EsTipoEntidad(TipoEntidades tipo);
        public abstract bool UsaEstiloDeCombate(CombatStyle estilo);
        
        /// <summary>
        /// Aplica un estado de efecto a la entidad.
        /// </summary>
        public virtual void AplicarEstado(StatusFlag status, int duracion, int danoPorTurno = 0, float modificador = 0f) 
        {
            GestorEstados.AplicarEstado(status, duracion, danoPorTurno, modificador);
        }
        
        /// <summary>
        /// Procesa los estados al inicio del turno de esta entidad.
        /// Retorna true si la entidad puede actuar (no esta incapacitada).
        /// </summary>
        public virtual bool ProcesarEstadosInicioTurno()
        {
            // Procesar dano por estados (veneno, quemado)
            int danoEstados = GestorEstados.ProcesarInicioTurno();
            
            if (danoEstados > 0)
            {
                // Aplicar dano directo sin mitigacion (es dano de estado)
                VidaActual_Entidad -= danoEstados;
                
                if (VidaActual_Entidad <= 0)
                {
                    VidaActual_Entidad = 0;
                    Morir();
                }
                
                NotificarVidaCambiada();
                Debug.Log(Nombre_Entidad + " recibe " + danoEstados + " de dano por estados. Vida: " + VidaActual_Entidad + "/" + Vida_Entidad);
            }
            
            // Verificar si puede actuar
            if (GestorEstados.EstaIncapacitado)
            {
                Debug.Log(Nombre_Entidad + " esta incapacitado y no puede actuar este turno.");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Verifica si tiene un estado especifico.
        /// </summary>
        public bool TieneEstado(StatusFlag status)
        {
            return GestorEstados.TieneEstado(status);
        }
        
        /// <summary>
        /// Remueve un estado especifico.
        /// </summary>
        public void RemoverEstado(StatusFlag status)
        {
            GestorEstados.RemoverEstado(status);
        }
        


        /// <summary>
        /// Permite a EntityStats aplicar bonos elementales a las estadisticas.
        /// Solo accesible desde el mismo assembly (internal).
        /// </summary>
        internal void AplicarBonusElementales(int ataque, int vidaMaxima, float defensa, int velocidad)
        {
            // Guardar la vida actual como porcentaje antes de cambiar la vida maxima
            float porcentajeVida = Vida_Entidad > 0 ? (float)VidaActual_Entidad / Vida_Entidad : 1f;
                
            // Aplicar las nuevas estadísticas con bonos
            PuntosDeAtaque_Entidad = ataque;
            Vida_Entidad = vidaMaxima;
            PuntosDeDefensa_Entidad = defensa;
            Velocidad = velocidad;
                
            // Ajustar la vida actual proporcionalmente
            VidaActual_Entidad = Mathf.RoundToInt(Vida_Entidad * porcentajeVida);
                
            // Asegurar que la vida actual no exceda la máxima
            if (VidaActual_Entidad > Vida_Entidad)
            {
                VidaActual_Entidad = Vida_Entidad;
            }
        }
            
        internal void ActualizarStat(StatType tipo, int valor)
        {
            switch (tipo)
            {
                case StatType.Ataque:
                    PuntosDeAtaque_Entidad = valor;
                    break;
                case StatType.VidaMaxima:
                    int vidaAnterior = Vida_Entidad;
                    Vida_Entidad = valor;
                    // Ajustar vida actual si la vida máxima cambió
                    if (VidaActual_Entidad > Vida_Entidad)
                        VidaActual_Entidad = Vida_Entidad;
                    break;
                case StatType.Velocidad:
                        Velocidad = valor;
                    break;
            }
        }
            
        internal void ActualizarStat(StatType tipo, float valor)
        {
            switch (tipo)
            {
                case StatType.Defensa:
                    PuntosDeDefensa_Entidad = valor;
                    break;
            }
        }
            
            
    }
        
            
    public enum StatType
    {
        Ataque,
        VidaMaxima,
        Defensa,
        Velocidad
    }

    
}