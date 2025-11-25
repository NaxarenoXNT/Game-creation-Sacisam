using Interfaces;
using Padres;
using Flasgs;
using UnityEngine;
using Habilidades;
using System.Collections.Generic;

[System.Serializable]
public class StatusEffect : IAbilidadEffect
{
    public StatusFlag statusAplicar;
    public int duracionTurnos = 3;

    public void Aplicar(Entidad invocador, Entidad objetivo, List<IEntidadCombate> aliados, List<IEntidadCombate> enemigos)
    {
        if (objetivo == null || !objetivo.EstaVivo()) return;

        // Asumimos que la clase Entidad tiene un método para manejar estados
        objetivo.AplicarEstado(statusAplicar, duracionTurnos);

        Debug.Log($"   [Efecto Estado]: {objetivo.Nombre_Entidad} afectado por {statusAplicar} por {duracionTurnos} turnos.");
    }
}