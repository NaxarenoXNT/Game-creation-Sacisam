using Interfaces;
using Padres;
using Flags;
using UnityEngine;
using Habilidades;
using System.Collections.Generic;

/// <summary>
/// Efecto que aplica un estado alterado al objetivo (veneno, aturdimiento, etc.)
/// </summary>
[System.Serializable]
public class StatusEffect : IHabilidadEffect
{
    [Tooltip("Tipo de estado a aplicar")]
    public StatusFlag statusAplicar;
    
    [Tooltip("Duracion en turnos")]
    public int duracionTurnos = 3;
    
    [Tooltip("Dano por turno (para veneno, quemado)")]
    public int danoPorTurno = 0;
    
    [Tooltip("Modificador de stats (0.2 = -20% velocidad)")]
    [Range(0f, 1f)]
    public float modificadorStats = 0f;

    public void Aplicar(Entidad invocador, Entidad objetivo, List<IEntidadCombate> aliados, List<IEntidadCombate> enemigos)
    {
        if (objetivo == null || !objetivo.EstaVivo()) return;

        objetivo.AplicarEstado(statusAplicar, duracionTurnos, danoPorTurno, modificadorStats);

        string infoExtra = "";
        if (danoPorTurno > 0)
            infoExtra += " (" + danoPorTurno + " dano/turno)";
        if (modificadorStats > 0)
            infoExtra += " (-" + (modificadorStats * 100) + "% stats)";

        Debug.Log("[Estado]: " + objetivo.Nombre_Entidad + " -> " + statusAplicar + " x" + duracionTurnos + " turnos" + infoExtra);
    }
}