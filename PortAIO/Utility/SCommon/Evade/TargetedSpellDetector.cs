﻿using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using Color = System.Drawing.Color;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;
using SCommon.Database;

namespace SCommon.Evade
{
    public static class TargetedSpellDetector
    {
        /// <summary>
        /// OnDeceted Event delegate
        /// </summary>
        /// <param name="args">The args.</param>
        public delegate void dOnDetected(DetectedTargetedSpellArgs args);
        /// <summary>
        /// The event which fired when targeted spell is detected
        /// </summary>
        public static event dOnDetected OnDetected;

        /// <summary>
        /// Initializes TargetedSpellDetector class
        /// </summary>
        static TargetedSpellDetector()
        {
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            if(SpellDatabase.TargetedSpells == null)
                SpellDatabase.InitalizeSpellDatabase();
        }

        /// <summary>
        /// OnProcessSpellCast Event which detects targeted spells to me
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The args.</param>
        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if(OnDetected != null && sender.IsChampion() && !sender.IsMe)
            {
                var spells = SpellDatabase.TargetedSpells.Where(p => p.ChampionName == (sender as AIHeroClient).ChampionName);
                if(spells != null && spells.Count() > 0)
                {
                    var spell = spells.Where(p => p.SpellName == args.SData.Name).FirstOrDefault();
                    if (spell != null)
                    {
                        if ((spell.IsTargeted && args.Target != null && args.Target.IsMe) ||
                            (!spell.IsTargeted && sender.Distance(ObjectManager.Player.ServerPosition) <= spell.Radius))
                            OnDetected(new DetectedTargetedSpellArgs { Caster = sender, SpellData = spell, SpellCastArgs = args });
                    }
                }
            }
        }
    }

    /// <summary>
    /// DetectedTargetedSpellArgs class
    /// </summary>
    public class DetectedTargetedSpellArgs : EventArgs
    {
        public Obj_AI_Base Caster;
        public SCommon.Database.SpellData SpellData;
        public GameObjectProcessSpellCastEventArgs SpellCastArgs;
    }
}
