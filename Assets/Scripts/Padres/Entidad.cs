using System;
using Interfaces;
using Flasgs;
using UnityEngine;





namespace Padres
{
    public abstract class Entidad : IEntidadCombate
    {
        public int Vida_Entidad { get; protected set; }
        public int VidaActual_Entidad { get; protected set; }
        public int PuntosDeAtaque_Entidad { get; protected set; }
        public int Nivel_Entidad { get; protected set; }

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

    
        public virtual bool PuedeActuar()
        {
            return EstaVivo() && !EsDerrotado;
        }
        public bool EstaVivo()
        {
            return VidaActual_Entidad > 0 && !EstaMuerto;
        }


        public virtual void RecibirDaño(int dañoBruto, ElementAttribute tipo)
        {
            // 1. Lógica de Mitigación por Facciones (Sobrescribir en NoMuerto.cs, Elemental.cs)
            int dañoDespuesFaccion = AplicarMitigacionPorFaccion(dañoBruto, tipo);

            // 2. Lógica de Mitigación por Defensa (Tu fórmula logarítmica)
            float multiplicadorDefensa = 1f - (PuntosDeDefensa_Entidad / (PuntosDeDefensa_Entidad + 100f));
            int dañoMitigado = Mathf.Max(1, (int)(dañoDespuesFaccion * multiplicadorDefensa));

            // 3. Aplicar daño y actualizar vida
            VidaActual_Entidad -= dañoMitigado;

            if (VidaActual_Entidad < 0)
            {
                VidaActual_Entidad = 0;
            }

            OnDañoRecibido?.Invoke(dañoMitigado);
            OnVidaCambiada?.Invoke(VidaActual_Entidad, Vida_Entidad);

            if (!EstaVivo())
            {
                Morir();
            }

            // Método ahora es void, no retorna dañoMitigado.
        }
        protected virtual int AplicarMitigacionPorFaccion(int dañoBruto, ElementAttribute tipo)
        {
            // Lógica base: no hay modificación (se usa el daño bruto)
            return dañoBruto;
        }
        public virtual int CalcularDañoContra(IEntidadCombate objetivo)
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
        public virtual void AplicarEstado(StatusFlag status, int duracion) 
        {
            // Aquí iría la lógica para añadir el estado a una lista de estados activos
            // y notificar al EntityController para efectos visuales/UI.
            Debug.Log($"[Estado Aplicado]: {Nombre_Entidad} afectado por {status} por {duracion} turnos.");
        }
        


        /// <summary>
        /// Permite a EntityStats aplicar bonos elementales a las estadísticas.
        /// Solo accesible desde el mismo assembly (internal).
        /// </summary>
        internal void AplicarBonusElementales(int ataque, int vidaMaxima, float defensa, int velocidad)
        {
            // Guardar la vida actual como porcentaje antes de cambiar la vida máxima
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