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

namespace PortAIO.Champion.Akali
{
    class Program
    {
        public const string ChampionName = "Akali";

        public static LeagueSharp.Common.Spell Q;
        public static LeagueSharp.Common.Spell W;
        public static LeagueSharp.Common.Spell E;
        public static LeagueSharp.Common.Spell R;
        public static List<LeagueSharp.Common.Spell> SpellList = new List<LeagueSharp.Common.Spell>();

        public static SpellSlot IgniteSlot;
        public static Items.Item Hex;
        public static Items.Item Cutlass;

        public static Menu Menu, ComboMenu, HarassMenu, FarmMenu, DrawingMenu, MiscMenu, JungleMenu;

        private static AIHeroClient myHero
        {
            get { return Player.Instance; }
        }
        public static void Main()
        {
            Game_OnGameLoad();
        }
        public static bool getCheckBoxItem(Menu m, string item)
        {
            return m[item].Cast<CheckBox>().CurrentValue;
        }

        public static int getSliderItem(Menu m, string item)
        {
            return m[item].Cast<Slider>().CurrentValue;
        }

        public static bool getKeyBindItem(Menu m, string item)
        {
            return m[item].Cast<KeyBind>().CurrentValue;
        }

        private static void Game_OnGameLoad()
        {
            if (myHero.BaseSkinName != ChampionName)
                return;

            Q = new LeagueSharp.Common.Spell(SpellSlot.Q, 600f);
            W = new LeagueSharp.Common.Spell(SpellSlot.W, 700f);
            E = new LeagueSharp.Common.Spell(SpellSlot.E, 290f);
            R = new LeagueSharp.Common.Spell(SpellSlot.R, 800f);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            IgniteSlot = myHero.GetSpellSlot("SummonerDot");
            Hex = new Items.Item(3146, 700);
            Cutlass = new Items.Item(3144, 450);

            Menu = MainMenu.AddMenu("xQx | " + ChampionName, ChampionName);

            ComboMenu = Menu.AddSubMenu("Combo", "Combo");
            ComboMenu.AddGroupLabel("Just hold your combo key! Let the addon do the work!");

            HarassMenu = Menu.AddSubMenu("Harass", "Harass");
            HarassMenu.Add("UseQHarass", new CheckBox("Use Q"));
            HarassMenu.Add("UseEHarass", new CheckBox("Use E"));
            HarassMenu.Add("HarassEnergy", new Slider("Min. Energy Percent: ", 50, 0, 100));
            HarassMenu.Add("HarassUseQT", new KeyBind("Use Q (toggle)!", false, KeyBind.BindTypes.PressToggle, 'J'));

            FarmMenu = Menu.AddSubMenu("Farm", "Farm");
            FarmMenu.Add("UseQFarm", new CheckBox("Use Q"));
            FarmMenu.Add("UseEFarm", new CheckBox("Use E"));

            JungleMenu = Menu.AddSubMenu("JungleFarm", "JungleFarm");
            JungleMenu.Add("UseQJFarm", new CheckBox("Use Q"));
            JungleMenu.Add("UseEJFarm", new CheckBox("Use E"));

            MiscMenu = Menu.AddSubMenu("Misc", "Misc");
            MiscMenu.Add("KillstealR", new CheckBox("Killsteal R", false));

            DrawingMenu = Menu.AddSubMenu("Drawings", "Drawings");
            DrawingMenu.Add("QRange", new CheckBox("Q Range"));
            DrawingMenu.Add("RRange", new CheckBox("R Range"));

            LeagueSharp.Common.Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            LeagueSharp.Common.Utility.HpBarDamageIndicator.Enabled = true;

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;

        }

        private static AIHeroClient enemyHaveMota
        {
            get
            {
                return (from enemy in ObjectManager.Get<AIHeroClient>().Where(enemy => enemy.IsEnemy && enemy.IsValidTarget(R.Range)) from buff in enemy.Buffs where buff.DisplayName == "AkaliMota" select enemy).FirstOrDefault();
            }
        }

        private static float GetComboDamage(Obj_AI_Base vTarget)
        {
            if (vTarget == null) 
            {
                return 0.0f;
            }

            var fComboDamage = 0d;

            if (Q.IsReady())
                fComboDamage += myHero.GetSpellDamage(vTarget, SpellSlot.Q) + myHero.LSGetSpellDamage(vTarget, SpellSlot.Q, 1);
            if (E.IsReady())
                fComboDamage += myHero.GetSpellDamage(vTarget, SpellSlot.E);

            if (R.IsReady())
                fComboDamage += myHero.GetSpellDamage(vTarget, SpellSlot.R) * R.Instance.Ammo;

            if (Items.CanUseItem(3146))
                fComboDamage += myHero.GetItemDamage(vTarget, LeagueSharp.Common.Damage.DamageItems.Hexgun);

            if (IgniteSlot != SpellSlot.Unknown && myHero.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                fComboDamage += myHero.GetSummonerSpellDamage(vTarget, LeagueSharp.Common.Damage.SummonerSpell.Ignite);

            return (float)fComboDamage;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (W.IsReady() && myHero.CountEnemiesInRange(W.Range / 2 + 100) >= 2)
            {
                W.Cast(myHero.Position);
            }

            if (myHero.HasBuff("zedulttargetmark"))
            {
                if (W.IsReady())
                {
                    W.Cast(myHero.Position);
                }
            }

            Orbwalker.DisableAttacking = false;

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) || getKeyBindItem(HarassMenu, "HarassUseQT"))
            {
                var vEnergy = getSliderItem(HarassMenu, "HarassEnergy");
                if (myHero.ManaPercent >= vEnergy)
                    Harass();
            }

