using Interfaces;
using Padres;
using Flasgs;
using UnityEngine; 
using System.Collections.Generic;
using Habilidades;

[System.Serializable]
public class DamageEffect : IAbilidadEffect
{
    [Tooltip("Daño base que la habilidad aplica (antes de stats).")]
    public int baseDamage = 10;
    
    [Tooltip("Tipo de daño (Físico, Fuego, etc.).")]
    public ElementAttribute tipoDaño = ElementAttribute.Fire;

    // Asumimos que necesitas un constructor para crear el efecto en el Inspector
    public DamageEffect() { }

    public void Aplicar(Entidad invocador, Entidad objetivo, List<IEntidadCombate> aliados, List<IEntidadCombate> enemigos)
    {
        if (objetivo == null || !objetivo.EstaVivo()) return;

        // 1. Cálculo de Daño Bruto (aquí se aplica el ATK del invocador)
        // Usamos el ATK actual de la Entidad, que incluye bonos de EntityStats.
        int dañoBruto = baseDamage + invocador.PuntosDeAtaque_Entidad;

        // 2. Aplicar el daño al objetivo.
        // La Entidad (el objetivo) es responsable de calcular la mitigación
        // por defensa, resistencias o inmunidades de facción.
        objetivo.RecibirDaño(dañoBruto, tipoDaño); 
        
        Debug.Log($"   [Efecto Daño]: {objetivo.Nombre_Entidad} recibe {dañoBruto} de {tipoDaño}.");
    }
}