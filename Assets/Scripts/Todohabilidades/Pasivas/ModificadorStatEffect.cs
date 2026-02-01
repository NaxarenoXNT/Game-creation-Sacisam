using UnityEngine;
using Padres;
using Habilidades;

/// <summary>
/// Efecto pasivo que modifica una estadística del portador.
/// Ejemplo: +20% ATK, +50 HP máximo, etc.
/// </summary>
[System.Serializable]
public class ModificadorStatEffect : IPasivaEffect
{
    public enum TipoStat
    {
        Vida,
        Ataque,
        Defensa,
        Velocidad
    }

    public enum TipoModificador
    {
        Plano,      // +50 ATK
        Porcentaje  // +20% ATK
    }

    [Tooltip("Stat a modificar")]
    public TipoStat stat = TipoStat.Ataque;

    [Tooltip("Tipo de modificación")]
    public TipoModificador tipo = TipoModificador.Porcentaje;

    [Tooltip("Valor del modificador (puede ser negativo para debuffs)")]
    public float valor = 10f;

    // Guardamos el valor aplicado para poder revertirlo
    [System.NonSerialized]
    private int valorAplicado = 0;

    public void Aplicar(Entidad portador)
    {
        valorAplicado = CalcularModificacion(portador);
        AplicarModificacion(portador, valorAplicado);
        
        Debug.Log($"   [Pasiva Stat]: {portador.Nombre_Entidad} {stat} {(valor >= 0 ? "+" : "")}{valor}{(tipo == TipoModificador.Porcentaje ? "%" : "")}");
    }

    public void Remover(Entidad portador)
    {
        // Revertir la modificación
        AplicarModificacion(portador, -valorAplicado);
        valorAplicado = 0;
    }

    public void ProcesarTurno(Entidad portador)
    {
        // Los modificadores de stats no hacen nada por turno
    }

    private int CalcularModificacion(Entidad portador)
    {
        if (tipo == TipoModificador.Plano)
        {
            return (int)valor;
        }
        else // Porcentaje
        {
            float baseValue = stat switch
            {
                TipoStat.Vida => portador.Vida_Entidad,
                TipoStat.Ataque => portador.PuntosDeAtaque_Entidad,
                TipoStat.Defensa => portador.PuntosDeDefensa_Entidad,
                TipoStat.Velocidad => portador.Velocidad,
                _ => 0
            };
            return (int)(baseValue * (valor / 100f));
        }
    }

    private void AplicarModificacion(Entidad portador, int cantidad)
    {
        switch (stat)
        {
            case TipoStat.Vida:
                portador.ModificarVidaMaxima(cantidad);
                // También aumentar vida actual si es positivo
                if (cantidad > 0)
                    portador.Curar(cantidad);
                break;
            case TipoStat.Ataque:
                portador.ModificarAtaque(cantidad);
                break;
            case TipoStat.Defensa:
                portador.ModificarDefensa(cantidad);
                break;
            case TipoStat.Velocidad:
                portador.ModificarVelocidad(cantidad);
                break;
        }
    }

    public string ObtenerDescripcion()
    {
        string signo = valor >= 0 ? "+" : "";
        string sufijo = tipo == TipoModificador.Porcentaje ? "%" : "";
        return $"{signo}{valor}{sufijo} {stat}";
    }
}
