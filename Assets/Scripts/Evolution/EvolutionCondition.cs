using System;
using UnityEngine;
using Flags;

namespace Evolution
{
    /// <summary>
    /// Condición genérica y extensible para desbloquear traits.
    /// Para agregar nuevas condiciones, solo añade al enum ConditionType
    /// y su lógica correspondiente en EvolutionEvaluator.EvaluarCondicion().
    /// </summary>
    [Serializable]
    public class EvolutionCondition
    {
        [Tooltip("Tipo de condición a evaluar")]
        public ConditionType tipo;

        [Header("Parámetros Genéricos")]
        [Tooltip("Parámetro string (para IDs custom o compatibilidad)")]
        public string parametro;

        [Tooltip("Cantidad o valor entero requerido")]
        public int cantidad = 1;

        [Tooltip("Valor float para condiciones como karma/reputación (mínimo)")]
        public float floatValor;

        [Tooltip("Segundo valor float para rangos (máximo)")]
        public float floatValorMax;

        [Header("Referencias Tipadas")]
        [Tooltip("Tipo de entidad para KillsTipo")]
        public TipoEntidades tipoEntidad;
        
        [Tooltip("Trait requerido (para TieneTrait)")]
        public TraitDefinition traitRequerido;
        
        [Tooltip("Elemento requerido")]
        public ElementAttribute elemento;
        
        [Tooltip("Estado de combate (para EstadoActivo/EstadoAplicadoVeces)")]
        public StatusFlag statusFlag;

        [Header("UI")]
        [Tooltip("Descripción para mostrar en UI (ej: 'Mata 50 no-muertos')")]
        public string descripcionUI;
        
        /// <summary>
        /// Obtiene el parámetro efectivo para evaluación.
        /// Prioriza referencias tipadas sobre strings genéricos.
        /// </summary>
        public string GetParametroEfectivo()
        {
            switch (tipo)
            {
                case ConditionType.KillsTipo:
                    return tipoEntidad != TipoEntidades.None ? tipoEntidad.ToString() : parametro;
                    
                case ConditionType.TieneTrait:
                    return traitRequerido != null ? traitRequerido.id : parametro;
                    
                case ConditionType.EstadoActivo:
                case ConditionType.EstadoAplicadoVeces:
                    return statusFlag != StatusFlag.None ? statusFlag.ToString() : parametro;
                    
                default:
                    return parametro;
            }
        }
        
        /// <summary>
        /// Genera descripción automática si no hay una manual.
        /// </summary>
        public string GetDescripcionAuto()
        {
            if (!string.IsNullOrEmpty(descripcionUI)) return descripcionUI;
            
            return tipo switch
            {
                ConditionType.KillsTipo => $"Elimina {cantidad} {(tipoEntidad != TipoEntidades.None ? tipoEntidad.ToString() : parametro)}",
                ConditionType.KillsTotal => $"Elimina {cantidad} enemigos",
                ConditionType.NivelMinimo => $"Alcanza nivel {cantidad}",
                ConditionType.TieneTrait => $"Obtén el trait: {(traitRequerido != null ? traitRequerido.nombreMostrar : parametro)}",
                ConditionType.KarmaMinimo => $"Karma mínimo: {floatValor:F1}",
                ConditionType.KarmaMaximo => $"Karma máximo: {floatValor:F1}",
                ConditionType.MisionCompletada => $"Completa: {parametro}",
                ConditionType.Sacrificios => $"Realiza {cantidad} sacrificios",
                _ => $"{tipo}: {GetParametroEfectivo()} x{cantidad}"
            };
        }
    }

    /// <summary>
    /// Tipos de condiciones soportados.
    /// Para agregar nuevos: añade aquí y en EvolutionEvaluator.EvaluarCondicion()
    /// </summary>
    public enum ConditionType
    {
        // ========== Combate ==========
        KillsTipo,              // parametro = TipoEntidades.ToString(), cantidad = número
        KillsTotal,             // cantidad = total de kills de cualquier tipo
        DañoInfligidoTotal,     // cantidad = daño total infligido
        DañoRecibidoTotal,      // cantidad = daño total recibido
        CuracionTotal,          // cantidad = curación total realizada
        CombatesSinRecibirDaño, // cantidad = combates ganados sin recibir daño
        ComboMaximo,            // cantidad = combo máximo alcanzado

        // ========== Progresión ==========
        NivelMinimo,            // cantidad = nivel requerido
        TiempoJugado,           // cantidad = segundos jugados

        // ========== Karma ==========
        KarmaMinimo,            // floatValor = karma mínimo (-1 a 1)
        KarmaMaximo,            // floatValor = karma máximo (-1 a 1)
        KarmaRango,             // floatValor = min, floatValorMax = max

        // ========== Facciones ==========
        ReputacionFaccion,      // parametro = faccionId, floatValor = reputación mínima
        RangoFaccion,           // parametro = faccionId, cantidad = rango mínimo

        // ========== Misiones ==========
        MisionCompletada,       // parametro = misionId
        MisionesCompletadasTotal, // cantidad = número total de misiones completadas

        // ========== Items ==========
        PoseeItem,              // parametro = itemId, cantidad = cuántos
        ItemUsado,              // parametro = itemId, cantidad = veces usado

        // ========== Economía ==========
        OroGastado,             // cantidad = oro total gastado
        OroActual,              // cantidad = oro actual en inventario

        // ========== Habilidades ==========
        HabilidadUsada,         // parametro = habilidadId, cantidad = veces
        PoseeHabilidad,         // parametro = habilidadId

        // ========== Estados de Combate ==========
        EstadoActivo,           // parametro = statusId (estado actualmente activo)
        EstadoAplicadoVeces,    // parametro = statusId, cantidad = veces aplicado

        // ========== Exploración ==========
        BiomaVisitado,          // parametro = biomaId
        BiomasVisitadosTotal,   // cantidad = número de biomas distintos

        // ========== Tags Genéricos ==========
        TieneTag,               // parametro = tagId (para condiciones custom)

        // ========== Traits (usado principalmente para evoluciones) ==========
        TieneTrait,             // parametro = traitId
        TraitsTotal,            // cantidad = número total de traits

        // ========== Muertes/Sacrificios ==========
        Sacrificios,            // cantidad = sacrificios realizados
        MuertesJugador,         // cantidad = veces que ha muerto el jugador

        // ========== Evoluciones Previas ==========
        EvolucionPrevia,        // parametro = evolutionId

        // ========== Custom ==========
        Custom                  // parametro = customFlagKey, cantidad = valor mínimo
    }
}
