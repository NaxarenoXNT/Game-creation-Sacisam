using UnityEngine;
using Padres;
using Habilidades;
using Flags;

/// <summary>
/// Efecto pasivo que regenera HP o un recurso cada turno.
/// </summary>
[System.Serializable]
public class RegeneracionEffect : IPasivaEffect
{
    public enum TipoRegeneracion
    {
        Vida,
        Mana,
        Energia
        // Agregar más según TipoRecurso
    }

    [Tooltip("Qué regenerar")]
    public TipoRegeneracion tipo = TipoRegeneracion.Vida;

    [Tooltip("Cantidad a regenerar por turno")]
    public float cantidad = 5f;

    [Tooltip("Si es true, la cantidad es un porcentaje del máximo")]
    public bool usaPorcentaje = false;

    public void Aplicar(Entidad portador)
    {
        // La regeneración no hace nada al aplicarse, solo por turno
        Debug.Log($"   [Pasiva Regen]: {portador.Nombre_Entidad} regenerará {ObtenerDescripcion()}");
    }

    public void Remover(Entidad portador)
    {
        // Nada que revertir
    }

    public void ProcesarTurno(Entidad portador)
    {
        float cantidadReal = cantidad;

        if (tipo == TipoRegeneracion.Vida)
        {
            if (usaPorcentaje)
            {
                cantidadReal = portador.Vida_Entidad * (cantidad / 100f);
            }

            int curado = portador.Curar((int)cantidadReal);
            if (curado > 0)
            {
                Debug.Log($"   [Regen]: {portador.Nombre_Entidad} regenera {curado} HP");
            }
        }
        else if (tipo == TipoRegeneracion.Mana || tipo == TipoRegeneracion.Energia)
        {
            // Usar IRecursoProvider si está disponible
            if (portador is Interfaces.IRecursoProvider provider)
            {
                TipoRecurso recurso = tipo == TipoRegeneracion.Mana ? TipoRecurso.Mana : TipoRecurso.Energia;
                
                if (usaPorcentaje)
                {
                    cantidadReal = provider.ObtenerRecursoMaximo(recurso) * (cantidad / 100f);
                }

                provider.RestaurarRecurso(recurso, cantidadReal);
                Debug.Log($"   [Regen]: {portador.Nombre_Entidad} regenera {cantidadReal} {tipo}");
            }
        }
    }

    public string ObtenerDescripcion()
    {
        string sufijo = usaPorcentaje ? "%" : "";
        return $"+{cantidad}{sufijo} {tipo}/turno";
    }
}
