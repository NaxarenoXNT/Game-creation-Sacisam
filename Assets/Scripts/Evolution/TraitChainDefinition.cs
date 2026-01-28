using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evolution
{
    /// <summary>
    /// Define un eslabón dentro de una cadena de traits.
    /// Cada eslabón tiene condiciones propias que se SUMAN a las heredadas.
    /// </summary>
    [Serializable]
    public class TraitChainNode
    {
        [Header("Identidad del Nodo")]
        [Tooltip("Sufijo para el nombre (ej: 'I', 'II', 'III' o 'Novato', 'Experto')")]
        public string sufijo;
        
        [Tooltip("Descripción específica de este nivel")]
        [TextArea]
        public string descripcion;
        
        [Header("Condiciones Adicionales")]
        [Tooltip("Condiciones ADICIONALES a las del nodo anterior (referencias a SOs)")]
        public List<EvolutionConditionSO> condicionesAdicionales = new List<EvolutionConditionSO>();
        
        [Header("Efectos de Este Nivel")]
        [Tooltip("Efectos que se aplican al obtener este nivel del trait")]
        public List<EvolutionEffect> efectos = new List<EvolutionEffect>();
        
        [Header("Modificadores de Progresión")]
        [Tooltip("Multiplicador de cantidad para condiciones base escalables (ej: 1.5x = 50% más)")]
        public float multiplicadorCantidad = 1.5f;
        
        [Tooltip("Si true, las condiciones base se escalan. Si false, solo se usan las adicionales")]
        public bool heredaCondicionesBase = true;
    }

    /// <summary>
    /// Define una cadena de traits que progresan en secuencia.
    /// Ejemplo: Sacrificios I → II → III → IV
    /// Usa ScriptableObjects para las condiciones (evita problemas de serialización).
    /// </summary>
    [CreateAssetMenu(fileName = "TraitChain", menuName = "Evolutions/Trait Chain")]
    public class TraitChainDefinition : ScriptableObject
    {
        [Header("Identidad de la Cadena")]
        [Tooltip("ID base de la cadena (ej: 'sacrificios')")]
        public string idBase;
        
        [Tooltip("Nombre base para mostrar (ej: 'Sacrificios')")]
        public string nombreBase;
        
        [TextArea]
        [Tooltip("Descripción general de la cadena")]
        public string descripcionGeneral;
        
        public Sprite iconoBase;
        public EvolutionRarity rarezaBase = EvolutionRarity.Common;

        [Header("Restricciones")]
        [Tooltip("Clases que NO pueden progresar en esta cadena")]
        public List<ClaseData> clasesBloqueadas = new List<ClaseData>();
        
        [Tooltip("Traits que excluyen toda esta cadena")]
        public List<TraitDefinition> exclusionesGlobales = new List<TraitDefinition>();

        [Header("Condiciones Base (Nivel 1)")]
        [Tooltip("Condiciones para desbloquear el PRIMER nivel de la cadena (referencias a SOs)")]
        public List<EvolutionConditionSO> condicionesBase = new List<EvolutionConditionSO>();

        [Header("Nodos de Progresión")]
        [Tooltip("Define cada nivel de la cadena (I, II, III, etc.)")]
        public List<TraitChainNode> nodos = new List<TraitChainNode>();

        [Header("Evolución Final (Opcional)")]
        [Tooltip("Evolución que se desbloquea al completar toda la cadena")]
        public ClassEvolutionDefinition evolucionFinal;
        
        [Tooltip("Condiciones adicionales para la evolución final")]
        public List<EvolutionConditionSO> condicionesEvolucionFinal = new List<EvolutionConditionSO>();

        /// <summary>
        /// Genera el ID de un trait específico en la cadena.
        /// </summary>
        public string GetTraitId(int indiceNodo)
        {
            if (indiceNodo < 0 || indiceNodo >= nodos.Count) return null;
            return $"{idBase}_{nodos[indiceNodo].sufijo}".ToLower().Replace(" ", "_");
        }

        /// <summary>
        /// Genera el nombre para mostrar de un trait específico.
        /// </summary>
        public string GetTraitNombre(int indiceNodo)
        {
            if (indiceNodo < 0 || indiceNodo >= nodos.Count) return nombreBase;
            return $"{nombreBase} {nodos[indiceNodo].sufijo}";
        }

        /// <summary>
        /// Obtiene las condiciones acumuladas para un nodo específico.
        /// Devuelve referencias a los SOs originales o copias escaladas según configuración.
        /// </summary>
        public List<EvolutionConditionSO> GetCondicionesAcumuladas(int indiceNodo)
        {
            var condiciones = new List<EvolutionConditionSO>();
            
            if (indiceNodo < 0 || indiceNodo >= nodos.Count) return condiciones;

            // Si es el primer nodo, usar condiciones base directamente
            if (indiceNodo == 0)
            {
                condiciones.AddRange(condicionesBase);
            }
            else
            {
                var nodo = nodos[indiceNodo];
                
                // Heredar condiciones base con escalado si corresponde
                if (nodo.heredaCondicionesBase && condicionesBase.Count > 0)
                {
                    // Calcular multiplicador acumulado
                    float multiplicadorAcumulado = 1f;
                    for (int i = 0; i <= indiceNodo; i++)
                    {
                        multiplicadorAcumulado *= nodos[i].multiplicadorCantidad;
                    }
                    
                    // Escalar condiciones base
                    foreach (var cond in condicionesBase)
                    {
                        if (cond == null) continue;
                        condiciones.Add(cond.CrearCopiaEscalada(multiplicadorAcumulado));
                    }
                }
            }

            // Agregar condiciones adicionales del nodo actual
            if (nodos[indiceNodo].condicionesAdicionales != null)
            {
                condiciones.AddRange(nodos[indiceNodo].condicionesAdicionales);
            }

            return condiciones;
        }

        /// <summary>
        /// Verifica si el jugador cumple las condiciones para un nodo específico.
        /// </summary>
        public bool CumpleCondicionesNodo(int indiceNodo, EvolutionState state)
        {
            // Primero verificar si tiene el trait anterior (excepto para nodo 0)
            if (indiceNodo > 0)
            {
                string traitAnteriorId = GetTraitId(indiceNodo - 1);
                if (!state.traitStacks.ContainsKey(traitAnteriorId))
                    return false;
            }

            // Verificar todas las condiciones del nodo
            var condiciones = GetCondicionesAcumuladas(indiceNodo);
            foreach (var cond in condiciones)
            {
                if (cond != null && !cond.Evaluar(state))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Obtiene el progreso del nodo (para UI).
        /// </summary>
        public float GetProgresoNodo(int indiceNodo, EvolutionState state)
        {
            var condiciones = GetCondicionesAcumuladas(indiceNodo);
            if (condiciones.Count == 0) return 1f;

            float progresoTotal = 0f;
            int condicionesValidas = 0;

            foreach (var cond in condiciones)
            {
                if (cond == null) continue;
                progresoTotal += cond.GetProgreso(state);
                condicionesValidas++;
            }

            return condicionesValidas > 0 ? progresoTotal / condicionesValidas : 1f;
        }

        /// <summary>
        /// Obtiene las descripciones de las condiciones de un nodo.
        /// </summary>
        public List<string> GetDescripcionesCondiciones(int indiceNodo)
        {
            var descripciones = new List<string>();
            
            // Requisito de trait anterior
            if (indiceNodo > 0)
            {
                descripciones.Add($"Requiere: {GetTraitNombre(indiceNodo - 1)}");
            }
            
            // Condiciones del nodo
            var condiciones = GetCondicionesAcumuladas(indiceNodo);
            foreach (var cond in condiciones)
            {
                if (cond != null)
                {
                    descripciones.Add(cond.GetDescripcion());
                }
            }
            
            return descripciones;
        }

        /// <summary>
        /// Valida la configuración de la cadena en el editor.
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(idBase))
            {
                Debug.LogWarning($"TraitChain '{name}' necesita un ID base.");
            }

            if (nodos == null || nodos.Count == 0)
            {
                Debug.LogWarning($"TraitChain '{name}' no tiene nodos definidos.");
            }

            // Verificar sufijos únicos
            var sufijos = new HashSet<string>();
            if (nodos != null)
            {
                foreach (var nodo in nodos)
                {
                    if (string.IsNullOrEmpty(nodo.sufijo))
                    {
                        Debug.LogWarning($"TraitChain '{name}' tiene un nodo sin sufijo.");
                        continue;
                    }
                    if (sufijos.Contains(nodo.sufijo))
                    {
                        Debug.LogWarning($"TraitChain '{name}' tiene sufijos duplicados: {nodo.sufijo}");
                    }
                    sufijos.Add(nodo.sufijo);
                }
            }
        }
    }
}
