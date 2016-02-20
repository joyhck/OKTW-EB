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

namespace Jinx
{
    public static class Program
    {

        private static Spell.Skillshot W, E, R;
        private static Spell.Active Q;

        private const int DefaultRange = 525;
        private static int _wCastTime;

        private static Menu Menu;

        private static AIHeroClient myHero
        {
            get { return Player.Instance; }
        }

        private static int LastAATick;

        public static void Main()
        {
            Loading.OnLoadingComplete += OnLoad;
        }

        #region Menu Items
        public static bool useQ { get { return Menu["useQ"].Cast<CheckBox>().CurrentValue; } }
        public static bool useW { get { return Menu["useW"].Cast<CheckBox>().CurrentValue; } }
        public static bool useE { get { return Menu["useE"].Cast<CheckBox>().CurrentValue; } }
        public static bool useR { get { return Menu["useR"].Cast<CheckBox>().CurrentValue; } }
        public static bool useQH { get { return Menu["useQH"].Cast<CheckBox>().CurrentValue; } }
        public static bool useWH { get { return Menu["useWH"].Cast<CheckBox>().CurrentValue; } }
        public static bool useQLH { get { return Menu["useQLH"].Cast<CheckBox>().CurrentValue; } }
        public static bool autoR { get { return Menu["autoR"].Cast<CheckBox>().CurrentValue; } }
        public static bool autoE { get { return Menu["autoE"].Cast<CheckBox>().CurrentValue; } }
        public static bool gapclose { get { return Menu["gapclose"].Cast<CheckBox>().CurrentValue; } }
        public static bool interrupt { get { return Menu["interrupt"].Cast<CheckBox>().CurrentValue; } }
        public static bool useWJG { get { return Menu["useWJG"].Cast<CheckBox>().CurrentValue; } }
        public static bool useQJG { get { return Menu["useQJG"].Cast<CheckBox>().CurrentValue; } }
        public static bool useQLC { get { return Menu["useQLC"].Cast<CheckBox>().CurrentValue; } }

        public static int autoSwitch { get { return Menu["autoSwitch"].Cast<Slider>().CurrentValue; } }
        public static int manaH { get { return Menu["manaH"].Cast<Slider>().CurrentValue; } }
        public static int manaLH { get { return Menu["manaLH"].Cast<Slider>().CurrentValue; } }
        public static int manaLC { get { return Menu["manaLC"].Cast<Slider>().CurrentValue; } }
        public static int manaJG { get { return Menu["manaJG"].Cast<Slider>().CurrentValue; } }

        public static bool autoH { get { return Menu["autoH"].Cast<KeyBind>().CurrentValue; } }
        #endregion

