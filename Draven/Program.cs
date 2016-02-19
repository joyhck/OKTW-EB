﻿using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using System.Diagnostics.CodeAnalysis;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using Color = System.Drawing.Color;

namespace RevampedDraven
{
    internal class AxeDropObjectData
    {
        internal int ExpireTime;
        internal GameObject Object;
    }

    public static class Program
    {
        private static readonly List<AxeDropObjectData> _axeDropObjectDataList = new List<AxeDropObjectData>();
        private static GameObject _bestDropObject;

        private static Spell.Skillshot E, R;
        private static Spell.Active Q, W;

        private static Menu Menu;

        private static int AxeCount
        {
            get
            {
                var buff = myHero.GetBuff("dravenspinningattack");
                return buff == null ? 0 : buff.Count + _axeDropObjectDataList.Count(x => x.Object.IsValid);
            }
        }

        private static int LastAATick;

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
        public static bool interrupt { get { return Menu["interrupt"].Cast<CheckBox>().CurrentValue; } }
        public static bool gapcloser { get { return Menu["gapcloser"].Cast<CheckBox>().CurrentValue; } }
        public static bool catchaxe { get { return Menu["catchaxe"].Cast<CheckBox>().CurrentValue; } }
        public static int mine { get { return Menu["mine"].Cast<Slider>().CurrentValue; } }
        public static int catchaxerange { get { return Menu["catchaxerange"].Cast<Slider>().CurrentValue; } }
        public static bool drawe { get { return Menu["drawe"].Cast<CheckBox>().CurrentValue; } }
        public static bool drawr { get { return Menu["drawr"].Cast<CheckBox>().CurrentValue; } }
        public static bool drawaxe { get { return Menu["drawaxe"].Cast<CheckBox>().CurrentValue; } }
        public static bool drawaxedrop { get { return Menu["drawaxedrop"].Cast<CheckBox>().CurrentValue; } }
        public static bool useQH { get { return Menu["useQH"].Cast<CheckBox>().CurrentValue; } }
        public static bool useWH { get { return Menu["useWH"].Cast<CheckBox>().CurrentValue; } }
        public static bool useEH { get { return Menu["useEH"].Cast<CheckBox>().CurrentValue; } }
        public static int manaH { get { return Menu["manaH"].Cast<Slider>().CurrentValue; } }

        public static bool useELC { get { return Menu["useELC"].Cast<CheckBox>().CurrentValue; } }
        public static int manaLC { get { return Menu["manaLC"].Cast<Slider>().CurrentValue; } }

        public static bool useEJG { get { return Menu["useEJG"].Cast<CheckBox>().CurrentValue; } }
        public static int manaJG { get { return Menu["manaJG"].Cast<Slider>().CurrentValue; } }


        public static bool useQLC { get { return Menu["useQLC"].Cast<CheckBox>().CurrentValue; } }
        public static bool useQJG { get { return Menu["useQJG"].Cast<CheckBox>().CurrentValue; } }
        #endregion

