using UnityEngine;
using Flags;
using Padres;

[CreateAssetMenu(fileName = "Nuevo Enemigo", menuName = "Combate/Enemigo Data")]
public class EnemigoData : ScriptableObject
{
    [Header("Info General")]
    public string nombreEnemigo;
    public string tipoEnemigo; // "Goblin", "Orco", "Dragon", etc.
    
    [Header("Stats Base")]
    public int vidaBase = 100;
    public int ataqueBase = 10;
    public float defensaBase = 5f;
    public int velocidadBase = 30;
    public int nivelBase = 1;
    
    [Header("Recompensas")]
    public float xpOtorgada = 50f;
    
    [Header("Atributos")]
    public ElementAttribute atributos;
    public TipoEntidades tipoEntidad;
    public CombatStyle estiloCombate;

    [Header("Visual")]
    public AnimatorOverrideController animatorOverride;
    public HabilidadData HabilidadPorDefecto;
    
    // Método factory - acá decidís qué clase instanciar
    public Enemigos CrearInstancia()
    {
        return tipoEnemigo switch
        {
            "Goblin" => new Subclases.Goblin(this),
            "Orcos" => new Subclases.Orcos(this),
            "Dragon" => new Subclases.Dragon(this),
            _ => throw new System.Exception($"Tipo de enemigo '{tipoEnemigo}' no implementado")
        };
    }
}