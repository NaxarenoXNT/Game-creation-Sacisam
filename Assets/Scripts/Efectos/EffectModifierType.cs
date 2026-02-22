namespace Efectos
{
    /// <summary>
    /// Identificador del tipo de lógica que ejecuta un efecto.
    /// Es la clave de binding entre EffectDefinitionSO y la clase concreta en EffectModifierRegistry.
    /// Agregar un valor nuevo aquí más una clase modifier correspondiente es todo lo que se necesita
    /// para introducir un nuevo tipo de efecto.
    /// </summary>
    public enum EffectModifierType
    {
        None = 0,

        // ── Daño por turno ──────────────────────────────────────────
        Bleed,       // Sangrado: % del ATK del atacante en el momento de aplicación
        Poison,      // Veneno: daño fijo por turno, se renueva sin stackear
        Burn,        // Quemado: daño % escalado con elemento Fire del source

        // ── Control ─────────────────────────────────────────────────
        Stun,        // Aturdido: impide actuar, no stackeable
        Freeze,      // Congelado: impide actuar + reduce velocidad a 0

        // ── Debuffs de stats ────────────────────────────────────────
        Weaken,      // Debilitar: reduce ATK %
        Slow,        // Lento: reduce velocidad %
        Vulnerable,  // Vulnerable: aumenta el daño recibido (modificador en el pipeline)
        Blind,       // Cegado: reduce precisión/crit del afectado

        // ── Buffs ───────────────────────────────────────────────────
        Regeneration,// Regeneración: cura % vida max por turno
        Shield,      // Escudo: absorbe daño plano antes de la vida
        Haste,       // Velocidad aumentada

        // ── Especiales ──────────────────────────────────────────────
        Curse,       // Maldición (Dark): reduce stats múltiples
        Electrified, // Electrificado: reacciona con Water → Shock (daño extra)
        Wet,         // Empapado (Water): multiplica daño Electric recibido
    }
}