        private static void OnLoad(EventArgs args)
        {
            if (myHero.Hero != Champion.Draven)
            {
                return;
            }

            EloBuddy.Chat.Print("<font color=\"#7CFC00\"><b>League Of Draven:</b></font> Loaded");

            Menu = MainMenu.AddMenu("League Of Draven", "draven");
            Menu.AddLabel("Ported from Exory's Ultima Series + Extra's - Berb");
            Menu.AddSeparator();

            Menu.AddGroupLabel("Combo");
            Menu.Add("useQ", new CheckBox("Use Q", true));
            Menu.Add("useW", new CheckBox("Use W", true));
            Menu.Add("useE", new CheckBox("Use E", true));
            Menu.Add("useR", new CheckBox("Use R", true));
            Menu.AddSeparator();
            Menu.AddGroupLabel("Harass");
            Menu.Add("useQH", new CheckBox("Use Q", true));
            Menu.Add("useWH", new CheckBox("Use W", true));
            Menu.Add("useEH", new CheckBox("Use E", true));
            Menu.Add("manaH", new Slider("Mininum Mana for Harass", 65, 0, 100));
            Menu.AddSeparator();
            Menu.AddGroupLabel("Lane Clear");
            Menu.Add("useQLC", new CheckBox("Use Q", true));
            Menu.Add("useELC", new CheckBox("Use E", true));
            Menu.Add("manaLC", new Slider("Mininum Mana for Lane Clear", 90, 0, 100));
            Menu.AddSeparator();
            Menu.AddGroupLabel("Jungle Clear");
            Menu.Add("useQJG", new CheckBox("Use Q", true));
            Menu.Add("useEJG", new CheckBox("Use E", true));
            Menu.Add("manaJG", new Slider("Mininum Mana for Jungle Clear", 65, 0, 100));
            Menu.AddSeparator();
            Menu.AddGroupLabel("Misc.");
            Menu.Add("interrupt", new CheckBox("Interrupter", true));
            Menu.Add("gapcloser", new CheckBox("Gapcloser", true));
            Menu.Add("catchaxe", new CheckBox("Auto Catch Axe", true));
            Menu.AddSeparator();
            Menu.Add("drawe", new CheckBox("Draw E", true));
            Menu.Add("drawr", new CheckBox("Draw R", true));
            Menu.Add("drawaxe", new CheckBox("Draw Axe Catch Range", true));
            Menu.Add("drawaxedrop", new CheckBox("Draw Axe Object", true));
            Menu.AddSeparator();
            Menu.Add("mine", new Slider("Mininum Mana for E", 65, 0, 100));
            Menu.Add("catchaxerange", new Slider("Axe Catch Range", 600, 0, 2000));

            Menu.AddSeparator();

            Q = new Spell.Active(SpellSlot.Q, (uint)myHero.GetAutoAttackRange());
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Skillshot(SpellSlot.E, 1000, SkillShotType.Linear, 250, 1400, 130);
            R = new Spell.Skillshot(SpellSlot.R, 2500, SkillShotType.Linear, 400, 2000, 160);

            Game.OnTick += OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            Player.OnIssueOrder += Player_OnIssueOrder;
            Gapcloser.OnGapcloser += OnEnemyGapcloser;
            Interrupter.OnInterruptableSpell += OnInterruptableTarget;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                LastAATick = Environment.TickCount;
            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (!myHero.IsDead)
            {
                if (ObjectManager.Get<GameObject>().Any(x => x.Name.Equals("Draven_Base_Q_reticle_self.troy")))
                {
                    var bestObject = ObjectManager.Get<GameObject>().First(x => x.Name.Equals("Draven_Base_Q_reticle_self.troy"));
                    if (bestObject == null)
                    {
                        Orbwalker.DisableAttacking = false;
                        Orbwalker.DisableMovement = false;
                    }
                }

                if (drawe && EIsReadyPerfectly())
                    Drawing.DrawCircle(myHero.Position, E.Range, Color.Red);

                if (drawr && RIsReadyPerfectly())
                    Drawing.DrawCircle(myHero.Position, R.Range, Color.Red);

                var DrawCatchAxeRange = drawaxe;
                if (DrawCatchAxeRange)
                {
                    Drawing.DrawCircle(Game.CursorPos, catchaxerange, Color.Red);
                }

                if (drawaxedrop)
                {
                    foreach (var data in _axeDropObjectDataList.Where(x => x.Object.IsValid))
                    {
                        var objectPos = Drawing.WorldToScreen(data.Object.Position);
                        Drawing.DrawCircle(data.Object.Position, 120, _bestDropObject != null && _bestDropObject.IsValid ? data.Object.NetworkId == _bestDropObject.NetworkId ? Color.YellowGreen : Color.Gray : Color.Gray);//, 3);
                        Drawing.DrawText(objectPos.X, objectPos.Y, _bestDropObject != null && _bestDropObject.IsValid ? data.Object.NetworkId == _bestDropObject.NetworkId ? Color.YellowGreen : Color.Gray : Color.Gray, ((float)(data.ExpireTime - Environment.TickCount) / 1000).ToString("0.0"));
                    }
                }
            }
        }

