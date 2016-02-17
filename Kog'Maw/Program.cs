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

namespace KogMaw
{
    public static class Program
    {

        private static Spell.Skillshot Q, E, R;
        private static Spell.Active W;

        private static int LastAATick;

        private static Menu Menu;

        private static bool IsZombie = ObjectManager.Player.HasBuff("kogmawicathiansurprise");

        private static AIHeroClient myHero
        {
            get { return Player.Instance; }
        }

        public static void Main()
        {
            Loading.OnLoadingComplete += OnLoad;
        }

        #region Menu Items
        public static bool useQ { get { return Menu["useQ"].Cast<CheckBox>().CurrentValue; } }
        public static bool useW { get { return Menu["useW"].Cast<CheckBox>().CurrentValue; } }
        public static bool useE { get { return Menu["useE"].Cast<CheckBox>().CurrentValue; } }
        public static bool useR { get { return Menu["useR"].Cast<CheckBox>().CurrentValue; } }
        public static int rLimit { get { return Menu["rLimit"].Cast<Slider>().CurrentValue; } }
        public static bool manaW { get { return Menu["manaW"].Cast<CheckBox>().CurrentValue; } }
        public static bool useWJG { get { return Menu["useWJG"].Cast<CheckBox>().CurrentValue; } }
        #endregion

        private static void OnLoad(EventArgs args)
        {
            if (myHero.Hero != Champion.KogMaw)
            {
                return;
            }

            Menu = MainMenu.AddMenu("SharpShooter Kog", "kogmaw");
            Menu.AddLabel("Ported from SharpShooter - Berb");
            Menu.AddSeparator();
            Menu.AddGroupLabel("Combo");
            Menu.Add("useQ", new CheckBox("Use Q"));
            Menu.Add("useW", new CheckBox("Use E"));
            Menu.Add("useE", new CheckBox("Use W"));
            Menu.Add("useR", new CheckBox("Use R"));
            Menu.Add("manaW", new CheckBox("Keep Mana For W"));
            Menu.Add("rLimit", new Slider("R stack limit", 3, 1, 6));
            Menu.AddSeparator();
            Menu.AddGroupLabel("Jungle Clear");
            Menu.Add("useWJG", new CheckBox("Use W"));
            Menu.AddSeparator();

            Q = new Spell.Skillshot(SpellSlot.Q, 950, SkillShotType.Linear, 250, 1650, 70);
            W = new Spell.Active(SpellSlot.W, (uint)myHero.GetAutoAttackRange());
            E = new Spell.Skillshot(SpellSlot.E, 650, SkillShotType.Linear, 500, 1400, 120);
            R = new Spell.Skillshot(SpellSlot.R, 1260, SkillShotType.Circular, 1500, int.MaxValue, 225);

            Drawing.OnDraw += OnDraw;
            Game.OnTick += OnTick;
            Orbwalker.OnPreAttack += OnPreAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        public static bool CanMove(float extraWindup)
        {
            if (LastAATick <= Environment.TickCount)
                return (Environment.TickCount + Game.Ping / 2 >= LastAATick + myHero.AttackCastDelay * 1000 + extraWindup);
            return false;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                LastAATick = Environment.TickCount;
            }
        }

