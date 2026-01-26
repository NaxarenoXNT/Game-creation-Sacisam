using Flags;
using Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace Padres
{
    /// <summary>
    /// Datos de escalado por nivel para enemigos.
    /// Permite configurar el crecimiento de stats desde el ScriptableObject.
    /// </summary>
    [System.Serializable]
    public class EscaladoEnemigo
    {
        public int vidaPorNivel = 100;
        public int ataquePorNivel = 10;
        public float defensaPorNivel = 5f;
        public int velocidadPorNivel = 2;
        
        public EscaladoEnemigo() { }
        
        public EscaladoEnemigo(int vida, int ataque, float defensa, int velocidad)
        {
            vidaPorNivel = vida;
            ataquePorNivel = ataque;
            defensaPorNivel = defensa;
            velocidadPorNivel = velocidad;
        }
    }

    public abstract class Enemigos : Entidad
    {
        public int XPOtorgada { get; protected set; }
        
        // Datos de escalado configurables
        protected EscaladoEnemigo escalado;

        private ElementAttribute _atributos;
        private TipoEntidades _tipoDeEnemigo;
        private CombatStyle _estiloDeCombate;
        public override TipoEntidades TipoEntidad => _tipoDeEnemigo;
        public override ElementAttribute AtributosEntidad => _atributos;

        public event Action<int> OnNivelSubido;


        public Enemigos(string nombre, int vida, int ataque, float defensa, int nivel, int velocidad, int xp, ElementAttribute atributos, TipoEntidades tipoDeEnemigo, CombatStyle estiloDeCombate, EscaladoEnemigo escaladoStats = null)
        {
            Nombre_Entidad = nombre;
            Vida_Entidad = vida;
            VidaActual_Entidad = vida;
            PuntosDeAtaque_Entidad = ataque;
            PuntosDeDefensa_Entidad = defensa;
            Nivel_Entidad = nivel;
            Velocidad = velocidad;

            XPOtorgada = xp;

            _atributos = atributos;
            _tipoDeEnemigo = tipoDeEnemigo;
            _estiloDeCombate = estiloDeCombate;
            
            // Usar escalado por defecto si no se proporciona
            escalado = escaladoStats ?? new EscaladoEnemigo();

            EsDerrotado = false;
            EstaMuerto = false;
        }

        /// <summary>
        /// Sube de nivel al enemigo aplicando el escalado configurado.
        /// </summary>
        public virtual void SubirNivel()
        {
            Nivel_Entidad++;
            
            // Aplicar escalado de stats de forma segura
            AplicarEscaladoNivel();
            
            OnNivelSubido?.Invoke(Nivel_Entidad);
        }
        
        /// <summary>
        /// Aplica el escalado de estadísticas al subir de nivel.
        /// Puede ser sobrescrito para comportamiento personalizado.
        /// </summary>
        protected virtual void AplicarEscaladoNivel()
        {
            if (escalado == null) return;
            
            // Incrementar stats
            Vida_Entidad += escalado.vidaPorNivel;
            PuntosDeAtaque_Entidad += escalado.ataquePorNivel;
            PuntosDeDefensa_Entidad += escalado.defensaPorNivel;
            Velocidad += escalado.velocidadPorNivel;
            
            // Curar completamente al subir de nivel
            VidaActual_Entidad = Vida_Entidad;
            
            // Notificar cambio de vida
            NotificarVidaCambiada();
        }



        // temporales para probar
        
        public override bool EsTipoEntidad(TipoEntidades tipo)
        {
            return (_tipoDeEnemigo & tipo) == tipo;
        }
        public override bool UsaEstiloDeCombate(CombatStyle estilo)
        {
            return (_estiloDeCombate & estilo) == estilo;
        }


        public abstract IEntidadCombate DecidirObjetivo(List<IEntidadCombate> jugadores);
    }
}