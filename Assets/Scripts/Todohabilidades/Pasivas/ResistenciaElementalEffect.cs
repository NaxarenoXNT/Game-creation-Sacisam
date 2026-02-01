using UnityEngine;
using Padres;
using Habilidades;
using Flags;

/// <summary>
/// Efecto pasivo que otorga resistencia o vulnerabilidad a un elemento.
/// Ejemplo: +25% resistencia a Fuego, -10% resistencia a Agua.
/// </summary>
[System.Serializable]
public class ResistenciaElementalEffect : IPasivaEffect
{
    [Tooltip("Elemento afectado")]
    public ElementAttribute elemento = ElementAttribute.Fire;

    [Tooltip("Porcentaje de resistencia (positivo = resistencia, negativo = vulnerabilidad)")]
    [Range(-100f, 100f)]
    public float porcentajeResistencia = 25f;

    // Nota: La implementación real requiere que Entidad tenga un sistema de resistencias.
    // Por ahora guardamos la referencia para cuando se implemente.
    
    public void Aplicar(Entidad portador)
    {
        // TODO: Cuando Entidad tenga sistema de resistencias, modificar aquí
        // portador.ModificarResistencia(elemento, porcentajeResistencia);
        
        string tipo = porcentajeResistencia >= 0 ? "resistencia" : "vulnerabilidad";
        Debug.Log($"   [Pasiva Resistencia]: {portador.Nombre_Entidad} +{porcentajeResistencia}% {tipo} a {elemento}");
    }

    public void Remover(Entidad portador)
    {
        // TODO: portador.ModificarResistencia(elemento, -porcentajeResistencia);
    }

    public void ProcesarTurno(Entidad portador)
    {
        // Las resistencias no hacen nada por turno
    }

    public string ObtenerDescripcion()
    {
        string signo = porcentajeResistencia >= 0 ? "+" : "";
        string tipo = porcentajeResistencia >= 0 ? "resistencia" : "vulnerabilidad";
        return $"{signo}{porcentajeResistencia}% {tipo} a {elemento}";
    }
}
