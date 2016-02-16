using System;
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

namespace ExoryDraven
{
    public static class Program
    {
        private static Spell.Skillshot E, R;
        private static Spell.Active Q, W;

        private static Menu Menu;

        private static AIHeroClient myHero
        {
            get { return Player.Instance; }
        }

        public static void Main()
        {
            Loading.OnLoadingComplete += OnLoad;
        }

        #region Menu Items
        public static bool useQL { get { return Menu["useQL"].Cast<CheckBox>().CurrentValue; } }
        public static bool useW { get { return Menu["useW"].Cast<CheckBox>().CurrentValue; } }
        public static bool useE { get { return Menu["useE"].Cast<CheckBox>().CurrentValue; } }
        public static bool useR { get { return Menu["useR"].Cast<CheckBox>().CurrentValue; } }
        public static bool ph { get { return Menu["ph"].Cast<CheckBox>().CurrentValue; } }

        public static bool useEH { get { return Menu["useEH"].Cast<CheckBox>().CurrentValue; } }
        public static int hmanamanager { get { return Menu["hmanamanager"].Cast<Slider>().CurrentValue; } }

        public static bool useQLC { get { return Menu["useQLC"].Cast<CheckBox>().CurrentValue; } }
        public static bool useELC { get { return Menu["useELC"].Cast<CheckBox>().CurrentValue; } }
        public static int qstack { get { return Menu["qstack"].Cast<Slider>().CurrentValue; } }
        public static int lcmana { get { return Menu["lcmana"].Cast<Slider>().CurrentValue; } }

        public static bool useQJG { get { return Menu["useQJG"].Cast<CheckBox>().CurrentValue; } }
        public static bool useEJG { get { return Menu["useEJG"].Cast<CheckBox>().CurrentValue; } }
        public static int qjgstack { get { return Menu["qjgstack"].Cast<Slider>().CurrentValue; } }
        public static int jgmana { get { return Menu["jgmana"].Cast<Slider>().CurrentValue; } }

        public static bool DontCatchUnderTurret { get { return Menu["DontCatchUnderTurret"].Cast<CheckBox>().CurrentValue; } }

        public static bool rks { get { return Menu["rks"].Cast<CheckBox>().CurrentValue; } }
        public static bool eks { get { return Menu["eks"].Cast<CheckBox>().CurrentValue; } }

        public static bool intenemy { get { return Menu["intenemy"].Cast<CheckBox>().CurrentValue; } }
        public static bool gapclose { get { return Menu["gapclose"].Cast<CheckBox>().CurrentValue; } }
        public static bool useWSlow { get { return Menu["useWSlow"].Cast<CheckBox>().CurrentValue; } }
        public static bool useWavail { get { return Menu["useWavail"].Cast<CheckBox>().CurrentValue; } }
        public static bool killable { get { return Menu["killable"].Cast<CheckBox>().CurrentValue; } }

        public static int stopcatch { get { return Menu["stopcatch"].Cast<Slider>().CurrentValue; } }

        public static int manaforW { get { return Menu["manaforW"].Cast<Slider>().CurrentValue; } }

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
            Menu.Add("useQL", new CheckBox("Use Q", true));
            Menu.Add("useW", new CheckBox("Use W", true));
            Menu.Add("useE", new CheckBox("Use E", true));
            Menu.Add("useR", new CheckBox("Use R", true));
            Menu.Add("ph", new CheckBox("Catch Axe/Axe Helper?", true));
            Menu.Add("killable", new CheckBox("If target killable with 3 auto's stop catch?", true));
            Menu.AddSeparator();

            Menu.AddGroupLabel("Harass");
            Menu.Add("useEH", new CheckBox("Use E", true));
            Menu.Add("hmanamanager", new Slider("[Harass] Mana Manager : ", 90, 1, 100));
            Menu.AddSeparator();

            Menu.AddGroupLabel("Lane Clear");
            Menu.Add("useQLC", new CheckBox("Use Q", true));
            Menu.Add("useELC", new CheckBox("Use E", false));
            Menu.Add("qstack", new Slider("[Lane Clear] Max Q Stack ", 1, 1, 2));
            Menu.Add("lcmana", new Slider("[Lane Clear] Mana Manager (Keep this high or do it manually) : ", 80, 1, 100));
            Menu.AddSeparator();

            Menu.AddGroupLabel("Jungle Clear");
            Menu.Add("useQJG", new CheckBox("Use Q", true));
            Menu.Add("useEJG", new CheckBox("Use E", true));
            Menu.Add("qjgstack", new Slider("[Jungle] Max Q Stack ", 2, 1, 2));
            Menu.Add("jgmana", new Slider("[Jungle] Mana Manager (Keep this high or do it manually) : ", 80, 1, 100));
            Menu.AddSeparator();

