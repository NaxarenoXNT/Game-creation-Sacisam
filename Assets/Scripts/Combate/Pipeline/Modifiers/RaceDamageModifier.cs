namespace Combate.Modifiers
{
    /// <summary>
    /// Aplica multiplicadores de raza/tipo de entidad al daño.
    /// Lee RaceModifiers desde el contexto. Si es null, no modifica nada.
    /// Cachea los multiplicadores en el contexto para reporting.
    /// </summary>
    public sealed class RaceDamageModifier : IDamageModifier
    {
        public int Order => 200;

        public void Modify(DamageContext context)
        {
            if (context.RaceModifiers == null) return;

            float raceAtk = context.RaceModifiers.GetAttackMultiplier(context.Attacker.TipoEntidad);
            float raceVsRace = context.RaceModifiers.GetRaceVsRaceMultiplier(
                context.Attacker.TipoEntidad,
                context.Defender.TipoEntidad);
            float raceDef = context.RaceModifiers.GetDefenseMultiplier(context.Defender.TipoEntidad);

            context.RaceAtkMultiplier = raceAtk * raceVsRace;
            context.RaceDefMultiplier = raceDef;

            // Raza afecta ambos canales de daño
            context.PhysicalDamage  *= context.RaceAtkMultiplier;
            context.ElementalDamage *= context.RaceAtkMultiplier;
        }
    }
}
