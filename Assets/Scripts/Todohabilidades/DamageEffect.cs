using Interfaces;
using Padres;
using Flags;
using UnityEngine; 
using System.Collections.Generic;
using Habilidades;

[System.Serializable]
public class DamageEffect : IHabilidadEffect
{
    [Tooltip("Dano base que la habilidad aplica (antes de stats).")]
    public int baseDamage = 10;
    
    [Tooltip("Tipo de dano (Fisico, Fuego, etc.).")]
    public ElementAttribute tipoDano = ElementAttribute.Fire;

    // Asumimos que necesitas un constructor para crear el efecto en el Inspector
    public DamageEffect() { }

    public void Aplicar(Entidad invocador, Entidad objetivo, List<IEntidadCombate> aliados, List<IEntidadCombate> enemigos)
    {
        if (objetivo == null || !objetivo.EstaVivo()) return;

        // 1. Calculo de Dano Bruto (aqui se aplica el ATK del invocador)
        // Usamos el ATK actual de la Entidad, que incluye bonos de EntityStats.
        int danoBruto = baseDamage + invocador.PuntosDeAtaque_Entidad;

        // 2. Aplicar el dano al objetivo.
        // La Entidad (el objetivo) es responsable de calcular la mitigacion
        // por defensa, resistencias o inmunidades de faccion.
        objetivo.RecibirDano(danoBruto, tipoDano); 
        
        Debug.Log("   [Efecto Dano]: " + objetivo.Nombre_Entidad + " recibe " + danoBruto + " de " + tipoDano + ".");
    }
}