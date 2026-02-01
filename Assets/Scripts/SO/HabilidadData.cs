using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Interfaces;
using Padres;
using Flags;
using Habilidades;

[CreateAssetMenu(fileName = "Nueva Habilidad", menuName = "Combate/Habilidad Data")]
public class HabilidadData : ScriptableObject, IHabilidadesCommand
{
    [Header("Info General")]
    public string nombreHabilidad;
    public Sprite icono;
    [TextArea(2, 4)]
    public string descripcion;

    [Header("Categoría")]
    [Tooltip("Tipo funcional de la habilidad (para IA y UI)")]
    public CategoriaHabilidad categoria = CategoriaHabilidad.Ataque;

    [Header("Costos de Recursos")]
    [Tooltip("Lista de recursos que consume la habilidad. Dejar vacío para habilidades sin costo.")]
    public List<CostoRecurso> costosRecursos = new List<CostoRecurso>();

    [Header("Cooldown")]
    [Tooltip("Turnos de espera después de usar la habilidad")]
    [Min(0)]
    public int cooldownTurnos = 0;
    
    [Header("Restricciones")]
    [Tooltip("Tipos de entidad que NO pueden usar esta habilidad.")]
    public List<TipoEntidades> faccionesProhibidas = new List<TipoEntidades>();

    [Header("Objetivos")]
    public TargetType tipoObjetivo = TargetType.EnemigoUnico;
    
    // Lista de efectos concretos (DamageEffect, HealEffect, etc.)
    // [SerializeReference] permite que Unity guarde clases que implementan IAbilityEffect
    [Header("Efectos (Lógica Pura)")]
    [SerializeReference] 
    public List<IHabilidadEffect> efectos = new List<IHabilidadEffect>();

    // === Implementación de IHabilidadCommand ===
    
    public bool EsViable(IEntidadCombate invocador, IEntidadCombate objetivo, List<IEntidadCombate> aliados, List<IEntidadCombate> enemigos)
    {
        // Verificar si la facción está prohibida.
        if (faccionesProhibidas.Contains(invocador.TipoEntidad))
        {
            return false;
        }

        // Verificar costos de recursos (nuevo sistema flexible)
        if (!VerificarCostosRecursos(invocador))
        {
            return false;
        }
        
        // Verificar si hay objetivo y si el invocador está vivo.
        if (!invocador.EstaVivo()) return false;
        if (tipoObjetivo != TargetType.Self && objetivo == null) return false;
        
        // en caso de complejizar la habilidad, aquí se pueden añadir más verificaciones.
        return true;
    }

    /// <summary>
    /// Verifica si el invocador tiene todos los recursos necesarios para usar la habilidad.
    /// </summary>
    public bool VerificarCostosRecursos(IEntidadCombate invocador)
    {
        // Si no hay costos, siempre es viable
        if (costosRecursos == null || costosRecursos.Count == 0)
            return true;

        // Si el invocador no implementa IRecursoProvider, usar fallback a maná clásico
        if (invocador is IRecursoProvider provider)
        {
            foreach (var costo in costosRecursos)
            {
                if (!costo.EsSignificativo()) continue;
                
                float costoReal = costo.CalcularCostoReal(provider.ObtenerRecursoMaximo(costo.tipo));
                if (!provider.TieneRecursoSuficiente(costo.tipo, costoReal))
                {
                    Debug.Log($"❌ {nombreHabilidad}: Falta {costo.tipo} (necesita {costoReal})");
                    return false;
                }
            }
        }
        else if (invocador is IJugadorProgresion jugador)
        {
            // Fallback: solo verificar maná para compatibilidad con sistema antiguo
            var costoMana = costosRecursos.FirstOrDefault(c => c.tipo == TipoRecurso.Mana);
            if (costoMana != null && costoMana.EsSignificativo())
            {
                if (jugador.ManaActual_jugador < costoMana.cantidad)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Consume los recursos necesarios para usar la habilidad.
    /// Llamar después de verificar que EsViable() retorna true.
    /// </summary>
    public void ConsumirRecursos(IEntidadCombate invocador)
    {
        if (costosRecursos == null || costosRecursos.Count == 0)
            return;

        if (invocador is IRecursoProvider provider)
        {
            foreach (var costo in costosRecursos)
            {
                if (!costo.EsSignificativo()) continue;
                
                float costoReal = costo.CalcularCostoReal(provider.ObtenerRecursoMaximo(costo.tipo));
                provider.ConsumirRecurso(costo.tipo, costoReal);
                Debug.Log($"💰 Consumido: {costoReal} {costo.tipo}");
            }
        }
    }

    /// <summary>
    /// Verifica si la habilidad tiene algún costo de recurso.
    /// </summary>
    public bool TieneCosto()
    {
        return costosRecursos != null && costosRecursos.Any(c => c.EsSignificativo());
    }

    /// <summary>
    /// Obtiene una descripción legible de los costos.
    /// </summary>
    public string ObtenerDescripcionCostos()
    {
        if (!TieneCosto()) return "Sin costo";
        
        var costosSignificativos = costosRecursos.Where(c => c.EsSignificativo());
        return string.Join(" + ", costosSignificativos.Select(c => c.ToString()));
    }

    public void Ejecutar(IEntidadCombate invocadorRaw, IEntidadCombate objetivoRaw, List<IEntidadCombate> aliados, List<IEntidadCombate> enemigos)
    {
        // Aseguramos que solo aplicamos efectos si las entidades son nuestra clase base (Entidad).
        if (invocadorRaw is Entidad invocador && objetivoRaw is Entidad objetivo)
        {
            Debug.Log($"⚙️ {invocador.Nombre_Entidad} ejecutando {nombreHabilidad} sobre {objetivo.Nombre_Entidad}");
            
            // **[Punto Clave]** La habilidad simplemente aplica todos sus efectos uno por uno.
            foreach (var effect in efectos)
            {
                effect.Aplicar(invocador, objetivo, aliados, enemigos);
            }
            
            // Notificar al sistema visual DESPUÉS de ejecutar la lógica (lo haremos en el siguiente paso).
            // invocador.NotificarHabilidadEjecutada(this, objetivo); 
        }
        else
        {
            Debug.LogError("Error: Las entidades deben heredar de la clase base 'Entidad' para aplicar efectos.");
        }
    }

    public HabilidadData ObtenerDatos() => this;
}

