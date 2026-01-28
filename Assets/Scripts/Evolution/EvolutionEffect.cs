using System;
using Flags;
using UnityEngine;

namespace Evolution
{
    [Serializable]
    public enum EvolutionEffectType
    {
        AddStatFlat,
        AddStatPercent,
        AddAbility,
        ModifyCooldowns,
        AddElement,
        AddStatusPassive,
        KarmaDelta,
        ReputationDelta,
        WorldRuleToggle,
        AITargetBias,
        LootTableBias,
        TagAdd
    }

    [Serializable]
    public enum TargetStat
    {
        HP,
        Attack,
        Defense,
        Speed,
        Mana
    }

    [Serializable]
    public enum CooldownTarget
    {
        All,
        ByTag,
        ByAbilityId
    }

    [Serializable]
    public enum AITargetBiasMode
    {
        Neutral,
        PreferPlayer,
        AvoidPlayer
    }

    [Serializable]
    public class EvolutionEffect
    {
        [Tooltip("Tipo de efecto a aplicar")]
        public EvolutionEffectType tipo;

        [Header("Stats")]
        public TargetStat stat;
        public float valor;

        [Header("Habilidades")]
        public HabilidadData habilidad;
        public string habilidadId;
        public string[] habilidadTags;

        [Header("Cooldowns")]
        public CooldownTarget cooldownTarget;
        public int cooldownDelta;

        [Header("Elementos/Estados")]
        public ElementAttribute elemento;
        public StatusEffect statusPasivo;

        [Header("Karma/Reputacion")]
        public float karmaDelta;
        public string faccionId;
        public float reputacionDelta;

        [Header("Mundo/IA/Loot")]
        public string worldRuleKey;
        public AITargetBiasMode aiBias;
        public string lootTableId;
        public float lootPesoExtra;

        [Header("Tags")]
        public string tagAgregar;
    }
}