            Menu.AddGroupLabel("Misc.");
            Menu.Add("DontCatchUnderTurret", new CheckBox("Don't Catch Axe Under Turret", true));
            Menu.AddSeparator();
            Menu.Add("eks", new CheckBox("Use E KS", true));
            Menu.Add("rks", new CheckBox("Use R KS", true));
            Menu.AddSeparator();
            Menu.Add("intenemy", new CheckBox("Use E to Interrupt Enemy Channels", true));
            Menu.Add("gapclose", new CheckBox("Use E to Interrupt GapClosers", true));
            Menu.Add("useWSlow", new CheckBox("Use W if slowed", true));
            Menu.Add("useWavail", new CheckBox("Use W when available", false));
            Menu.AddSeparator();
            Menu.Add("stopcatch", new Slider("If lower than X% of HP = stop catching : ", 10, 1, 30));
            Menu.AddSeparator();
            Menu.Add("manaforW", new Slider("Universal Mana Manager for W : ", 40, 1, 100));


            Menu.AddSeparator();

            Q = new Spell.Active(SpellSlot.Q, (uint)myHero.GetAutoAttackRange());
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Skillshot(SpellSlot.E, 1059, SkillShotType.Linear, 250, 1400, 130);
            R = new Spell.Skillshot(SpellSlot.R, 1500, SkillShotType.Linear, 400, 2000, 160);

            Game.OnTick += OnUpdate;

            Gapcloser.OnGapcloser += OnEnemyGapcloser;
            Interrupter.OnInterruptableSpell += OnInterruptableTarget;
        }

        private static void OnInterruptableTarget(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (E.IsReady() && !IsSpellShielded(sender) && sender.IsValidTarget(E.Range) && intenemy)
            {
                E.Cast(sender.Position);
            }
        }