        static void Player_OnIssueOrder(Obj_AI_Base sender, PlayerIssueOrderEventArgs args)
        {
            if (catchaxe)
                if (sender.IsMe)
                    if (args.Order == GameObjectOrder.MoveTo)
                        if (_bestDropObject != null)
                            if (_bestDropObject.IsValid)
                                if (_bestDropObject.Position.Distance(myHero.Position) < 120)
                                    if (_bestDropObject.Position.Distance(args.TargetPosition) >= 120)
                                        for (var i = _bestDropObject.Position.Distance(args.TargetPosition);
                                            i > 0;
                                            i = i - 1)
                                        {
                                            var position = myHero.Position.Extend(args.TargetPosition, i);
                                            if (_bestDropObject.Position.Distance(position) < 120)
                                            {
                                                Player.IssueOrder(GameObjectOrder.MoveTo, (Vector3)position);
                                                args.Process = false;
                                                break;
                                            }
                                        }
        }

        static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Draven_Base_Q_reticle_self.troy")
                _axeDropObjectDataList.RemoveAll(x => x.Object.NetworkId == sender.NetworkId);
        }

        static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Draven_Base_Q_reticle_self.troy")
                _axeDropObjectDataList.Add(new AxeDropObjectData
                {
                    Object = sender,
                    ExpireTime = Environment.TickCount + 1200
                });
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