        public static float getSpellMana(SpellSlot spell)
        {
            return myHero.Spellbook.GetSpell(spell).SData.Mana;
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

        private static void OnTick(EventArgs args)
        {


            if (ObjectManager.Player.IsDead) return;

            W = new Spell.Active(SpellSlot.W, (uint)(565 + 60 + W.Level * 30 + 65));
            R = new Spell.Skillshot(SpellSlot.R, (uint)(900 + R.Level * 300), SkillShotType.Circular, 1500, int.MaxValue, 225);

            if (CanMove(100))
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    if (useQ)
                    {
                        if (QIsReadyPerfectly())
                        {
                            if (manaW && W.Level > 0 ? ObjectManager.Player.Mana - getSpellMana(SpellSlot.Q) >= getSpellMana(SpellSlot.W) : true)
                            {
                                var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                                if (target != null)
                                {
                                    Q.Cast(target);
                                }
                            }
                        }
                    }


                    if (useW)
                    {
                        if (WIsReadyPerfectly())
                        {
                            if (EntityManager.Heroes.Enemies.Any(x => x.IsValidTarget(W.Range)))
                            {
                                W.Cast();
                            }
                        }

                    }


                    if (useE)
                    {
                        if (EIsReadyPerfectly())
                        {
                            if (manaW && W.Level > 0 ? ObjectManager.Player.Mana - getSpellMana(SpellSlot.E) >= getSpellMana(SpellSlot.W) : true)
                            {
                                var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                                if (target != null)
                                {
                                    E.Cast(target);
                                }
                            }
                        }
                    }


                    if (useR)
                    {

                        if (RIsReadyPerfectly())
                        {
                            if (manaW && W.Level > 0 ? ObjectManager.Player.Mana - getSpellMana(SpellSlot.R) >= getSpellMana(SpellSlot.W) : true)
                            {
                                if (ObjectManager.Player.GetBuffCount("kogmawlivingartillerycost") < rLimit)
                                {
                                    var target = TargetSelector.GetTarget(R.Range, DamageType.Magical);
                                    if (target != null)
                                        R.Cast(target);
                                }
                                else
                                {
                                    var killableTarget = EntityManager.Heroes.Enemies.FirstOrDefault(x => x.IsKillableAndValidTarget(myHero.GetSpellDamage(x, SpellSlot.R), DamageType.Magical, R.Range) && R.GetPrediction(x).HitChance >= HitChance.High);
                                    if (killableTarget != null)
                                        R.Cast(killableTarget);
                                }
                            }
                        }
                    }
                }
            }
        }

        internal static bool IsKillableAndValidTarget(this AIHeroClient target, double calculatedDamage, DamageType damageType, float distance = float.MaxValue)
        {
            if (target == null || !target.IsValidTarget(distance) || target.CharData.BaseSkinName == "gangplankbarrel")
                return false;

            if (target.HasBuff("kindredrnodeathbuff"))
            {
                return false;
            }

            // Tryndamere's Undying Rage (R)
            if (target.HasBuff("Undying Rage"))
            {
                return false;
            }

            // Kayle's Intervention (R)
            if (target.HasBuff("JudicatorIntervention"))
            {
                return false;
            }

            // Poppy's Diplomatic Immunity (R)
            if (target.HasBuff("DiplomaticImmunity") && !ObjectManager.Player.HasBuff("poppyulttargetmark"))
            {
                //TODO: Get the actual target mark buff name
                return false;
            }

            // Banshee's Veil (PASSIVE)
            if (target.HasBuff("BansheesVeil"))
            {
                // TODO: Get exact Banshee's Veil buff name.
                return false;
            }

            // Sivir's Spell Shield (E)
            if (target.HasBuff("SivirShield"))
            {
                // TODO: Get exact Sivir's Spell Shield buff name
                return false;
            }

            // Nocturne's Shroud of Darkness (W)
            if (target.HasBuff("ShroudofDarkness"))
            {
                // TODO: Get exact Nocturne's Shourd of Darkness buff name
                return false;
            }

            if (ObjectManager.Player.HasBuff("summonerexhaust"))
                calculatedDamage *= 0.6;

            if (target.ChampionName == "Blitzcrank")
                if (!target.HasBuff("manabarriercooldown"))
                    if (target.Health + target.HPRegenRate +
                        (damageType == DamageType.Physical ? target.AttackShield : target.MagicShield) +
                        target.Mana * 0.6 + target.PARRegenRate < calculatedDamage)
                        return true;

            if (target.ChampionName == "Garen")
                if (target.HasBuff("GarenW"))
                    calculatedDamage *= 0.7;

            if (target.HasBuff("FerociousHowl"))
                calculatedDamage *= 0.3;

            return target.Health + target.HPRegenRate + (damageType == DamageType.Physical ? target.AttackShield : target.MagicShield) < calculatedDamage - 2;
        }

        private static void OnDraw(EventArgs args)
        {
        }

        private static void OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (!args.Target.IsMe) return;

            if (IsZombie)
                args.Process = false;

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                if (WIsReadyPerfectly())
                    if (useW)
                        if (args.Target.IsValidTarget(W.Range))
                            W.Cast();
            }
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                if (WIsReadyPerfectly())
                    if (Menu.AddSeparator())
                        if (EntityManager.MinionsAndMonsters.GetJungleMonsters().Count() >= 1)
                            W.Cast();
            }
        }
    }
}
