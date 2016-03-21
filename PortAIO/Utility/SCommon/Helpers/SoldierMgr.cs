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
using SCommon.PluginBase;
using SharpDX;

namespace SAutoCarry.Champions.Helpers
{
    public static class SoldierMgr
    {
        public const int SoldierAttackRange = 315;

        private static SCommon.PluginBase.Champion s_Champion;
        private static List<GameObject> s_Soldiers;
        private static bool s_ProcessNextSoldier;
        private static int s_LastSoldierSpawn;

        public static Menu m;

        public static bool SoldierAttacking { get; private set; }
        public static int LastSoldierSpawn { get { return s_LastSoldierSpawn; } }
        public static List<GameObject> ActiveSoldiers { get { return s_Soldiers; } }

        public static void Initialize(SCommon.PluginBase.Champion champ)
        {
            s_Champion = champ;
            s_Soldiers = new List<GameObject>();

            m = MainMenu.AddMenu("SoldierMgr", "SAutoCarry.Helpers.SoldierMgr.Root");
            m.Add("SAutoCarry.Helpers.SoldierMgr.Root.DrawRanges", new CheckBox("Draw Soldier Range"));

            AIHeroClient.OnCreate += AIHeroClient_OnCreate;
            AIHeroClient.OnPlayAnimation += AIHeroClient_OnPlayAnimation;
            AIHeroClient.OnProcessSpellCast += AIHeroClient_OnProcessSpellCast;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        public static float GetAADamage(AIHeroClient target)
        {
            float dmg = (float)ObjectManager.Player.CalcDamage(target, DamageType.Magical, new[] { 50, 55, 60, 65, 70, 75, 80, 85, 90, 100, 110, 120, 130, 140, 150, 160, 170, 180 }[ObjectManager.Player.Level - 1] + 0.6f * ObjectManager.Player.AbilityPower());
            dmg += dmg * 0.25f * (ActiveSoldiers.Count(p => p.Position.Distance(target.Position) < SoldierAttackRange) - 1);
            return dmg;
        }

        public static bool InAARange(Obj_AI_Base target)
        {
            foreach(var soldier in s_Soldiers)
            {
                if (Vector2.DistanceSquared(target.Position.To2D(), soldier.Position.To2D()) <= SoldierAttackRange * SoldierAttackRange)
                    return true;
            }
            return false;
        }

        private static void AIHeroClient_OnCreate(GameObject sender, EventArgs args)
        {
            if (s_ProcessNextSoldier && sender.Name == "AzirSoldier")
            {
                s_Soldiers.Add(sender);
                s_ProcessNextSoldier = false;
            }
        }

        private static void AIHeroClient_OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if(args.Animation == "Death")
            {
                int idx = s_Soldiers.FindIndex(p => p.NetworkId == sender.NetworkId);
                if (idx != -1)
                    s_Soldiers.RemoveAt(idx);
            }
        }

        private static void AIHeroClient_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name.ToLower() == "azirbasicattacksoldier")
                {
                    SoldierAttacking = true;
                    LeagueSharp.Common.Utility.DelayAction.Add((int)(ObjectManager.Player.AttackCastDelay * 1000), () => SoldierAttacking = false);
                }
                else if(args.SData.Name == ObjectManager.Player.GetSpell(SpellSlot.W).SData.Name)
                {
                    s_ProcessNextSoldier = true;
                    s_LastSoldierSpawn = Utils.TickCount;
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var it = m["SAutoCarry.Helpers.SoldierMgr.Root.DrawRanges"].Cast<CheckBox>().CurrentValue;
            if (it)
            {
                foreach(var soldier in s_Soldiers)
                {
                    if (ObjectManager.Player.ServerPosition.Distance(soldier.Position) < 1000f)
                        Render.Circle.DrawCircle(soldier.Position, SoldierAttackRange, Color.Yellow);
                }
            }
        }
    }
}