        static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            _axeDropObjectDataList.RemoveAll(x => !x.Object.IsValid);

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                if (args.Target.Type == GameObjectType.AIHeroClient)
                {
                    if (useQ)
                        if (AxeCount < 2)
                            if (QIsReadyPerfectly())
                                Q.Cast();
                }
            }
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                if (useQH)
                    if (myHero.IsManaPercentOkay(manaH))
                        if (AxeCount < 2)
                            if (QIsReadyPerfectly())
                                Q.Cast();
            }
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                if (EntityManager.MinionsAndMonsters.GetLaneMinions().Any(x => x.NetworkId == args.Target.NetworkId))
                {
                    if (useQLC)
                        if (myHero.IsManaPercentOkay(manaLC))
                            if (AxeCount < 2)
                                if (QIsReadyPerfectly())
                                    Q.Cast();
                }

                if (EntityManager.MinionsAndMonsters.GetJungleMonsters().Any(x => x.NetworkId == args.Target.NetworkId))
                {
                    if (useQJG)
                        if (myHero.IsManaPercentOkay(manaJG))
                            if (AxeCount < 2)
                                if (QIsReadyPerfectly())
                                    Q.Cast();
                }
            }
        }

        private static void OnInterruptableTarget(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (interrupt)
                if (EIsReadyPerfectly())
                    if (sender.IsValidTarget(E.Range))
                        E.Cast(sender);
        }

        private static void OnEnemyGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (gapcloser)
                if (EIsReadyPerfectly())
                    if (e.Sender.IsValidTarget(E.Range))
                        E.Cast(e.Sender);
        }

        public static bool CanMove(float extraWindup)
        {
            if (LastAATick <= Environment.TickCount)
                return (Environment.TickCount + Game.Ping / 2 >= LastAATick + myHero.AttackCastDelay * 1000 + extraWindup);
            return false;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (!myHero.IsDead)
            {
                var bestObjecta = _axeDropObjectDataList.Where(x => x.Object.IsValid).OrderBy(x => x.ExpireTime).FirstOrDefault();

                if (catchaxe) {
                    if (bestObjecta != null)
                    {
                        if (Game.CursorPos.Distance(bestObjecta.Object.Position) <= catchaxerange)
                        {
                            if (bestObjecta.Object.Position.Distance(myHero.ServerPosition) >= 0f)
                            {
                                Orbwalker.DisableMovement = false;
                                Orbwalker.OrbwalkTo(bestObjecta.Object.Position);
                                Orbwalker.DisableMovement = true;

                                Core.DelayAction(delegate { Orbwalker.DisableMovement = false; }, 50);
                            }
                        }
                    }
                }

                if (CanMove(100))
                {
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                    {
                        if (useW)
                        {
                            if (WIsReadyPerfectly())
                            {
                                if (!myHero.HasBuff("dravenfurybuff"))
                                {
                                    if (EntityManager.Heroes.Enemies.Any(x => x.IsValid && myHero.IsInAutoAttackRange(x)))
                                    {
                                        W.Cast();
                                    }
                                }
                            }
                        }

                        if (useE && myHero.ManaPercent >= mine)
                        {
                            if (EIsReadyPerfectly())
                            {
                                var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                                if (target != null)
                                    E.Cast(target);
                            }
                        }


                        if (useR)
                        {
                            if (RIsReadyPerfectly())
                            {
                                var target = EntityManager.Heroes.Enemies.FirstOrDefault(x => !myHero.IsInAutoAttackRange(x) && x.IsKillableAndValidTarget(myHero.GetSpellDamage(x, SpellSlot.R) * 2, DamageType.Physical, R.Range));
                                if (target != null)
                                    R.Cast(target);
                            }
                        }
                    }
                    else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                    {
                        if (useWH)
                            if (ObjectManager.Player.IsManaPercentOkay(manaH))
                                if (WIsReadyPerfectly())
                                    if (!ObjectManager.Player.HasBuff("dravenfurybuff"))
                                        if (EntityManager.Heroes.Enemies.Any(x => x.IsValid && myHero.IsInAutoAttackRange(x)))
                                            W.Cast();

                        if (useEH)
                            if (ObjectManager.Player.IsManaPercentOkay(manaH))
                                if (EIsReadyPerfectly())
                                {
                                    var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                                    if (target != null)
                                        E.Cast(target);
                                }
                    }
                    else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
                    {
                        foreach (var minion in EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.ServerPosition, E.Range))
                        {
                            if (useELC)
                            {
                                if (ObjectManager.Player.IsManaPercentOkay(manaLC))
                                {
                                    if (EIsReadyPerfectly())
                                    {
                                        var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.ServerPosition, E.Range);
                                        var farmLocation = EntityManager.MinionsAndMonsters.GetLineFarmLocation(minions, E.Width, (int)E.Range);
                                        if (farmLocation.HitNumber >= 3)
                                        {
                                            E.Cast(farmLocation.CastPosition);
                                        }
                                    }
                                }
                            }

                        }

                        foreach (var jungleMobs in ObjectManager.Get<Obj_AI_Minion>().Where(o => o.IsValidTarget(Program.E.Range) && o.Team == GameObjectTeam.Neutral && o.IsVisible && !o.IsDead))
                        {
                            if (useEJG)
                            {
                                if (ObjectManager.Player.IsManaPercentOkay(manaJG))
                                {
                                    if (EIsReadyPerfectly())
                                    {
                                        var minions = EntityManager.MinionsAndMonsters.GetJungleMonsters(myHero.ServerPosition, E.Range);
                                        var farmLocation = EntityManager.MinionsAndMonsters.GetLineFarmLocation(minions, E.Width, (int)E.Range);
                                        if (farmLocation.HitNumber >= 2)
                                        {
                                            E.Cast(farmLocation.CastPosition);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        internal static bool IsManaPercentOkay(this AIHeroClient hero, int manaPercent)
        {
            return myHero.ManaPercent > manaPercent;
        }

        internal static bool IsKillableAndValidTarget(this AIHeroClient target, double calculatedDamage, DamageType damageType, float distance = float.MaxValue)
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

    }
}
