using System;
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
using PortAIO.Utility.BrianSharp;

namespace PortAIO.Champion.Aatrox
{
    public static class Program
    {

        private static LeagueSharp.Common.Spell Q, E, Q2;
        private static LeagueSharp.Common.Spell W, R;
        public static Items.Item Tiamat, Hydra, Youmuu, Zhonya, Seraph, Sheen, Iceborn, Trinity;
        public static SpellSlot Flash, Smite, Ignite;

        private static Menu Menu, ComboMenu, HarassMenu, ClearMenu, DrawMenu, FleeMenu, KSMenu, GapMenu, IntMenu, SmiteMenu;

        private static AIHeroClient myHero
        {
            get { return Player.Instance; }
        }

        public static void Main()
        {
            OnLoad();
        }

        #region Menu Items
        public static bool comboQ { get { return ComboMenu["Q"].Cast<CheckBox>().CurrentValue; } }
        public static bool comboW { get { return ComboMenu["W"].Cast<CheckBox>().CurrentValue; } }
        public static bool comboE { get { return ComboMenu["E"].Cast<CheckBox>().CurrentValue; } }
        public static bool comboR { get { return ComboMenu["R"].Cast<CheckBox>().CurrentValue; } }
        public static int ComboWHpU { get { return ComboMenu["WHpU"].Cast<Slider>().CurrentValue; } }
        public static int RHpU { get { return ComboMenu["RHpU"].Cast<Slider>().CurrentValue; } }
        public static int RCountA { get { return ComboMenu["RCountA"].Cast<Slider>().CurrentValue; } }

        public static bool harassQ { get { return HarassMenu["Q"].Cast<CheckBox>().CurrentValue; } }
        public static bool harassE { get { return HarassMenu["E"].Cast<CheckBox>().CurrentValue; } }
        public static bool _AutoE { get { return HarassMenu["AutoE"].Cast<KeyBind>().CurrentValue; } }
        public static int AutoEHpA { get { return HarassMenu["AutoEHpA"].Cast<Slider>().CurrentValue; } }
        public static int QHpA { get { return HarassMenu["QHpA"].Cast<Slider>().CurrentValue; } }

        public static bool clearQ { get { return ClearMenu["Q"].Cast<CheckBox>().CurrentValue; } }
        public static bool clearW { get { return ClearMenu["W"].Cast<CheckBox>().CurrentValue; } }
        public static bool clearE { get { return ClearMenu["E"].Cast<CheckBox>().CurrentValue; } }
        public static bool WPriority { get { return ClearMenu["WPriority"].Cast<CheckBox>().CurrentValue; } }
        public static bool Item { get { return ClearMenu["Item"].Cast<CheckBox>().CurrentValue; } }
        public static int ClearWHpU { get { return ClearMenu["WHpU"].Cast<Slider>().CurrentValue; } }

        public static bool drawQ { get { return DrawMenu["Q"].Cast<CheckBox>().CurrentValue; } }
        public static bool drawE { get { return DrawMenu["E"].Cast<CheckBox>().CurrentValue; } }
        public static bool drawR { get { return DrawMenu["R"].Cast<CheckBox>().CurrentValue; } }

        public static bool fleeQ { get { return FleeMenu["Q"].Cast<CheckBox>().CurrentValue; } }
        public static bool fleeE { get { return FleeMenu["E"].Cast<CheckBox>().CurrentValue; } }

        public static bool ksQ { get { return KSMenu["Q"].Cast<CheckBox>().CurrentValue; } }
        public static bool ksE { get { return KSMenu["E"].Cast<CheckBox>().CurrentValue; } }
        public static bool ksSmite { get { return KSMenu["Smite"].Cast<CheckBox>().CurrentValue; } }
        public static bool ksIgnite { get { return KSMenu["Ignite"].Cast<CheckBox>().CurrentValue; } }

        public static bool antiQ { get { return GapMenu["Q"].Cast<CheckBox>().CurrentValue; } }

