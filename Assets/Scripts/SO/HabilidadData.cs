using UnityEngine;
using System.Collections.Generic;
using Interfaces;
using Padres;
using Flasgs;
using Habilidades;

[CreateAssetMenu(fileName = "Nueva Habilidad", menuName = "Combate/Habilidad Data")]
public class HabilidadData : ScriptableObject, IHabilidadesCommad
{
    [Header("Info General")]
    public string nombreHabilidad;
    public Sprite icono;
    public string descripcion;

    [Header("Restricciones y Costos")]
    public float costeMana = 0;
    public float cooldownTurnos = 0;
    
    [Tooltip("Tipos de entidad que NO pueden usar esta habilidad.")]
    public List<TipoEntidades> faccionesProhibidas = new List<TipoEntidades>();

    [Header("Objetivos")]
    public TargetType tipoObjetivo = TargetType.EnemigoUnico;
    
    // Lista de efectos concretos (DamageEffect, HealEffect, etc.)
    // [SerializeReference] permite que Unity guarde clases que implementan IAbilityEffect
    [Header("Efectos (Lógica Pura)")]
    [SerializeReference] 
    public List<IAbilidadEffect> efectos = new List<IAbilidadEffect>();

    // === Implementación de IHabilidadCommand ===
    
    public bool EsViable(IEntidadCombate invocador, IEntidadCombate objetivo, List<IEntidadCombate> aliados, List<IEntidadCombate> enemigos)
    {
        // Verificar si la facción está prohibida.
        if (faccionesProhibidas.Contains(invocador.TipoEntidad))
        {
            return false;
        }

        // Verificar costo de Maná (asumiendo que IEntidadCombate tiene una forma de acceder al maná).
        if (invocador is IJugadorProgresion jugador && jugador.ManaActual_jugador < costeMana)
        {
            return false;
        }
        
        // Verificar si hay objetivo y si el invocador está vivo.
        if (!invocador.EstaVivo()) return false;
        if (tipoObjetivo != TargetType.Self && objetivo == null) return false;
        
        // en caso de complejizar la habilidad, aquí se pueden añadir más verificaciones.
        return true;
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

