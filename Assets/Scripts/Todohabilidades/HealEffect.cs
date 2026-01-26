using Interfaces;
using Padres;
using Flags;
using UnityEngine;
using Habilidades;
using System.Collections.Generic;

[System.Serializable]
public class HealEffect : IHabilidadEffect
{
    [Tooltip("Cantidad fija de vida a curar.")]
    public int curacionBase = 25;
    
    [Tooltip("¿El objetivo debe ser el invocador (Self) o un aliado?")]
    public TargetType targetType = TargetType.Self;

    public void Aplicar(Entidad invocador, Entidad objetivo, List<IEntidadCombate> aliados, List<IEntidadCombate> enemigos)
    {
        Entidad target = null;

        if (targetType == TargetType.Self)
        {
            target = invocador;
        }
        else if (targetType == TargetType.AliadoUnico)
        {
            // Nota: Aquí se usa el objetivo elegido en el Command (que debería ser un aliado)
            target = objetivo;
        }
        
        if (target == null || !target.EstaVivo()) return;

        // 1. Aplicar la curación
        int vidaCurada = target.Curar(curacionBase);

        Debug.Log($"   [Efecto Curación]: {target.Nombre_Entidad} se cura {vidaCurada} HP.");
    }
}