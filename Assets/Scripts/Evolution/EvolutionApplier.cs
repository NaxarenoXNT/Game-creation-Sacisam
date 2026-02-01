using Flags;
using Padres;
using UnityEngine;

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
                    AplicarAddAbility(jugador, efecto);
                    break;
                case EvolutionEffectType.AddPassive:
                    AplicarAddPassive(jugador, efecto);
                    break;
                case EvolutionEffectType.RemoveAbility:
                    AplicarRemoveAbility(jugador, efecto);
                    break;
                case EvolutionEffectType.RemovePassive:
                    AplicarRemovePassive(jugador, efecto);
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

        #region Habilidades Activas
        
        private void AplicarAddAbility(Jugador jugador, EvolutionEffect e)
        {
            if (e.habilidad == null)
            {
                Debug.LogWarning("[EvolutionApplier] AddAbility: No hay habilidad configurada");
                return;
            }

            if (jugador.GestorHabilidades == null)
            {
                Debug.LogError("[EvolutionApplier] El jugador no tiene GestorHabilidades inicializado");
                return;
            }

            bool exito = jugador.GestorHabilidades.AgregarHabilidad(e.habilidad);
            if (exito)
            {
                Debug.Log($"[Evolution] {jugador.Nombre_Entidad} aprendió: {e.habilidad.nombreHabilidad}");
            }
        }

        private void AplicarRemoveAbility(Jugador jugador, EvolutionEffect e)
        {
            if (jugador.GestorHabilidades == null) return;

            // Intentar por referencia directa primero
            if (e.habilidad != null)
            {
                jugador.GestorHabilidades.RemoverHabilidad(e.habilidad);
                Debug.Log($"[Evolution] {jugador.Nombre_Entidad} olvidó: {e.habilidad.nombreHabilidad}");
            }
            // O por ID/nombre
            else if (!string.IsNullOrEmpty(e.habilidadId))
            {
                jugador.GestorHabilidades.RemoverHabilidad(e.habilidadId);
                Debug.Log($"[Evolution] {jugador.Nombre_Entidad} olvidó: {e.habilidadId}");
            }
        }

        #endregion

        #region Habilidades Pasivas

        private void AplicarAddPassive(Jugador jugador, EvolutionEffect e)
        {
            if (e.pasiva == null)
            {
                Debug.LogWarning("[EvolutionApplier] AddPassive: No hay pasiva configurada");
                return;
            }

            if (jugador.GestorPasivas == null)
            {
                Debug.LogError("[EvolutionApplier] El jugador no tiene GestorPasivas inicializado");
                return;
            }

            bool exito = jugador.GestorPasivas.AgregarPasiva(e.pasiva);
            if (exito)
            {
                Debug.Log($"[Evolution] {jugador.Nombre_Entidad} obtuvo pasiva: {e.pasiva.nombrePasiva}");
            }
        }

        private void AplicarRemovePassive(Jugador jugador, EvolutionEffect e)
        {
            if (jugador.GestorPasivas == null) return;

            if (e.pasiva != null)
            {
                jugador.GestorPasivas.RemoverPasiva(e.pasiva);
                Debug.Log($"[Evolution] {jugador.Nombre_Entidad} perdió pasiva: {e.pasiva.nombrePasiva}");
            }
        }

        #endregion

        #region Stats

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

        #endregion
    }
}