        public static bool intQ { get { return IntMenu["Q"].Cast<CheckBox>().CurrentValue; } }

        public static bool _Smite { get { return SmiteMenu["Smite"].Cast<CheckBox>().CurrentValue; } }
        public static bool Auto { get { return SmiteMenu["Auto"].Cast<CheckBox>().CurrentValue; } }
        public static bool Baron { get { return SmiteMenu["Baron"].Cast<CheckBox>().CurrentValue; } }
        public static bool Dragon { get { return SmiteMenu["Dragon"].Cast<CheckBox>().CurrentValue; } }
        public static bool Red { get { return SmiteMenu["Red"].Cast<CheckBox>().CurrentValue; } }
        public static bool Blue { get { return SmiteMenu["Blue"].Cast<CheckBox>().CurrentValue; } }
        public static bool Krug { get { return SmiteMenu["Krug"].Cast<CheckBox>().CurrentValue; } }
        public static bool Gromp { get { return SmiteMenu["Gromp"].Cast<CheckBox>().CurrentValue; } }
        public static bool Raptor { get { return SmiteMenu["Raptor"].Cast<CheckBox>().CurrentValue; } }
        public static bool Wolf { get { return SmiteMenu["Wolf"].Cast<CheckBox>().CurrentValue; } }

        #endregion

