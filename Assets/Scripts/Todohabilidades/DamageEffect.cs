using Interfaces;
using Padres;
using Flags;
using UnityEngine; 
using System.Collections.Generic;
using Habilidades;

/// <summary>
/// Efecto que aplica daño al objetivo.
/// Soporta daño base, escalado con stats y diferentes elementos.
/// </summary>
[System.Serializable]
public class DamageEffect : IHabilidadEffect
{
    [Tooltip("Daño base que la habilidad aplica (antes de stats).")]
    public int baseDamage = 10;
    
    [Tooltip("Tipo de daño elemental (None = daño físico puro).")]
    public ElementAttribute tipoDano = ElementAttribute.None;
    
    [Tooltip("Escala con el ataque del invocador (0 = no escala, 1 = +100% ATK, 1.5 = +150% ATK).")]
    [Range(0f, 3f)]
    public float escaladoATK = 1f;
    
    [Tooltip("Si es true, ignora la defensa del objetivo.")]
    public bool ignoraDefensa = false;
    
    [Tooltip("Si es true, baseDamage es un % de la vida actual del objetivo.")]
    public bool usaPorcentajeVidaObjetivo = false;

    public DamageEffect() { }

    public void Aplicar(Entidad invocador, Entidad objetivo, List<IEntidadCombate> aliados, List<IEntidadCombate> enemigos)
    {
        if (objetivo == null || !objetivo.EstaVivo()) return;

        // 1. Calcular daño base
        float danoCalculado = baseDamage;
        
        // Si usa porcentaje de la vida del objetivo
        if (usaPorcentajeVidaObjetivo)
        {
            danoCalculado = objetivo.VidaActual_Entidad * (baseDamage / 100f);
        }
        
        // 2. Aplicar escalado con ATK del invocador
        if (escaladoATK > 0)
        {
            danoCalculado += invocador.PuntosDeAtaque_Entidad * escaladoATK;
        }

        // 3. Aplicar el daño al objetivo
        // La Entidad es responsable de calcular mitigación por defensa y resistencias
        if (ignoraDefensa)
        {
            objetivo.RecibirDanoPuro((int)danoCalculado, tipoDano);
        }
        else
        {
            objetivo.RecibirDano((int)danoCalculado, tipoDano);
        }
        
        Debug.Log($"   [Efecto Daño]: {objetivo.Nombre_Entidad} recibe {(int)danoCalculado} de {tipoDano}.");
    }
}