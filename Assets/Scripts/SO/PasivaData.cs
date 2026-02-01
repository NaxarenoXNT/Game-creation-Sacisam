using UnityEngine;
using System.Collections.Generic;
using Padres;
using Habilidades;
using Flags;

/// <summary>
/// ScriptableObject que define una habilidad pasiva.
/// Las pasivas siempre están activas mientras la entidad las posea.
/// No requieren activación manual, objetivo, costo ni cooldown.
/// </summary>
[CreateAssetMenu(fileName = "Nueva Pasiva", menuName = "Combate/Pasiva Data")]
public class PasivaData : ScriptableObject
{
    [Header("Info General")]
    public string nombrePasiva;
    public Sprite icono;
    [TextArea(2, 4)]
    public string descripcion;

    [Header("Categoría")]
    [Tooltip("Tipo de pasiva para organización y filtrado")]
    public CategoriaPasiva categoria = CategoriaPasiva.Estadisticas;

    [Header("Condiciones de Activación")]
    [Tooltip("Si es true, la pasiva siempre está activa. Si es false, requiere condiciones.")]
    public bool siempreActiva = true;
    
    [Tooltip("Condición para que la pasiva se active (si siempreActiva = false)")]
    public CondicionPasiva condicion = CondicionPasiva.Ninguna;
    
    [Tooltip("Valor umbral para la condición (ej: 50 para 'HP menor a 50%')")]
    [Range(0f, 100f)]
    public float valorCondicion = 50f;

    [Header("Restricciones")]
    [Tooltip("Tipos de entidad que NO pueden tener esta pasiva")]
    public List<TipoEntidades> faccionesProhibidas = new List<TipoEntidades>();

    [Header("Efectos")]
    [SerializeReference]
    public List<IPasivaEffect> efectos = new List<IPasivaEffect>();

    // Estado interno (no serializado)
    [System.NonSerialized]
    private bool _estaActiva = false;

    /// <summary>
    /// Aplica todos los efectos de la pasiva al portador.
    /// Llamar cuando la entidad obtiene la pasiva.
    /// </summary>
    public void Activar(Entidad portador)
    {
        if (_estaActiva) return;
        if (!PuedeActivarse(portador)) return;

        foreach (var efecto in efectos)
        {
            efecto?.Aplicar(portador);
        }
        
        _estaActiva = true;
        Debug.Log($"✨ Pasiva '{nombrePasiva}' activada en {portador.Nombre_Entidad}");
    }

    /// <summary>
    /// Remueve todos los efectos de la pasiva del portador.
    /// Llamar cuando la entidad pierde la pasiva.
    /// </summary>
    public void Desactivar(Entidad portador)
    {
        if (!_estaActiva) return;

        foreach (var efecto in efectos)
        {
            efecto?.Remover(portador);
        }
        
        _estaActiva = false;
        Debug.Log($"💤 Pasiva '{nombrePasiva}' desactivada en {portador.Nombre_Entidad}");
    }

    /// <summary>
    /// Procesa efectos por turno (regeneración, etc.).
    /// Llamar al inicio de cada turno del portador.
    /// </summary>
    public void ProcesarTurno(Entidad portador)
    {
        if (!_estaActiva) return;
        
        // Re-verificar condiciones cada turno
        if (!siempreActiva && !CumpleCondicion(portador))
        {
            Desactivar(portador);
            return;
        }

        foreach (var efecto in efectos)
        {
            efecto?.ProcesarTurno(portador);
        }
    }

    /// <summary>
    /// Verifica condiciones y activa/desactiva según corresponda.
    /// Llamar cuando cambia el estado del portador (HP, etc.).
    /// </summary>
    public void ActualizarEstado(Entidad portador)
    {
        if (siempreActiva) return;

        bool deberiaEstarActiva = CumpleCondicion(portador);
        
        if (deberiaEstarActiva && !_estaActiva)
        {
            Activar(portador);
        }
        else if (!deberiaEstarActiva && _estaActiva)
        {
            Desactivar(portador);
        }
    }

    /// <summary>
    /// Verifica si la pasiva puede activarse para este portador.
    /// </summary>
    public bool PuedeActivarse(Entidad portador)
    {
        if (portador == null) return false;
        
        // Verificar restricciones de facción
        if (faccionesProhibidas.Contains(portador.TipoEntidad))
            return false;

        // Verificar condición si no es siempreActiva
        if (!siempreActiva && !CumpleCondicion(portador))
            return false;

        return true;
    }

    /// <summary>
    /// Evalúa si el portador cumple la condición de activación.
    /// </summary>
    private bool CumpleCondicion(Entidad portador)
    {
        if (siempreActiva) return true;

        float porcentajeHP = (float)portador.VidaActual_Entidad / portador.Vida_Entidad * 100f;

        return condicion switch
        {
            CondicionPasiva.Ninguna => true,
            CondicionPasiva.VidaMenorQue => porcentajeHP < valorCondicion,
            CondicionPasiva.VidaMayorQue => porcentajeHP > valorCondicion,
            CondicionPasiva.VidaIgualA => Mathf.Approximately(porcentajeHP, valorCondicion),
            CondicionPasiva.VidaLlena => porcentajeHP >= 100f,
            CondicionPasiva.VidaCritica => porcentajeHP <= 25f,
            _ => true
        };
    }

    public bool EstaActiva => _estaActiva;

    /// <summary>
    /// Genera descripción completa de la pasiva.
    /// </summary>
    public string ObtenerDescripcionCompleta()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(descripcion);
        sb.AppendLine();
        
        if (!siempreActiva)
        {
            sb.AppendLine($"Condición: {ObtenerTextoCondicion()}");
        }
        
        sb.AppendLine("Efectos:");
        foreach (var efecto in efectos)
        {
            if (efecto != null)
                sb.AppendLine($"  • {efecto.ObtenerDescripcion()}");
        }
        
        return sb.ToString();
    }

    private string ObtenerTextoCondicion()
    {
        return condicion switch
        {
            CondicionPasiva.VidaMenorQue => $"HP menor a {valorCondicion}%",
            CondicionPasiva.VidaMayorQue => $"HP mayor a {valorCondicion}%",
            CondicionPasiva.VidaIgualA => $"HP igual a {valorCondicion}%",
            CondicionPasiva.VidaLlena => "HP al 100%",
            CondicionPasiva.VidaCritica => "HP crítico (≤25%)",
            _ => "Siempre activa"
        };
    }
}

/// <summary>
/// Categorías de pasivas para organización.
/// </summary>
public enum CategoriaPasiva
{
    Estadisticas,   // Modifican ATK, DEF, etc.
    Resistencias,   // Resistencias elementales
    Regeneracion,   // Regen HP/Mana por turno
    Triggers,       // Efectos al golpear/ser golpeado
    Supervivencia,  // Efectos defensivos especiales
    Ofensiva        // Efectos ofensivos especiales
}

/// <summary>
/// Condiciones para activación de pasivas condicionales.
/// </summary>
public enum CondicionPasiva
{
    Ninguna,        // Siempre activa (usar siempreActiva = true mejor)
    VidaMenorQue,   // Se activa cuando HP < X%
    VidaMayorQue,   // Se activa cuando HP > X%
    VidaIgualA,     // Se activa cuando HP = X%
    VidaLlena,      // Se activa cuando HP = 100%
    VidaCritica     // Se activa cuando HP <= 25%
}
