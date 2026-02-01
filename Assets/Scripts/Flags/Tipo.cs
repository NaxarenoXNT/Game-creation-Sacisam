using System;




namespace Flags
{
    [Flags]
    public enum TipoEntidades
    {
        None = 0,
        Humanoid = 1 << 0,
        Undead = 1 << 1,
        Elemental = 1 << 2,
        Beast = 1 << 3,
    }

    [Flags]
    public enum ElementAttribute
    {
        None = 0,
        Fire = 1 << 0,
        Water = 1 << 1,
        Light = 1 << 2,
        Dark = 1 << 3,
        Air = 1 << 4,
        Geo = 1 << 5,
        Electric = 1 << 6,
        BloodSpilet = 1 << 7,

    }
    
    [Flags]
    public enum StatusFlag
    {
        None = 0,
        Envenenado = 1 << 0,
        Aturdido = 1 << 1,
        Quemado = 1 << 2,
        Congelado = 1 << 3
    }

    [Flags]
    public enum CombatStyle
    {
        None = 0,
        Melee = 1 << 0,
        Ranged = 1 << 1,
        Caster = 1 << 2,
    }


    public enum TargetType
    {
        EnemigoUnico,
        EnemigoTodos,
        AliadoUnico,
        AliadoTodos,
        Self
    }


}