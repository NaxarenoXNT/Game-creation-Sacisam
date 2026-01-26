using System;
using Interfaces;
using Flags;
using UnityEngine;





namespace Padres
{
    public abstract class Entidad : IEntidadCombate
    {
        public int Vida_Entidad { get; protected set; }
        public int VidaActual_Entidad { get; protected set; }
        public int PuntosDeAtaque_Entidad { get; protected set; }
        public int Nivel_Entidad { get; protected set; }
        
        // Sistema de estados activos
        public GestorEstados GestorEstados { get; protected set; } = new GestorEstados();

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
            // 1. Logica de Mitigacion por Facciones (Sobrescribir en NoMuerto.cs, Elemental.cs)
            int danoDespuesFaccion = AplicarMitigacionPorFaccion(danoBruto, tipo);

            // 2. Logica de Mitigacion por Defensa (formula logaritmica)
            float multiplicadorDefensa = 1f - (PuntosDeDefensa_Entidad / (PuntosDeDefensa_Entidad + 100f));
            int danoMitigado = Mathf.Max(1, (int)(danoDespuesFaccion * multiplicadorDefensa));

            // 3. Aplicar dano y actualizar vida
            VidaActual_Entidad -= danoMitigado;

            if (VidaActual_Entidad < 0)
            {
                VidaActual_Entidad = 0;
            }

            OnDañoRecibido?.Invoke(danoMitigado);
            OnVidaCambiada?.Invoke(VidaActual_Entidad, Vida_Entidad);

            if (!EstaVivo())
            {
                Morir();
            }
        }
        protected virtual int AplicarMitigacionPorFaccion(int danoBruto, ElementAttribute tipo)
        {
            // Logica base: no hay modificacion (se usa el dano bruto)
            return danoBruto;
        }
        public virtual int CalcularDanoContra(IEntidadCombate objetivo)
        {
            return PuntosDeAtaque_Entidad;
        }
        public virtual int Curar(int cantidad)
        {
            if (cantidad <= 0 || !EstaVivo()) return 0;

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

        protected virtual void Morir()
        {
            EstaMuerto = true;
            EsDerrotado = true;
            OnMuerte?.Invoke();
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