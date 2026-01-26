using System;
using Flags;
using UnityEngine;

/// <summary>
/// Representa un estado de efecto activo en una entidad (veneno, aturdimiento, etc.)
/// </summary>
[System.Serializable]
public class EstadoActivo
{
    public StatusFlag tipo;
    public int turnosRestantes;
    public int danoPorTurno;
    public float modificadorStats;
    
    public event Action<StatusFlag> OnEstadoExpirado;
    
    public EstadoActivo(StatusFlag tipo, int duracion, int dano = 0, float modificador = 0f)
    {
        this.tipo = tipo;
        this.turnosRestantes = duracion;
        this.danoPorTurno = dano;
        this.modificadorStats = modificador;
    }
    
    public int ProcesarTurno()
    {
        turnosRestantes--;
        if (turnosRestantes <= 0)
        {
            OnEstadoExpirado?.Invoke(tipo);
        }
        return danoPorTurno;
    }
    
    public bool HaExpirado => turnosRestantes <= 0;
    public bool ImpidenActuar => tipo == StatusFlag.Aturdido || tipo == StatusFlag.Congelado;
    
    public Color ObtenerColor()
    {
        return tipo switch
        {
            StatusFlag.Envenenado => new Color(0.5f, 0f, 0.5f),
            StatusFlag.Aturdido => Color.yellow,
            StatusFlag.Quemado => new Color(1f, 0.5f, 0f),
            StatusFlag.Congelado => Color.cyan,
            _ => Color.white
        };
    }
    
    public override string ToString() => tipo + " (" + turnosRestantes + " turnos)";
}
