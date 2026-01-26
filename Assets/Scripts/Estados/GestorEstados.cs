using System;
using System.Collections.Generic;
using System.Linq;
using Flags;
using UnityEngine;

/// <summary>
/// Gestor de estados activos para una entidad.
/// </summary>
[System.Serializable]
public class GestorEstados
{
    [SerializeField]
    private List<EstadoActivo> estadosActivos = new List<EstadoActivo>();
    
    public event Action<EstadoActivo> OnEstadoAplicado;
    public event Action<StatusFlag> OnEstadoExpirado;
    public event Action<int, StatusFlag> OnDanoPorEstado;
    
    public IReadOnlyList<EstadoActivo> EstadosActivos => estadosActivos.AsReadOnly();
    public bool EstaIncapacitado => estadosActivos.Any(e => e.ImpidenActuar);
    
    public StatusFlag EstadosActualesFlag
    {
        get
        {
            StatusFlag resultado = StatusFlag.None;
            foreach (var estado in estadosActivos)
            {
                resultado |= estado.tipo;
            }
            return resultado;
        }
    }
    
    public void AplicarEstado(StatusFlag tipo, int duracion, int danoPorTurno = 0, float modificador = 0f)
    {
        if (tipo == StatusFlag.None || duracion <= 0) return;
        
        var estadoExistente = estadosActivos.Find(e => e.tipo == tipo);
        
        if (estadoExistente != null)
        {
            if (duracion > estadoExistente.turnosRestantes)
            {
                estadoExistente.turnosRestantes = duracion;
                Debug.Log("Estado " + tipo + " renovado: " + duracion + " turnos");
            }
            if (danoPorTurno > estadoExistente.danoPorTurno)
            {
                estadoExistente.danoPorTurno = danoPorTurno;
            }
        }
        else
        {
            var nuevoEstado = new EstadoActivo(tipo, duracion, danoPorTurno, modificador);
            nuevoEstado.OnEstadoExpirado += ManejarEstadoExpirado;
            estadosActivos.Add(nuevoEstado);
            
            OnEstadoAplicado?.Invoke(nuevoEstado);
            Debug.Log("Nuevo estado aplicado: " + tipo + " por " + duracion + " turnos");
        }
    }
    
    public bool RemoverEstado(StatusFlag tipo)
    {
        var estado = estadosActivos.Find(e => e.tipo == tipo);
        if (estado != null)
        {
            estado.OnEstadoExpirado -= ManejarEstadoExpirado;
            estadosActivos.Remove(estado);
            OnEstadoExpirado?.Invoke(tipo);
            Debug.Log("Estado " + tipo + " removido");
            return true;
        }
        return false;
    }
    
    public void LimpiarTodosLosEstados()
    {
        foreach (var estado in estadosActivos)
        {
            estado.OnEstadoExpirado -= ManejarEstadoExpirado;
        }
        estadosActivos.Clear();
    }
    
    public int ProcesarInicioTurno()
    {
        int danoTotal = 0;
        var estadosARemover = new List<EstadoActivo>();
        
        foreach (var estado in estadosActivos)
        {
            int dano = estado.ProcesarTurno();
            
            if (dano > 0)
            {
                danoTotal += dano;
                OnDanoPorEstado?.Invoke(dano, estado.tipo);
                Debug.Log(estado.tipo + " causa " + dano + " de dano");
            }
            
            if (estado.HaExpirado)
            {
                estadosARemover.Add(estado);
            }
        }
        
        foreach (var estado in estadosARemover)
        {
            estado.OnEstadoExpirado -= ManejarEstadoExpirado;
            estadosActivos.Remove(estado);
        }
        
        return danoTotal;
    }
    
    public bool TieneEstado(StatusFlag tipo) => estadosActivos.Any(e => e.tipo == tipo);
    
    public EstadoActivo ObtenerEstado(StatusFlag tipo) => estadosActivos.Find(e => e.tipo == tipo);
    
    public float ObtenerModificadorVelocidad()
    {
        float modificador = 1f;
        
        foreach (var estado in estadosActivos)
        {
            if (estado.tipo == StatusFlag.Congelado)
                return 0f;
            modificador *= (1f - estado.modificadorStats);
        }
        
        return Mathf.Max(0.1f, modificador);
    }
    
    private void ManejarEstadoExpirado(StatusFlag tipo)
    {
        OnEstadoExpirado?.Invoke(tipo);
        Debug.Log("Estado " + tipo + " ha expirado");
    }
    
    public override string ToString()
    {
        if (estadosActivos.Count == 0) return "Sin estados activos";
        return string.Join(", ", estadosActivos.Select(e => e.ToString()));
    }
}