        private static void OnLoad(EventArgs args)
        {
            if (myHero.Hero != Champion.Jinx)
            {
                return;
            }

            Menu = MainMenu.AddMenu("SS Jinx", "jinx");
            Menu.AddLabel("Ported from SharpShooter - Berb");
            Menu.AddSeparator();
            Menu.AddGroupLabel("Combo");
            Menu.Add("useQ", new CheckBox("Use Q"));
            Menu.Add("useW", new CheckBox("Use W"));
            Menu.Add("useE", new CheckBox("Use E"));
            Menu.Add("useR", new CheckBox("Use R"));
            Menu.Add("autoSwitch", new Slider("Switch to Rocket If will hit enemy Number >= ", 2, 1, 5));
            Menu.AddSeparator();
            Menu.AddGroupLabel("Harass");
            Menu.Add("useQH", new CheckBox("Use Q"));
            Menu.Add("useWH", new CheckBox("Use W"));
            Menu.Add("manaH", new Slider("If Mana > ", 65, 0, 100));
            Menu.Add("autoH", new KeyBind("Auto Harass", false, KeyBind.BindTypes.PressToggle, 'T'));
            Menu.AddSeparator();
            Menu.AddGroupLabel("Lane Clear");
            Menu.Add("useQLC", new CheckBox("Use Q"));
            Menu.Add("manaLC", new Slider("If Mana > ", 65, 0, 100));
            Menu.AddSeparator();
            Menu.AddGroupLabel("Jungle Clear");
            Menu.Add("useQJG", new CheckBox("Use Q"));
            Menu.Add("useWJG", new CheckBox("Use W"));
            Menu.Add("manaJG", new Slider("If Mana > ", 65, 0, 100));
            Menu.AddSeparator();
            Menu.AddGroupLabel("Last Hit");
            Menu.Add("useQLH", new CheckBox("Use Q"));
            Menu.Add("manaLH", new Slider("If Mana > ", 65, 0, 100));
            Menu.AddSeparator();
            Menu.Add("autoR", new CheckBox("Auto R on Killable Target"));
            Menu.Add("autoE", new CheckBox("Auto E on Immobile Target"));
            Menu.Add("gapclose", new CheckBox("Gapcloser"));
            Menu.Add("interrupt", new CheckBox("Interrupter"));


            Q = new Spell.Active(SpellSlot.Q);
            W = new Spell.Skillshot(SpellSlot.W, 1450, SkillShotType.Linear, 600, 3300, 60);
            E = new Spell.Skillshot(SpellSlot.E, 900, SkillShotType.Circular, 1100, 1750, 1);
            R = new Spell.Skillshot(SpellSlot.R, 2000, SkillShotType.Linear, 700, 1500, 140);

            Game.OnTick += OnTick;
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
            Orbwalker.OnPreAttack += OnPreAttack;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            Gapcloser.OnGapcloser += OnGapCloser;
        }

        private static int GetQRange
        {
            get { return DefaultRange + 25 * Q.Level; }
        }

        private static bool IsQActive
        {
            get { return myHero.HasBuff("JinxQ"); }
        }

        private static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (interrupt)
                if (EIsReadyPerfectly())
                    if (e.DangerLevel >= DangerLevel.Medium)
                        E.Cast(sender);
        }

        private static void OnTick(EventArgs args)
        {
            if (!myHero.IsDead)
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    if (useQ)
                    {
                        if (QIsReadyPerfectly())
                        {
                            if (myHero.CountEnemiesInRange(2000f) > 0)
                            {
                                var target = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(myHero.GetRealAutoAttackRange(x, GetQRange))).OrderByDescending(a => TargetSelector.GetPriority(a)).FirstOrDefault();
                                if (target != null)
                                {
                                    if (target.CountEnemiesInRange(200) >= autoSwitch)
                                        QSwitch(true);
                                    else
                                        QSwitch(!target.IsValidTarget(myHero.GetRealAutoAttackRange(target, DefaultRange)));
                                }
                                else
                                {
                                    QSwitch(true);
                                }
                            }
                            else
                            {
                                QSwitch(false);
                            }
                        }
                    }

                    if (useW)
                        if (myHero.CountEnemiesInRange(400f) == 0)
                            if (WIsReadyPerfectly())
                            {
                                var target = TargetSelector.GetTarget(W.Range, DamageType.Physical);
                                if (target.IsValidTarget(W.Range))
                                {
                                    W.Cast(target);
                                }
                            }