        private static void OnLoad()
        {
            Menu = MainMenu.AddMenu("Aatrox", "Aatrox");
            Menu.AddLabel("Ported from BrianSharp - Berb");
            Menu.AddSeparator();

            ComboMenu = Menu.AddSubMenu("Combo");
            ComboMenu.Add("Q", new CheckBox("Use Q"));
            ComboMenu.Add("W", new CheckBox("Use W"));
            ComboMenu.Add("WHpU", new Slider("-> Switch To Heal If Hp <", 50, 0, 100));
            ComboMenu.Add("E", new CheckBox("Use E"));
            ComboMenu.Add("R", new CheckBox("Use R"));
            ComboMenu.Add("RHpU", new Slider("-> If Enemy Hp <", 60, 0, 100));
            ComboMenu.Add("RCountA", new Slider("-> Or Enemy >=", 2, 1, 5));
            ComboMenu.AddSeparator();

            HarassMenu = Menu.AddSubMenu("Harass");
            HarassMenu.Add("AutoE", new KeyBind("Auto E", false, KeyBind.BindTypes.PressToggle, 'H'));
            HarassMenu.Add("AutoEHpA", new Slider("-> If Hp >=", 50, 0, 100));
            HarassMenu.Add("Q", new CheckBox("Use Q"));
            HarassMenu.Add("QHpA", new Slider("-> If Hp >=", 20, 0, 100));
            HarassMenu.Add("E", new CheckBox("Use E"));
            HarassMenu.AddSeparator();

            ClearMenu = Menu.AddSubMenu("Clear");
            ClearMenu.Add("Q", new CheckBox("Use Q"));
            ClearMenu.Add("W", new CheckBox("Use W"));
            ClearMenu.Add("WPriority", new CheckBox("-> Priority Heal"));
            ClearMenu.Add("WHpU", new Slider("-> Switch To Heal If Hp <", 50, 0, 100));
            ClearMenu.Add("E", new CheckBox("Use E"));
            ClearMenu.Add("Item", new CheckBox("Use Tiamat/Hydra Item"));
            ClearMenu.AddSeparator();

            FleeMenu = Menu.AddSubMenu("Flee");
            FleeMenu.Add("Q", new CheckBox("Use Q"));
            FleeMenu.Add("E", new CheckBox("Use E"));
            FleeMenu.AddSeparator();

            KSMenu = Menu.AddSubMenu("Kill Steal");
            KSMenu.Add("Q", new CheckBox("Use Q"));
            KSMenu.Add("E", new CheckBox("Use E"));
            KSMenu.Add("Smite", new CheckBox("Use Smite"));
            KSMenu.Add("Ignite", new CheckBox("Use Ignite"));
            KSMenu.AddSeparator();

            GapMenu = Menu.AddSubMenu("Anti Gap Closer");
            GapMenu.Add("Q", new CheckBox("Use Q"));
            foreach (var spell in AntiGapcloser.Spells.Where(i => HeroManager.Enemies.Any(a => i.ChampionName == a.ChampionName)))
            {
                GapMenu.Add(spell.ChampionName + "_" + spell.Slot, new CheckBox("-> Skill " + spell.Slot + " Of " + spell.ChampionName));
            }
            GapMenu.AddSeparator();

            IntMenu = Menu.AddSubMenu("Interrupt");
            IntMenu.Add("Q", new CheckBox("Use Q"));
            foreach (var spell in LeagueSharp.Common.Interrupter.Spells.Where(i => HeroManager.Enemies.Any(a => i.ChampionName == a.ChampionName)))
            {
                IntMenu.Add(spell.ChampionName + "_" + spell.Slot, new CheckBox("-> Skill " + spell.Slot + " Of " + spell.ChampionName));
            }
            IntMenu.AddSeparator();

            SmiteMenu = Menu.AddSubMenu("Smite Mob");
            SmiteMenu.Add("Smite", new CheckBox("Use Smite"));
            SmiteMenu.Add("Auto", new CheckBox("-> Auto Smite"));
            SmiteMenu.Add("Baron", new CheckBox("-> Baron Nashor"));
            SmiteMenu.Add("Dragon", new CheckBox("-> Dragon"));
            SmiteMenu.Add("Red", new CheckBox("-> Red Brambleback"));
            SmiteMenu.Add("Blue", new CheckBox("-> Blue Sentinel"));
            SmiteMenu.Add("Krug", new CheckBox("-> Ancient Krug"));
            SmiteMenu.Add("Gromp", new CheckBox("-> Gromp"));
            SmiteMenu.Add("Raptor", new CheckBox("-> Crimson Raptor"));
            SmiteMenu.Add("Wolf", new CheckBox("-> Greater Murk Wolf"));
            SmiteMenu.AddSeparator();

            DrawMenu = Menu.AddSubMenu("Drawings");
            DrawMenu.Add("Q", new CheckBox("Use Q"));
            DrawMenu.Add("E", new CheckBox("Use E"));
            DrawMenu.Add("R", new CheckBox("Use R"));
            DrawMenu.AddSeparator();

            Q = new LeagueSharp.Common.Spell(SpellSlot.Q, 650);
            Q2 = new LeagueSharp.Common.Spell(SpellSlot.Q, 650);
            W = new LeagueSharp.Common.Spell(SpellSlot.W);
            E = new LeagueSharp.Common.Spell(SpellSlot.E, 1075);
            R = new LeagueSharp.Common.Spell(SpellSlot.R, 550);
            Q.SetSkillshot(0.6f, 250, 2000, false, SkillshotType.SkillshotCircle);
            Q2.SetSkillshot(0.6f, 150, 2000, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.25f, 35, 1250, false, SkillshotType.SkillshotLine);

            Tiamat = LeagueSharp.Common.Data.ItemData.Tiamat_Melee_Only.GetItem();
            Hydra = LeagueSharp.Common.Data.ItemData.Ravenous_Hydra_Melee_Only.GetItem();

            foreach (var spell in myHero.Spellbook.Spells.Where(i => i.Name.ToLower().Contains("smite") && (i.Slot == SpellSlot.Summoner1 || i.Slot == SpellSlot.Summoner2)))
            {
                Smite = spell.Slot;
            }
            Ignite = myHero.GetSpellSlot("summonerdot");

            Game.OnTick += OnTick;
            EloBuddy.SDK.Events.Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            LeagueSharp.Common.Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
            Drawing.OnDraw += OnDraw;
        }

