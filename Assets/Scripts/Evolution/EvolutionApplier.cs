using Flags;
using Padres;

namespace Evolution
{
    /// <summary>
    /// Aplica efectos atómicos a la entidad. Completar con tu lógica concreta.
    /// </summary>
    public class EvolutionApplier
    {
        public void Aplicar(EvolutionEffect efecto, Jugador jugador)
        {
            switch (efecto.tipo)
            {
                case EvolutionEffectType.AddStatFlat:
                    AplicarStatFlat(jugador, efecto);
                    break;
                case EvolutionEffectType.AddStatPercent:
                    AplicarStatPercent(jugador, efecto);
                    break;
                case EvolutionEffectType.AddAbility:
                    // TODO: Añadir habilidad al pool del jugador
                    break;
                case EvolutionEffectType.AddElement:
                    // TODO: Combinar atributo elemental del jugador con efecto.elemento
                    break;
                case EvolutionEffectType.AddStatusPassive:
                    // TODO: Aplicar status pasivo persistente
                    break;
                case EvolutionEffectType.KarmaDelta:
                    // TODO: Ajustar karma en EvolutionState
                    break;
                case EvolutionEffectType.ReputationDelta:
                    // TODO: Ajustar reputación de facción
                    break;
                case EvolutionEffectType.WorldRuleToggle:
                    // TODO: Marcar regla de mundo
                    break;
                case EvolutionEffectType.AITargetBias:
                    // TODO: Ajustar bias de IA global
                    break;
                case EvolutionEffectType.LootTableBias:
                    // TODO: Ajustar peso en tablas de drop
                    break;
                case EvolutionEffectType.TagAdd:
                    // Manejar en EvolutionState
                    break;
                case EvolutionEffectType.ModifyCooldowns:
                    // TODO: Ajustar cooldowns activos/base
                    break;
            }
        }

        private void AplicarStatFlat(Jugador jugador, EvolutionEffect e)
        {
            // Usamos los métodos internos de Entidad para respetar encapsulación.
            switch (e.stat)
            {
                case TargetStat.HP:
                    int oldMaxHp = jugador.Vida_Entidad;
                    int newMaxHp = oldMaxHp + (int)e.valor;
                    jugador.ActualizarStat(StatType.VidaMaxima, newMaxHp);
                    int falta = newMaxHp - jugador.VidaActual_Entidad;
                    if (falta > 0) jugador.Curar(falta);
                    break;
                case TargetStat.Attack:
                    jugador.ActualizarStat(StatType.Ataque, jugador.PuntosDeAtaque_Entidad + (int)e.valor);
                    break;
                case TargetStat.Defense:
                    jugador.ActualizarStat(StatType.Defensa, jugador.PuntosDeDefensa_Entidad + e.valor);
                    break;
                case TargetStat.Speed:
                    jugador.ActualizarStat(StatType.Velocidad, jugador.Velocidad + (int)e.valor);
                    break;
                case TargetStat.Mana:
                    jugador.AjustarMana((int)e.valor);
                    break;
            }
        }

        private void AplicarStatPercent(Jugador jugador, EvolutionEffect e)
        {
            float mult = 1f + e.valor;
            switch (e.stat)
            {
                case TargetStat.HP:
                    int oldHp = jugador.Vida_Entidad;
                    int newHp = (int)(oldHp * mult);
                    jugador.ActualizarStat(StatType.VidaMaxima, newHp);
                    int falta = newHp - jugador.VidaActual_Entidad;
                    if (falta > 0) jugador.Curar(falta);
                    break;
                case TargetStat.Attack:
                    jugador.ActualizarStat(StatType.Ataque, (int)(jugador.PuntosDeAtaque_Entidad * mult));
                    break;
                case TargetStat.Defense:
                    jugador.ActualizarStat(StatType.Defensa, jugador.PuntosDeDefensa_Entidad * mult);
                    break;
                case TargetStat.Speed:
                    jugador.ActualizarStat(StatType.Velocidad, (int)(jugador.Velocidad * mult));
                    break;
                case TargetStat.Mana:
                    jugador.AjustarManaPercent(mult);
                    break;
            }
        }
    }
}
