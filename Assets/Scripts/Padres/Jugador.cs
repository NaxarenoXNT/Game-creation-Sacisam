using System;
using Flags;
using Interfaces;
using UnityEngine;



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

    public abstract class Jugador : Entidad, IJugadorProgresion
    {
        public int Mana_jugador { get; protected set; }
        public int ManaActual_jugador { get; protected set; }

        public EntityStats entityStats;
        
        // Datos de escalado configurables
        protected EscaladoJugador escalado;

        private ElementAttribute _atributos;
        private TipoEntidades _tipoDeJugador;
        private CombatStyle _estiloDeCombate;


        public event Action<int> OnNivelSubido;
        public event Action<float, float> OnXPGanada;
        public event Action<int, int> OnManaCambiado;

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
    }
}