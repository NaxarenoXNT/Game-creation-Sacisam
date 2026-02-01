using UnityEngine;
using Padres;
using Habilidades;

/// <summary>
/// Efecto pasivo que se activa cuando el portador ataca o es atacado.
/// Ejemplo: Al golpear, 20% de robar 5 HP. Al ser golpeado, 10% de contraatacar.
/// </summary>
[System.Serializable]
public class TriggerCombateEffect : IPasivaEffect
{
    public enum TipoTrigger
    {
        AlGolpear,      // Se activa cuando el portador hace daño
        AlSerGolpeado,  // Se activa cuando el portador recibe daño
        AlMatar,        // Se activa cuando el portador mata a un enemigo
        AlCurar         // Se activa cuando el portador cura
    }

    public enum TipoEfectoTrigger
    {
        CurarVida,          // Cura HP al portador
        DanoAdicional,      // Hace daño adicional
        AplicarEstado,      // Aplica un estado al objetivo
        ReducirCooldown     // Reduce cooldown de habilidades
    }

    [Tooltip("Cuándo se activa el efecto")]
    public TipoTrigger trigger = TipoTrigger.AlGolpear;

    [Tooltip("Probabilidad de activación (0-100)")]
    [Range(0f, 100f)]
    public float probabilidad = 100f;

    [Tooltip("Qué hace cuando se activa")]
    public TipoEfectoTrigger efectoTrigger = TipoEfectoTrigger.CurarVida;

    [Tooltip("Valor del efecto (HP a curar, daño adicional, turnos de estado, etc.)")]
    public float valorEfecto = 10f;

    [Tooltip("Si el valor es porcentaje del daño/curación realizada")]
    public bool usaPorcentajeDelDano = false;

    // Nota: Para que esto funcione, necesitamos conectarlo al sistema de eventos de Entidad
    // La entidad debe notificar cuando golpea/es golpeada/mata/cura

    public void Aplicar(Entidad portador)
    {
        // Suscribirse a eventos de la entidad
        // TODO: Conectar cuando Entidad tenga los eventos apropiados
        // portador.OnDanoRealizado += OnDanoRealizado;
        // portador.OnDañoRecibido += OnDañoRecibido;
        
        Debug.Log($"   [Pasiva Trigger]: {portador.Nombre_Entidad} - {ObtenerDescripcion()}");
    }

    public void Remover(Entidad portador)
    {
        // Desuscribirse de eventos
        // portador.OnDanoRealizado -= OnDanoRealizado;
        // portador.OnDañoRecibido -= OnDañoRecibido;
    }

    public void ProcesarTurno(Entidad portador)
    {
        // Los triggers no hacen nada por turno, solo reaccionan a eventos
    }

    /// <summary>
    /// Ejecuta el efecto del trigger. Llamar desde el sistema de combate.
    /// </summary>
    public void EjecutarTrigger(Entidad portador, Entidad otro, int valorBase)
    {
        // Verificar probabilidad
        if (Random.Range(0f, 100f) > probabilidad)
            return;

        float valorFinal = valorEfecto;
        if (usaPorcentajeDelDano)
        {
            valorFinal = valorBase * (valorEfecto / 100f);
        }

        switch (efectoTrigger)
        {
            case TipoEfectoTrigger.CurarVida:
                int curado = portador.Curar((int)valorFinal);
                Debug.Log($"   [Trigger]: {portador.Nombre_Entidad} roba {curado} HP!");
                break;
                
            case TipoEfectoTrigger.DanoAdicional:
                if (otro != null && otro.EstaVivo())
                {
                    otro.RecibirDanoPuro((int)valorFinal, Flags.ElementAttribute.None);
                    Debug.Log($"   [Trigger]: {otro.Nombre_Entidad} recibe {valorFinal} daño adicional!");
                }
                break;
                
            // Implementar otros efectos según necesidad
        }
    }

    public string ObtenerDescripcion()
    {
        string triggerText = trigger switch
        {
            TipoTrigger.AlGolpear => "Al golpear",
            TipoTrigger.AlSerGolpeado => "Al ser golpeado",
            TipoTrigger.AlMatar => "Al matar",
            TipoTrigger.AlCurar => "Al curar",
            _ => ""
        };

        string efectoText = efectoTrigger switch
        {
            TipoEfectoTrigger.CurarVida => $"cura {valorEfecto}{(usaPorcentajeDelDano ? "% del daño" : " HP")}",
            TipoEfectoTrigger.DanoAdicional => $"hace {valorEfecto}{(usaPorcentajeDelDano ? "% daño adicional" : " daño")}",
            TipoEfectoTrigger.AplicarEstado => $"aplica estado",
            TipoEfectoTrigger.ReducirCooldown => $"reduce cooldown en {valorEfecto}",
            _ => ""
        };

        string prob = probabilidad < 100 ? $" ({probabilidad}% prob)" : "";
        return $"{triggerText}: {efectoText}{prob}";
    }
}
