using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Buffs
{
    /* Buffs overview
     * 
     * Just declare the buffs like they are below and the system will automatically handle them
     * Use the functions AddBuff, RemoveBuff, HasBuff and BuffCount in charactermaster
     * 
     * For example in Surge.cs:
     * if (!characterMaster.HasBuff(Buffs.BladeSurge))
     * ^^^ Checks that a blade surge is not already active before casting
     * 
     * On BladeSurgeGroup.cs start:
     * owner.AddBuff(Buffs.BladeSurge);
     * ^^^^ Adds blade surge buff stopping another blade surge from being created due to above check
     * 
     * In BladeSurgeGroup.cs when group is fired or destroyed
     * owner.RemoveBuff(Buffs.BladeSurge);
     * ^^^^ Removes the buff so another blade surge can be created
     * 
     * Buffs are automagically synced over the network in case you need that
     * I fucking hate the term automagically
     * 
     * I still need to add support for damage over time ticks on the player for fire etc and timed buffs (AddTimedBuff function)
     * 
     * Honestly your rupture system has advanced to the point where I'm not even sure that the buff system is necessary for it since you have references to all the blades already but give it a go if you want
     * 
     */

    // Example for your rupture thingy
    [BuffInfoAttribute(stackable = false)]
    public static BuffDef BladeRupture;

    [BuffInfoAttribute(stackable = false)]
    public static BuffDef CosmicBlades;

    [BuffInfoAttribute(stackable = false)]
    public static BuffDef BladeSurge;

    [BuffInfoAttribute(stackable = true)]
    public static BuffDef Burning;

    public static BuffDef ComboLightning;

    public static BuffDef Poison;

    [BuffInfoAttribute(stackable = false)]
    public static BuffDef EdenBoundActivated;

    public static BuffDef LeechRegen;


    //Curses
    [BuffInfoAttribute(buffType = BuffDef.BuffType.Curse, description = "Max Health Halved")]
    public static BuffDef HalfMaxHealth;

    [BuffInfoAttribute(buffType = BuffDef.BuffType.Curse, description = "AP Gain Reduced")]
    public static BuffDef QuarterAPOnHit;

    //Needs to be supported first
    [BuffInfoAttribute(buffType = BuffDef.BuffType.Curse, description = "Max AP Reduced")]
    public static BuffDef HalfMaxAP;

    [BuffInfoAttribute(buffType = BuffDef.BuffType.Curse, description = "Damaged by Jumping")]
    public static BuffDef DamageOnJumps;

    [BuffInfoAttribute(buffType = BuffDef.BuffType.Curse, description = "Burn on Spell Cast")]
    public static BuffDef BurnOnCast;

    [BuffInfoAttribute(buffType = BuffDef.BuffType.Curse, description = "Healing Time Increased")]
    public static BuffDef IncreasedHealingTime;

    //Needs to be supported first
    //[BuffInfoAttribute(buffType = BuffDef.BuffType.Curse)]
    //public static BuffDef IncreaseCastTime;

    [BuffInfoAttribute(buffType = BuffDef.BuffType.Curse, description = "Poisoned on Dash")]
    public static BuffDef PoisonOnDash;

    [BuffInfoAttribute(buffType = BuffDef.BuffType.Curse, description = "Poisoned On Roll")]
    public static BuffDef PoisonOnRoll;
}