                    if (useE)
                        if (EIsReadyPerfectly())
                        {
                            var target = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(600) && E.GetPrediction(x).HitChance >= HitChance.High && x.IsMoving).OrderBy(x => x.Distance(myHero)).FirstOrDefault();
                            if (target != null)
                            {
                                E.Cast(target);
                            }
                        }

                    if (useR)
                    {
                        if (RIsReadyPerfectly())
                        {
                            var target = TargetSelector.GetTarget(R.Range, DamageType.Physical);
                            if (target != null && GetRDamage(target) > target.Health)
                            {
                                R.Cast(target);
                            }
                        }
                    }
                }
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                {
                    if (useQH)
                    {
                        if (QIsReadyPerfectly())
                            if (myHero.IsManaPercentOkay(manaH))
                                if (myHero.CountEnemiesInRange(2000f) > 0)
                                {
                                    var target = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(myHero.GetRealAutoAttackRange(x, GetQRange))).OrderByDescending(a => TargetSelector.GetPriority(a)).FirstOrDefault();
                                    QSwitch(!target.IsValidTarget(myHero.GetRealAutoAttackRange(target, DefaultRange)));
                                }
                                else
                                    QSwitch(false);
                            else
                                QSwitch(false);
                    }
                    else
                        QSwitch(false);

                    if (useWH)
                        if (myHero.IsManaPercentOkay(manaH))
                            if (myHero.CountEnemiesInRange(400f) == 0)
                                if (WIsReadyPerfectly())
                                {
                                    var target = TargetSelector.GetTarget(W.Range, DamageType.Physical);
                                    if (target.IsValidTarget(W.Range))
                                        W.Cast(target);
                                }
                }
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                {
                    if (useQLC)
                    {
                        if (QIsReadyPerfectly())
                            if (myHero.IsManaPercentOkay(manaLC))
                            {
                            }
                            else
                                QSwitch(false);
                    }
                    else
                        QSwitch(false);

                    if (useWJG)
                        if (myHero.IsManaPercentOkay(manaJG))
                            if (WIsReadyPerfectly())
                            {
                                var target = EntityManager.MinionsAndMonsters.GetJungleMonsters().FirstOrDefault(x => x.IsValidTarget(600) && W.GetPrediction(x).HitChance >= HitChance.High);
                                if (target != null)
                                    W.Cast(target);
                            }
                }
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
                {
                    if (useQLH)
                    {
                        if (QIsReadyPerfectly())
                            if (myHero.IsManaPercentOkay(manaLH))
                            {
                                var target = EntityManager.MinionsAndMonsters.GetLaneMinions().FirstOrDefault(x => x.IsKillableAndValidTarget((double)myHero.GetAutoAttackDamage(x, false) + myHero.GetSpellDamage(x, SpellSlot.Q), DamageType.Physical) && x.IsValidTarget(myHero.GetRealAutoAttackRange(x, GetQRange)) && !x.IsValidTarget(myHero.GetRealAutoAttackRange(x, DefaultRange)));
                                if (target != null)
                                {
                                    QSwitch(true);

                                    if (myHero.IsInAutoAttackRange(target))
                                        Orbwalker.ForcedTarget = target;
                                }
                                else
                                    QSwitch(false);
                            }
                            else
                                QSwitch(false);
                    }
                    else
                        QSwitch(false);
                }
            }

            if (autoR)
            {
                if (RIsReadyPerfectly())
                {
                    var target = TargetSelector.GetTarget(R.Range, DamageType.Physical);
                    if (target != null && GetRDamage(target) > target.Health)
                    {
                        R.Cast(target);
                    }
                }

            }

            if (autoE)
                if (EIsReadyPerfectly())
                {
                    var target = EntityManager.Heroes.Enemies.FirstOrDefault(x => x.IsValidTarget(E.Range) && x.IsImmobileUntil() > 0.5f);
                    if (target != null)
                        E.Cast(target);
                }

            if (autoH)
            {
                if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                    if (!myHero.IsRecalling())
                        if (useWH)
                            if (myHero.IsManaPercentOkay(manaH))
                            {
                                var target = TargetSelector.GetTarget(W.Range, DamageType.Physical);
                                if (target != null)
                                {
                                    if (UnderEnemyTower(myHero.Position) ? !UnderEnemyTower(target.Position) : true)
                                    {
                                        W.Cast(target);
                                    }
                                }
                            }
            }
        }


        private static double GetRDamage(Obj_AI_Base target)
        {
            return ObjectManager.Player.CalculateDamageOnUnit(target, DamageType.Physical, (float)(new double[] { 0, 25, 30, 35 }[R.Level] / 100 * (target.MaxHealth - target.Health) + (new double[] { 0, 25, 35, 45 }[R.Level] + 0.1 * ObjectManager.Player.FlatPhysicalDamageMod) * Math.Min(1 + ObjectManager.Player.Distance(target.ServerPosition) / 15 * 0.09d, 10)));
        }

        internal static float GetRealAutoAttackRange(this AttackableUnit unit, AttackableUnit target, int autoAttackRange)
        {
            var result = autoAttackRange + unit.BoundingRadius;
            if (target.IsValidTarget())
                return result + target.BoundingRadius;
            return result;
        }

        public static bool UnderEnemyTower(Vector3 pos)
        {
            return EntityManager.Turrets.Enemies.Any(a => a.Distance(pos) < 950);
        }

        internal static double IsImmobileUntil(this AIHeroClient unit)
        {
            var result = unit.Buffs.Where(buff => buff.IsActive && Game.Time <= buff.EndTime && (buff.Type == BuffType.Charm || buff.Type == BuffType.Knockup || buff.Type == BuffType.Stun || buff.Type == BuffType.Suppression || buff.Type == BuffType.Snare)).Aggregate(0d, (current, buff) => Math.Max(current, buff.EndTime));
            return result - Game.Time;
        }

        internal static bool IsKillableAndValidTarget(this Obj_AI_Base target, double calculatedDamage, DamageType damageType, float distance = float.MaxValue)
        {
            if (target == null || !target.IsValidTarget(distance) || target.CharData.BaseSkinName == "gangplankbarrel")
                return false;

            if (target.HasBuff("kindredrnodeathbuff"))
            {
                return false;
            }

            if (target.HasBuff("Undying Rage"))
            {
                return false;
            }

            if (target.HasBuff("JudicatorIntervention"))
            {
                return false;
            }

            if (target.HasBuff("DiplomaticImmunity") && !myHero.HasBuff("poppyulttargetmark"))
            {
                return false;
            }

            if (target.HasBuff("BansheesVeil"))
            {
                return false;
            }

            if (target.HasBuff("SivirShield"))
            {
                return false;
            }

            if (target.HasBuff("ShroudofDarkness"))
            {
                return false;
            }

            if (myHero.HasBuff("summonerexhaust"))
                calculatedDamage *= 0.6;

            if (target.Name == "Blitzcrank")
                if (!target.HasBuff("manabarriercooldown"))
                    if (target.Health + target.HPRegenRate +
                        (damageType == DamageType.Physical ? target.AttackShield : target.MagicShield) +
                        target.Mana * 0.6 + target.PARRegenRate < calculatedDamage)
                        return true;

            if (target.Name == "Garen")
                if (target.HasBuff("GarenW"))
                    calculatedDamage *= 0.7;

            if (target.HasBuff("FerociousHowl"))
                calculatedDamage *= 0.3;

            return target.Health + target.HPRegenRate + (damageType == DamageType.Physical ? target.AttackShield : target.MagicShield) < calculatedDamage - 2;
        }

        public static bool CanMove(float extraWindup)
        {
            if (LastAATick <= Environment.TickCount)
                return (Environment.TickCount + Game.Ping / 2 >= LastAATick + myHero.AttackCastDelay * 1000 + extraWindup);
            return false;
        }

        public static float getSpellMana(SpellSlot spell)
        {
            return myHero.Spellbook.GetSpell(spell).SData.Mana;
        }

        internal static bool IsManaPercentOkay(this AIHeroClient hero, int manaPercent)
        {
            return myHero.ManaPercent > manaPercent;
        }

        public static bool QIsReadyPerfectly()
        {
            var spell = Q;
            return spell != null && spell.Slot != SpellSlot.Unknown && spell.State != SpellState.Cooldown && spell.State != SpellState.Disabled && spell.State != SpellState.NoMana && spell.State != SpellState.NotLearned && spell.State != SpellState.Surpressed && spell.State != SpellState.Unknown && spell.State == SpellState.Ready;
        }

        public static bool WIsReadyPerfectly()
        {
            var spell = W;
            return spell != null && spell.Slot != SpellSlot.Unknown && spell.State != SpellState.Cooldown && spell.State != SpellState.Disabled && spell.State != SpellState.NoMana && spell.State != SpellState.NotLearned && spell.State != SpellState.Surpressed && spell.State != SpellState.Unknown && spell.State == SpellState.Ready;
        }

        public static bool EIsReadyPerfectly()
        {
            var spell = E;
            return spell != null && spell.Slot != SpellSlot.Unknown && spell.State != SpellState.Cooldown && spell.State != SpellState.Disabled && spell.State != SpellState.NoMana && spell.State != SpellState.NotLearned && spell.State != SpellState.Surpressed && spell.State != SpellState.Unknown && spell.State == SpellState.Ready;
        }

        public static bool RIsReadyPerfectly()
        {
            var spell = R;
            return spell != null && spell.Slot != SpellSlot.Unknown && spell.State != SpellState.Cooldown && spell.State != SpellState.Disabled && spell.State != SpellState.NoMana && spell.State != SpellState.NotLearned && spell.State != SpellState.Surpressed && spell.State != SpellState.Unknown && spell.State == SpellState.Ready;
        }

        private static void OnGapCloser(Obj_AI_Base sender, Gapcloser.GapcloserEventArgs args)
        {
            if (gapclose)
                if (EIsReadyPerfectly())
                    if (args.End.Distance(myHero.Position) <= 200)
                        E.Cast(args.End);
        }

        private static void OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.Slot == SpellSlot.W)
                {
                    _wCastTime = Environment.TickCount;
                }
            }

            if (sender.IsMe)
            {
                LastAATick = Environment.TickCount;
            }
        }

        private static void OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (args.Target.IsMe)
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                {
                    if (useQH)
                    {
                        if (ObjectManager.Player.IsManaPercentOkay(manaH))
                        {
                            if (args.Target.IsValidTarget(ObjectManager.Player.GetRealAutoAttackRange(args.Target, DefaultRange)))
                                if (IsQActive)
                                {
                                    QSwitch(false);
                                    args.Process = false;
                                }
                        }
                        else
                            QSwitch(false);
                    }
                    else
                    {
                        QSwitch(false);
                    }
                }

                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                {
                    if (EntityManager.MinionsAndMonsters.GetLaneMinions().Any(x => x.NetworkId == args.Target.NetworkId))
                    {
                        if (useQLC)
                        {
                            if (ObjectManager.Player.IsManaPercentOkay(manaLC))
                            {
                                if (EntityManager.MinionsAndMonsters.GetLaneMinions().Count(x => x.IsValidTarget(200, true, args.Target.Position) && (x.Health > ObjectManager.Player.GetAutoAttackDamage(x) * 2 || x.Health <= ObjectManager.Player.GetAutoAttackDamage(x) + myHero.GetSpellDamage(x, SpellSlot.Q))) >= 2)
                                    QSwitch(true);
                                else
                                    QSwitch(false);
                            }
                            else
                                QSwitch(false);
                        }
                        else
                            QSwitch(false);
                    }
                    else if (EntityManager.MinionsAndMonsters.GetJungleMonsters().Any(x => x.NetworkId == args.Target.NetworkId))
                    {
                        if (useQJG)
                        {
                            if (ObjectManager.Player.IsManaPercentOkay(manaJG))
                            {
                                if (EntityManager.MinionsAndMonsters.GetJungleMonsters().Count(x => x.IsValidTarget(200, true, args.Target.Position)) >= 2)
                                    QSwitch(true);
                                else
                                    QSwitch(false);
                            }
                            else
                                QSwitch(false);
                        }
                        else
                            QSwitch(false);
                    }
                    else
                    {
                        QSwitch(false);
                    }
                }
            }
        }

        private static void QSwitch(bool activate)
        {
            if (QIsReadyPerfectly())
            {
                switch (activate)
                {
                    case true:
                        if (!myHero.HasBuff("JinxQ"))
                        {
                            Q.Cast();
                        }
                        break;
                    case false:
                        if (myHero.HasBuff("JinxQ"))
                        {
                            Q.Cast();
                        }
                        break;
                }
            }

        }
    }
}