        static void Gapcloser_OnGapcloser(AIHeroClient sender, EloBuddy.SDK.Events.Gapcloser.GapcloserEventArgs e)
        {
            if (sender == myHero)
            {
                return;
            }
            if (myHero.IsDead || !antiQ || !Q.IsReady())
            {
                return;
            }
            if (GapMenu[sender.ChampionName + "_" + e.Slot].Cast<CheckBox>().CurrentValue)
            {
                Q2.Cast(sender, false);
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (myHero.IsDead)
            {
                return;
            }
            if (drawQ && Q.Level > 0)
            {
                Render.Circle.DrawCircle(myHero.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);
            }
            if (drawE && E.Level > 0)
            {
                Render.Circle.DrawCircle(myHero.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);
            }
            if (drawR && R.Level > 0)
            {
                Render.Circle.DrawCircle(myHero.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
            }
        }

        private static void OnPossibleToInterrupt(AIHeroClient unit, InterruptableSpell spell)
        {
            if (myHero.IsDead || !intQ || !Q.IsReady())
            {
                return;
            }
            if (IntMenu[unit.ChampionName + "_" + spell.Slot].Cast<CheckBox>().CurrentValue)
            {
                Q2.Cast(unit, false);
            }
        }

        public static bool CastIgnite(AIHeroClient target)
        {
            return Ignite.IsReady() && target.IsValidTarget(600) && target.Health + 5 < myHero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) && myHero.Spellbook.CastSpell(Ignite, target);
        }

        public static bool CastSmite(Obj_AI_Base target, bool killable = true)
        {
            return Smite.IsReady() && target.IsValidTarget(760) && (!killable || target.Health < myHero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Smite)) && myHero.Spellbook.CastSpell(Smite, target);
        }

        private static bool HaveWDmg
        {
            get { return myHero.HasBuff("AatroxWPower"); }
        }

        private static List<AIHeroClient> GetRTarget
        {
            get
            {
                return HeroManager.Enemies.Where(i => i.LSIsValidTarget() && myHero.LSDistance(LeagueSharp.Common.Prediction.GetPrediction(i, 0.25f).UnitPosition) < R.Range).ToList();
            }
        }

        private static void OnTick(EventArgs args)
        {
            if (myHero.IsDead || MenuGUI.IsChatOpen || myHero.LSIsRecalling())
            {
                return;
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Fight("Combo");
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Fight("Harass");
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                Clear();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
            {
                Flee();
            }
            if (Auto) //&& !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) || !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                SmiteMob();
            }
            AutoE();
            KillSteal();
        }

        private static void AutoE()
        {
            if (_AutoE || myHero.HealthPercent < AutoEHpA)
            {
                return;
            }
            E.CastOnBestTarget(0, false, true);
        }

        private static void Flee()
        {
            if (fleeQ && Q.IsReady())
            {
                Q.Cast(Game.CursorPos, false);
                return;
            }
            if (fleeE)
            {
                E.CastOnBestTarget(0, false, true);
            }
        }

        private static void Fight(string mode)
        {
            if ((comboQ || harassQ) && (mode == "Combo" || myHero.HealthPercent >= QHpA) && Q.IsReady())
            {
                Q2.CastOnBestTarget(Q2.Width / 2, false, true);
                return;
            }

            if ((comboE || harassE) && E.IsReady())
            {
                E.CastOnBestTarget(0, false, true);
                return;
            }

            if (mode != "Combo")
            {
                return;
            }

            if (comboW && W.IsReady())
            {
                if (myHero.HealthPercent >= ComboWHpU)
                {
                    if (!HaveWDmg)
                    {
                        W.Cast(false);
                        return;
                    }
                }
                else if (HaveWDmg)
                {
                    W.Cast(false);
                    return;
                }
            }
            if (comboR && R.IsReady() && !myHero.LSIsDashing())
            {
                var obj = GetRTarget;
                if ((obj.Count > 1 && obj.Any(i => R.IsKillable(i))) || obj.Any(i => i.HealthPercent < RHpU) || obj.Count >= RCountA)
                {
                    R.Cast(false);
                }
            }
        }

