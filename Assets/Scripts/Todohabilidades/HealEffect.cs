using Interfaces;
using Padres;
using Flags;
using UnityEngine;
using Habilidades;
using System.Collections.Generic;

/// <summary>
/// Efecto que cura vida al objetivo.
/// El objetivo real lo determina el HabilidadData.tipoObjetivo, no este efecto.
/// </summary>
[System.Serializable]
public class HealEffect : IHabilidadEffect
{
    [Tooltip("Cantidad fija de vida a curar.")]
    public int curacionBase = 25;
    
    [Tooltip("Si es true, curacionBase es un % de la vida máxima del objetivo.")]
    public bool usaPorcentajeVidaMax = false;
    
    [Tooltip("Escala con el poder mágico/ataque del invocador (0 = no escala, 1 = 100% del stat).")]
    [Range(0f, 2f)]
    public float escaladoConStat = 0f;

    public void Aplicar(Entidad invocador, Entidad objetivo, List<IEntidadCombate> aliados, List<IEntidadCombate> enemigos)
    {
        // El objetivo ya viene determinado por HabilidadData según tipoObjetivo
        if (objetivo == null || !objetivo.EstaVivo()) return;

        // Calcular curación base
        float curacionTotal = curacionBase;
        
        // Si usa porcentaje de vida máxima
        if (usaPorcentajeVidaMax)
        {
            curacionTotal = objetivo.Vida_Entidad * (curacionBase / 100f);
        }
        
        // Aplicar escalado con stats del invocador (si está configurado)
        if (escaladoConStat > 0)
        {
            curacionTotal += invocador.PuntosDeAtaque_Entidad * escaladoConStat;
        }

        // Aplicar la curación
        int vidaCurada = objetivo.Curar((int)curacionTotal);

        Debug.Log($"   [Efecto Curación]: {objetivo.Nombre_Entidad} se cura {vidaCurada} HP.");
    }
}