        private static void OnEnemyGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (E.IsReady() && !IsSpellShielded(e.Sender) && myHero.Distance(e.End) < E.Range && gapclose)
            {
                E.Cast(e.End);
            }
        }

        public static bool IsSpellShielded(Obj_AI_Base unit)
        {
            return unit.HasBuffOfType(BuffType.SpellShield) || unit.HasBuffOfType(BuffType.SpellImmunity);
        }

        private static void OnUpdate(EventArgs args)
        {
            if (!myHero.IsDead)
            {
                ExecutePathing(args);

                KS(args);

                qssCheck();

                var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);

                if (useWSlow && myHero.HasBuffOfType(BuffType.Slow) && myHero.ManaPercent > manaforW && W.IsReady())
                {
                    W.Cast();
                }

                if (useWavail && myHero.ManaPercent > manaforW && W.IsReady())
                {
                    W.Cast();
                }

                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    useItems(target);
                    if (target != null)
                    {
                        ExecuteQ(args);
                    }

                    if (target != null && target.IsValidTarget() && !target.IsInvulnerable && !IsSpellShielded(target))
                    {
                        ExecuteAuto(args);
                    }
                }

                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                {
                    Harass();
                }

                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
                {
                    Clear();
                }
            }
        }

        private static void useItems(AIHeroClient target)
        {
            if (Item.HasItem(ItemId.Youmuus_Ghostblade) && Item.CanUseItem(ItemId.Quicksilver_Sash) && target.Distance(myHero) >= myHero.GetAutoAttackRange())
            {
                Item.UseItem(ItemId.Youmuus_Ghostblade);
            }
        }

        private static void qssCheck()
        {
            var target = myHero;
            if (target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Knockup) || target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Fear) || target.HasBuffOfType(BuffType.Knockback) || target.HasBuffOfType(BuffType.Taunt) || target.HasBuffOfType(BuffType.Suppression) || target.IsStunned)
            {
                if ((Item.HasItem(ItemId.Quicksilver_Sash) && Item.CanUseItem(ItemId.Quicksilver_Sash)))
                {
                    Item.UseItem(ItemId.Quicksilver_Sash);
                }
                if ((Item.HasItem(ItemId.Mercurial_Scimitar) && Item.CanUseItem(ItemId.Mercurial_Scimitar)))
                {
                    Item.UseItem(ItemId.Mercurial_Scimitar);
                }
            }
        }

        public static int QCount
        {
            get
            {
                return (myHero.HasBuff("dravenspinning") ? 1 : 0) + (myHero.HasBuff("dravenspinningleft") ? 1 : 0);
            }
        }

        private static void Clear()
        {
            #region Lane Clear
            foreach (var minion in EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.ServerPosition, myHero.GetAutoAttackRange()))
            {
                if (myHero.ManaPercent < lcmana)
                {
                    return;
                }

                if (Q.IsReady() && QCount < qstack && useQLC && Q.IsInRange(minion))
                {
                    Q.Cast();
                }

                if (E.IsReady() && useELC && E.IsInRange(minion.Position))
                {
                    E.Cast(minion);
                }
            }

            #endregion

            #region Jungle
            foreach (var jungleMobs in ObjectManager.Get<Obj_AI_Minion>().Where(o => o.IsValidTarget(Program.Q.Range) && o.Team == GameObjectTeam.Neutral && o.IsVisible && !o.IsDead))
            {
                if (myHero.ManaPercent < jgmana)
                {
                    return;
                }

                if (jungleMobs.BaseSkinName == "SRU_Red" || jungleMobs.BaseSkinName == "SRU_Blue" || jungleMobs.BaseSkinName == "SRU_Gromp" || jungleMobs.BaseSkinName == "SRU_Murkwolf" || jungleMobs.BaseSkinName == "SRU_Razorbeak" || jungleMobs.BaseSkinName == "SRU_Krug" || jungleMobs.BaseSkinName == "Sru_Crab")
                {
                    if (E.IsReady() && useEJG && jungleMobs.IsInRange(myHero, E.Range))
                    {
                        E.Cast(jungleMobs);
                    }
                    if (Q.IsReady() && QCount < qjgstack && useQJG && jungleMobs.IsInRange(myHero, myHero.GetAutoAttackRange()))
                    {
                        Q.Cast();
                    }
                }
            }

            #endregion
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
            if (target == null) { return; }

            if (myHero.ManaPercent < hmanamanager) { return; }

            if (E.IsReady() && useEH && E.IsInRange(target))
            {
                E.Cast(target);
            }
        }

        public static void ExecuteQ(EventArgs args)
        {
            if (Q.IsReady() && QCount < 2 && useQL)
            {
                Q.Cast();
            }
        }

        public static void KS(EventArgs args)
        {
            if (E.IsReady() && eks)
            {
                foreach (var target in EntityManager.Heroes.Enemies.Where(t => t.IsValidTarget(E.Range) && t.Health < myHero.GetSpellDamage(t, SpellSlot.E)))
                {
                    E.Cast(target);
                }
            }

            if (R.IsReady() && rks)
            {
                foreach (var target in EntityManager.Heroes.Enemies.Where(t => t.IsValidTarget(R.Range) && t.Health < myHero.GetSpellDamage(t, SpellSlot.R) && (!t.IsValidTarget(E.Range) || !E.IsReady())))
                {
                    R.Cast(target);
                }
            }
        }

        public static bool UnderEnemyTower(Vector3 pos)
        {
            return EntityManager.Turrets.Enemies.Any(a => a.Distance(pos) < 950);
        }

        public static void ExecutePathing(EventArgs args)
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

            if (ph)
            {
                if (stopcatch >= myHero.HealthPercent || myHero.IsRecalling())
                {
                    return;
                }

                if (killable && target != null)
                {
                    if (myHero.GetAutoAttackDamage(target) * 2 > target.Health)
                    {
                        return;
                    }
                }

                if (ObjectManager.Get<GameObject>().Any(x => x.Name.Equals("Draven_Base_Q_reticle_self.troy")) && ObjectManager.Get<GameObject>().First(x => x.Name.Equals("Draven_Base_Q_reticle_self.troy")).Position.Distance(myHero.ServerPosition) >= 0f)
                {

                    if (UnderEnemyTower(ObjectManager.Get<GameObject>().FirstOrDefault(x => x.Name.Equals("Draven_Base_Q_reticle_self.troy")).Position) && DontCatchUnderTurret)
                    {
                        return;
                    }

                    if (ObjectManager.Get<GameObject>().FirstOrDefault(x => x.Name.Equals("Draven_Base_Q_reticle_self.troy")).Position.Distance(myHero.ServerPosition) <= 92f)
                    {
                        Orbwalker.DisableMovement = true;
                        Orbwalker.OrbwalkTo(ObjectManager.Get<GameObject>().FirstOrDefault(x => x.Name.Equals("Draven_Base_Q_reticle_self.troy")).Position);
						Orbwalker.DisableMovement = false;
                        return;
                    }
                    else
                    {
                        Orbwalker.DisableMovement = false;
                        Orbwalker.DisableAttacking = true;
                        Orbwalker.OrbwalkTo(ObjectManager.Get<GameObject>().FirstOrDefault(x => x.Name.Equals("Draven_Base_Q_reticle_self.troy")).Position);
                        Orbwalker.DisableMovement = true;
                        Orbwalker.DisableAttacking = false;
                    }
                }
                else
                {
                    Orbwalker.DisableMovement = !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None);
                }
            }
        }

        public static void ExecuteAuto(EventArgs args)
        {

            var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);

            if (W.IsReady() && target.IsValidTarget(800f) && !myHero.HasBuff("dravenfurybuff") && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && useW)
            {
                if (myHero.ManaPercent > manaforW)
                {
                    W.Cast();
                }
            }

            if (E.IsReady() && E.IsInRange(target) && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                E.Cast(target);
            }

            if (R.IsReady() && R.IsInRange(target) && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && myHero.CountEnemiesInRange(myHero.AttackRange + 100) >= 2 && useR)
            {
                R.Cast(target);
            }
        }
    }
}