            var lc = Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear);
            if (lc)
            {
                Farm(lc);
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                JungleFarm();
            }

            if (getCheckBoxItem(MiscMenu, "KillstealR"))
            {
                Killsteal();
            }
        }

        private static void Combo()
        {
            var t = TargetSelector.GetTarget((R.IsReady() ? R.Range : Q.Range), DamageType.Magical);

            if (t == null) {
                return;
            }

            if (GetComboDamage(t) > t.Health && IgniteSlot != SpellSlot.Unknown && myHero.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                myHero.Spellbook.CastSpell(IgniteSlot, t);
            }

            if (Q.IsReady() && t.IsValidTarget(Q.Range))
            {
                Q.CastOnUnit(t);
            }

            if (Hex.IsReady() && t.IsValidTarget(Hex.Range))
            {
                Hex.Cast(t);
            }

            if (Cutlass.IsReady() && t.IsValidTarget(Cutlass.Range))
            {
                Cutlass.Cast(t);
            }

            var motaEnemy = enemyHaveMota;
            if (motaEnemy != null && motaEnemy.IsValidTarget(Orbwalking.GetRealAutoAttackRange(t)))
                return;

            if (E.IsReady() && t.IsValidTarget(E.Range))
            {
                E.Cast();
            }

            if (R.IsReady() && t.IsValidTarget(R.Range))
            {
                R.CastOnUnit(t);
            }
        }

        private static void Harass()
        {
            var useQ = getCheckBoxItem(HarassMenu, "UseQHarass") && Q.IsReady();
            var useE = getCheckBoxItem(HarassMenu, "UseEHarass") && E.IsReady();
            var t = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

            if (useQ && t.IsValidTarget(Q.Range))
            {
                Q.CastOnUnit(t);
            }

            if (useE && t.IsValidTarget(E.Range))
            {
                E.Cast();
            }
        }

        private static void Farm(bool laneClear)
        {
            if (!Orbwalking.CanMove(40))
                return;

            var allMinions = MinionManager.GetMinions(myHero.ServerPosition, Q.Range);
            var useQ = getCheckBoxItem(FarmMenu, "UseQFarm");
            var useE = getCheckBoxItem(FarmMenu, "UseEFarm");

            if (useQ && Q.IsReady())
            {
                foreach (var minion in allMinions)
                {
                    if (minion.IsValidTarget() && HealthPrediction.GetHealthPrediction(minion, (int)(myHero.Distance(minion) * 1000 / 1400)) < 0.75 * myHero.GetSpellDamage(minion, SpellSlot.Q))
                    {
                        Q.CastOnUnit(minion);
                        return;
                    }
                }
            }

            if (useE && E.IsReady())
            {
                if (allMinions.Any(minion => minion.IsValidTarget(E.Range) && minion.Health < 0.75 * myHero.GetSpellDamage(minion, SpellSlot.E) && minion.IsValidTarget(E.Range)))
                {
                    E.Cast();
                    return;
                }
            }

            if (laneClear)
            {
                foreach (var minion in allMinions)
                {
                    if (useQ)
                        Q.CastOnUnit(minion);

                    if (useE && minion.IsValidTarget(E.Range))
                        E.Cast();
                }
            }
        }

        private static void JungleFarm()
        {
            var mobs = MinionManager.GetMinions(
                myHero.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count > 0)
            {
                var mob = mobs[0];

                if (Q.IsReady())
                    Q.CastOnUnit(mob);

                if (E.IsReady() && E.IsInRange(mob))
                    E.Cast();
            }
        }

        private static void Killsteal()
        {
            var useR = getCheckBoxItem(MiscMenu, "KillstealR") && R.IsReady();
            if (useR)
            {
                foreach (var hero in ObjectManager.Get<AIHeroClient>().Where(hero => hero.IsValidTarget(R.Range)))
                {
                    if (hero.Distance(ObjectManager.Player) <= R.Range &&
                        myHero.GetSpellDamage(hero, SpellSlot.R) >= hero.Health)
                        R.CastOnUnit(hero, true);
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (myHero.IsDead)
                return;

            var qcheck = getCheckBoxItem(DrawingMenu, "QRange");
            var rcheck = getCheckBoxItem(DrawingMenu, "RRange");
            if (qcheck)
            {
                Render.Circle.DrawCircle(myHero.Position, Q.Range, Color.FromArgb(255, 255, 255, 255), 1);
            }
            if (rcheck)
            {
                Render.Circle.DrawCircle(myHero.Position, R.Range, Color.FromArgb(255, 255, 255, 255), 1);
            }
        }
    }
}