        public static bool CanSmiteMob(string name)
        {
            if (Baron && name.StartsWith("SRU_Baron"))
            {
                return true;
            }
            if (Dragon && name.StartsWith("SRU_Dragon"))
            {
                return true;
            }
            if (name.Contains("Mini"))
            {
                return false;
            }
            if (Red && name.StartsWith("SRU_Red"))
            {
                return true;
            }
            if (Blue && name.StartsWith("SRU_Blue"))
            {
                return true;
            }
            if (Krug && name.StartsWith("SRU_Krug"))
            {
                return true;
            }
            if (Gromp && name.StartsWith("SRU_Gromp"))
            {
                return true;
            }
            if (Raptor && name.StartsWith("SRU_Razorbeak"))
            {
                return true;
            }
            return Wolf && name.StartsWith("SRU_Murkwolf");
        }

        public static void SmiteMob()
        {
            if (!_Smite || !Smite.IsReady())
            {
                return;
            }
            var obj = MinionManager.GetMinions(760, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault(i => CanSmiteMob(i.Name));
            if (obj == null)
            {
                return;
            }
            CastSmite(obj);
        }

        private static void Clear()
        {
            SmiteMob();
            var minionObj = Helper.GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth).Cast<Obj_AI_Base>().ToList();
            if (!minionObj.Any())
            {
                return;
            }
            if (clearQ && Q.IsReady())
            {
                var pos = Q.GetCircularFarmLocation(minionObj.Where(i => Q.IsInRange(i, Q.Range + Q.Width / 2)).ToList());
                if (pos.MinionsHit > 1)
                {
                    Q.Cast(pos.Position, false);
                    return;
                }
                else
                {
                    var obj = minionObj.FirstOrDefault(i => i.MaxHealth >= 1200);
                    if (obj != null && Q.IsInRange(obj, Q.Range + Q2.Width / 2) && Q.IsReady())
                    {
                        Q.Cast(obj, false);
                        return;
                    }
                }
            }
            if (clearE && E.IsReady())
            {
                var pos = E.GetLineFarmLocation(minionObj);
                if (pos.MinionsHit > 0)
                {
                    E.Cast(pos.Position, false);
                    return;
                }
            }
            if (clearW && W.IsReady())
            {
                if (myHero.HealthPercent >= (WPriority ? 85 : ClearWHpU))
                {
                    if (!HaveWDmg)
                    {
                        W.Cast(false);
                        return;
                    }
                }
                else if (HaveWDmg)
                {
                    W.Cast(false);
                    return;
                }
            }
            if (Item)
            {
                var item = Hydra.IsReady() ? Hydra : Tiamat;
                if (item.IsReady() && (minionObj.Count(i => item.IsInRange(i)) > 2 || minionObj.Any(i => i.MaxHealth >= 1200 && i.Distance(myHero) < item.Range - 80)))
                {
                    item.Cast();
                }
            }
        }

        private static void KillSteal()
        {
            if (ksIgnite && Ignite.IsReady())
            {
                var target = EloBuddy.SDK.TargetSelector.GetTarget(600, DamageType.True);
                if (target != null)
                {
                    CastIgnite(target);
                    return;
                }
            }
            if (ksSmite && (Helper.CurrentSmiteType == Helper.SmiteType.Blue || Helper.CurrentSmiteType == Helper.SmiteType.Red))
            {
                var target = EloBuddy.SDK.TargetSelector.GetTarget(760, DamageType.True);
                if (target != null)
                {
                    CastSmite(target);
                    return;
                }
            }
            if (ksQ && Q.IsReady())
            {
                var target = Q.GetTarget(Q.Width / 2);
                if (target != null && Q.IsKillable(target))
                {
                    Q2.Cast(target);
                    return;
                }
            }
            if (ksE && E.IsReady())
            {
                var target = E.GetTarget();
                if (target != null && E.IsKillable(target))
                {
                    E.Cast(target);
                    return;
                }
            }
        }

    }
}
