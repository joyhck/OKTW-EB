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

namespace OKTWJayce
{
    public static class Program
    {

        private static Spell.Skillshot Q, Qext, QextCol, E;
        private static Spell.Targeted Q2, E2;
        private static Spell.Active W, W2, R;

        private static float Qcd, Wcd, Ecd, Q2cd, W2cd, E2cd;
        private static float Qcdt, Wcdt, Ecdt, Q2cdt, W2cdt, E2cdt;

        private static Vector3 EcastPos;

        private static int Etick = 0;

        private static float QMANA = 0, WMANA = 0, EMANA = 0, QMANA2 = 0, WMANA2 = 0, EMANA2 = 0, RMANA = 0;

        private static Menu Menu;

        private static bool Range { get { return myHero.HasBuff("jaycestancegun"); } }

        private static AIHeroClient myHero
        {
            get { return Player.Instance; }
        }
        public static bool Farm { get { return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit); } }

        public static bool LaneClear { get { return (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear)); } }


        public static void Main()
        {
            Loading.OnLoadingComplete += OnLoad;
        }

        #region Menu Items
        public static bool getGapCloseE { get { return Menu["gapE"].Cast<CheckBox>().CurrentValue; } }
        public static bool autoE { get { return Menu["autoE"].Cast<CheckBox>().CurrentValue; } }
        public static bool QEsplash { get { return Menu["QEsplash"].Cast<CheckBox>().CurrentValue; } }
        public static int QEsplashAdjust { get { return Menu["QEsplashAdjust"].Cast<Slider>().CurrentValue; } }
        public static bool autoRm { get { return Menu["autoRm"].Cast<CheckBox>().CurrentValue; } }
        public static bool autoR { get { return Menu["autoR"].Cast<CheckBox>().CurrentValue; } }
        public static bool autoEks { get { return Menu["autoEks"].Cast<CheckBox>().CurrentValue; } }
        public static int harassMana { get { return Menu["harassMana"].Cast<Slider>().CurrentValue; } }
        public static bool autoW { get { return Menu["autoW"].Cast<CheckBox>().CurrentValue; } }
        public static bool intE { get { return Menu["intE"].Cast<CheckBox>().CurrentValue; } }
        public static bool autoQ { get { return Menu["autoQ"].Cast<CheckBox>().CurrentValue; } }
        public static bool autoQm { get { return Menu["autoQm"].Cast<CheckBox>().CurrentValue; } }
        public static bool autoWm { get { return Menu["autoWm"].Cast<CheckBox>().CurrentValue; } }
        public static bool autoEm { get { return Menu["autoEm"].Cast<CheckBox>().CurrentValue; } }
        public static int Mana { get { return Menu["Mana"].Cast<Slider>().CurrentValue; } }
        public static int LCminions { get { return Menu["LCminions"].Cast<Slider>().CurrentValue; } }
        public static int qeset { get { return Menu["qeset"].Cast<Slider>().CurrentValue; } }
        public static bool farmQ { get { return Menu["farmQ"].Cast<CheckBox>().CurrentValue; } }
        public static bool farmW { get { return Menu["farmW"].Cast<CheckBox>().CurrentValue; } }
        public static bool jungleQ { get { return Menu["jungleQ"].Cast<CheckBox>().CurrentValue; } }
        public static bool jungleE { get { return Menu["jungleE"].Cast<CheckBox>().CurrentValue; } }
        public static bool jungleR { get { return Menu["jungleR"].Cast<CheckBox>().CurrentValue; } }
        public static bool jungleQm { get { return Menu["jungleQm"].Cast<CheckBox>().CurrentValue; } }
        public static bool jungleWm { get { return Menu["jungleWm"].Cast<CheckBox>().CurrentValue; } }
        public static bool jungleEm { get { return Menu["jungleEm"].Cast<CheckBox>().CurrentValue; } }
        public static bool showcd { get { return Menu["showcd"].Cast<CheckBox>().CurrentValue; } }
        public static bool onlyRdy { get { return Menu["onlyRdy"].Cast<CheckBox>().CurrentValue; } }
        public static bool qRange { get { return Menu["qRange"].Cast<CheckBox>().CurrentValue; } }
        #endregion

        private static void OnLoad(EventArgs args)
        {
            if (myHero.Hero != Champion.Jayce)
            {
                return;
            }

            Menu = MainMenu.AddMenu("OKTW Jayce", "jayce");
            Menu.AddLabel("Ported from Sebby's OKTW - Berb");
            Menu.AddGroupLabel("Ex : Q1 = Ranged");
            Menu.AddGroupLabel("Ex : Q2 = Melee");
            Menu.AddSeparator();
            Menu.AddGroupLabel("Combo");
            Menu.Add("autoQ", new CheckBox("Use Q1"));
            Menu.Add("autoW", new CheckBox("Use W1"));
            Menu.Add("autoE", new CheckBox("Use E1 (Q + E)"));
            Menu.AddSeparator();
            Menu.Add("autoQm", new CheckBox("Use Q2"));
            Menu.Add("autoWm", new CheckBox("Use W2"));
            Menu.Add("autoEm", new CheckBox("Use E2"));
            Menu.AddSeparator();
            Menu.Add("autoRm", new CheckBox("Automatic R logic to melee"));
            Menu.Add("autoR", new CheckBox("Automatic R logic to ranged"));
            Menu.AddSeparator();

            Menu.AddGroupLabel("Harass");
            foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(enemy => enemy.Team != myHero.Team))
                Menu.Add("haras" + enemy.ChampionName, new CheckBox("Harass " + enemy.ChampionName));

            Menu.Add("harassMana", new Slider("Harass Mana", 80, 0, 100));
            Menu.AddSeparator();

            Menu.AddGroupLabel("Laneclear");
            Menu.Add("farmQ", new CheckBox("Use Q2 + E2"));
            Menu.Add("farmW", new CheckBox("Use W1 & W2"));
            Menu.AddSeparator();
            Menu.Add("Mana", new Slider("Mana Manager", 80, 0, 100));
            Menu.Add("LCminions", new Slider("Minimum Minions", 2, 0, 10));
            Menu.AddSeparator();

            Menu.AddGroupLabel("Jungle clear");
            Menu.Add("jungleQ", new CheckBox("Use Q1"));
            Menu.Add("jungleE", new CheckBox("Use E1"));
            Menu.AddSeparator();
            Menu.Add("jungleQm", new CheckBox("Use Q2"));
            Menu.Add("jungleWm", new CheckBox("Use W2"));
            Menu.Add("jungleEm", new CheckBox("Use E2"));
            Menu.AddSeparator();
            Menu.Add("jungleR", new CheckBox("Use R to switch"));
            Menu.AddSeparator();

            Menu.AddGroupLabel("Drawings");
            Menu.Add("showcd", new CheckBox("Show cooldown"));
            Menu.Add("onlyRdy", new CheckBox("Draw only ready spells"));
            Menu.Add("qRange", new CheckBox("Show [Q1+E1]/[Q2] range"));
            Menu.AddSeparator();

            Menu.AddGroupLabel("Misc");
            Menu.Add("QEsplash", new CheckBox("Q + E splash minion damage"));
            Menu.Add("QEsplashAdjust", new Slider("Q + E splash minion radius", 150, 50, 250));
            Menu.AddSeparator();
            Menu.Add("gapE", new CheckBox("Gapclose R + E2"));
            Menu.Add("intE", new CheckBox("Interrupt spells"));
            Menu.AddSeparator();
            Menu.Add("autoEks", new CheckBox("Use E2 to KS only?"));
            Menu.AddSeparator();
            Menu.AddLabel("1 : Only Force E1 > Q1 in Combo");
            Menu.AddLabel("2 : NEVER use E1 after Q1");
            Menu.AddLabel("3 : Always");
            Menu.AddLabel("4 : Only E1 > Q1 in harass");
            Menu.AddLabel("5 : Only E1 > Q1 in combo & harass");
            Menu.Add("qeset", new Slider("E1 > Q1 Combo settings : (^^) ", 3, 1, 5));
            Menu.AddSeparator();


            Q = new Spell.Skillshot(SpellSlot.Q, 1030, SkillShotType.Linear, 25, 1450, 70);
            Qext = new Spell.Skillshot(SpellSlot.Q, 1650, SkillShotType.Linear, 30, 2000, 80);
            QextCol = new Spell.Skillshot(SpellSlot.Q, 1650, SkillShotType.Linear, 30, 1600, 100);
            Q2 = new Spell.Targeted(SpellSlot.Q, 600);

            W = new Spell.Active(SpellSlot.W);
            W2 = new Spell.Active(SpellSlot.W, 350);

            E = new Spell.Skillshot(SpellSlot.E, 650, SkillShotType.Linear, 10, int.MaxValue, 120);
            E2 = new Spell.Targeted(SpellSlot.E, 240);

            R = new Spell.Active(SpellSlot.R);

            Drawing.OnDraw += OnDraw;
            Game.OnTick += OnTick;

            Orbwalker.OnPreAttack += OnPreAttack;

            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            Spellbook.OnCastSpell += OnCastSpell;

            Interrupter.OnInterruptableSpell += OnInterruptableSpell;
            Gapcloser.OnGapcloser += OnGapCloser;
        }

        private static void FleeMode()
        {
            if (Range)
            {
                if (E.IsReady())
                    E.Cast((Vector3)myHero.Position.Extend(Game.CursorPos, 150));
                else if (R.IsReady())
                    R.Cast();
            }
            else
            {
                if (Q2.IsReady())
                {
                    var mobs = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.ServerPosition, Q2.Range);

                    if (mobs.Count() > 0)
                    {
                        Obj_AI_Base best;
                        best = mobs.First();

                        foreach (var mob in mobs.Where(mob => mob.IsValidTarget(Q2.Range)))
                        {
                            if (mob.Distance(Game.CursorPos) < best.Distance(Game.CursorPos))
                                best = mob;
                        }
                        if (best.Distance(Game.CursorPos) + 200 < myHero.Distance(Game.CursorPos))
                            Q2.Cast(best);
                    }
                    else if (R.IsReady())
                        R.Cast();
                }
                else if (R.IsReady())
                    R.Cast();
            }
        }

        private static void OnTick(EventArgs args)
        {
            if (Range && E.IsReady() && Environment.TickCount - Etick < 250 + Game.Ping)
            {
                E.Cast(EcastPos);
            }

            if (Range)
            {
                if (Q.IsReady() && autoQ)
                    LogicQ();

                if (W.IsReady() && autoW)
                    LogicW();
            }
            else
            {
                if (autoEm)
                    LogicE2();
                if (autoQm)
                    LogicQ2();
                if (autoWm)
                    LogicW2();
            }
            SetValue();
            if (R.IsReady())
                LogicR();

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                Jungle();
                LaneClearLogic();
            }

        }

        private static void LaneClearLogic()
        {
            if (Range && Q.IsReady() && E.IsReady() && myHero.ManaPercent > Mana && farmQ && myHero.Mana > RMANA + WMANA)
            {
                var mobs = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.ServerPosition, Q.Range);
                if (!mobs.Any()) return;
                var QMinions = mobs.OrderBy(it => it.Health);
                Console.WriteLine("got here!");
                if (mobs.Count(it => it.IsValidTarget(Q.Range)) >= LCminions)
                    Q.Cast(QMinions.First());
            }

            if (W.IsReady() && myHero.ManaPercent > Mana && farmW)
            {
                if (Range)
                {
                    var mobs = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.ServerPosition, 550);
                    if (mobs.Count() >= LCminions)
                    {
                        W.Cast();
                    }
                }
                else
                {
                    var mobs = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.ServerPosition, 300);
                    if (mobs.Count() >= LCminions)
                    {
                        W.Cast();
                    }
                }
            }
        }

        private static void Jungle()
        {
            if (Program.LaneClear && myHero.Mana > RMANA + WMANA + WMANA)
            {

                var jgminion = EntityManager.MinionsAndMonsters.GetJungleMonsters().OrderByDescending(j => j.Health).FirstOrDefault(j => j.IsValidTarget(Q.Range));

                if (jgminion != null)
                {
                    if (Range)
                    {
                        if (Q.IsReady() && jungleQ)
                        {
                            Q.Cast(jgminion.ServerPosition);
                            return;
                        }
                        if (W.IsReady() && jungleE)
                        {
                            if (myHero.IsInAutoAttackRange(jgminion))
                                W.Cast();
                            return;
                        }
                        if (jungleR)
                            R.Cast();
                    }
                    else
                    {
                        if (Q2.IsReady() && jungleQm && jgminion.IsValidTarget(Q2.Range))
                        {
                            Q2.Cast(jgminion);
                            return;
                        }

                        if (W2.IsReady() && jungleWm)
                        {
                            if (jgminion.IsValidTarget(300))
                                W.Cast();
                            return;
                        }
                        if (E2.IsReady() && jungleEm && jgminion.IsValidTarget(E2.Range))
                        {
                            if (jgminion.IsValidTarget(E2.Range))
                                E2.Cast(jgminion);
                            return;
                        }
                        if (jungleR)
                            R.Cast();
                    }
                }
            }
        }

        private static void LogicQ()
        {
            var Qtype = Q;
            if (CanUseQE())
                Qtype = Qext;

            var t = TargetSelector.GetTarget(Qtype.Range, DamageType.Physical);

            if (t.IsValidTarget())
            {
                var qDmg = myHero.GetSpellDamage(t, SpellSlot.Q);

                if (CanUseQE())
                {
                    qDmg = qDmg * 1.4f;
                }

                if (qDmg > t.Health)
                    CastQ(t);
                else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && myHero.Mana > EMANA + QMANA)
                    CastQ(t);
                else if (Farm && myHero.ManaPercent > harassMana)
                {
                    foreach (var enemy in EntityManager.Heroes.Enemies.Where(enemy => enemy.IsValidTarget(Qtype.Range) && Menu["haras" + enemy.ChampionName].Cast<CheckBox>().CurrentValue))
                    {
                        CastQ(t);
                    }
                }
                else if ((Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) || Farm) && myHero.Mana > RMANA + QMANA + EMANA)
                {
                    foreach (var enemy in EntityManager.Heroes.Enemies.Where(enemy => enemy.IsValidTarget(Qtype.Range) && !CanMove(enemy)))
                        CastQ(t);
                }
            }
        }

        public static bool CanMove(AIHeroClient target)
        {
            if (target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Knockup) ||
                target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Fear) || target.HasBuffOfType(BuffType.Knockback) ||
                target.HasBuffOfType(BuffType.Taunt) || target.HasBuffOfType(BuffType.Suppression) ||
                target.IsStunned || (!target.IsMoving) || target.MoveSpeed < 50)
            {
                return false;
            }
            else
                return true;
        }

        private static void LogicW()
        {
            var t = TargetSelector.GetTarget(myHero.GetAutoAttackRange(), DamageType.Physical);

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && R.IsReady() && Range && t.IsValidTarget())
            {
                W.Cast();
            }
        }

        private static void LogicE()
        {
            var t = TargetSelector.GetTarget(E2.Range, DamageType.Physical);

            if (t.IsValidTarget())
            {
                var qDmg = myHero.GetSpellDamage(t, SpellSlot.E);
                if (qDmg > t.Health)
                    E2.Cast(t);
                else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && myHero.Mana > RMANA + QMANA)
                    E2.Cast(t);
            }
        }

        private static void LogicQ2()
        {
            var t = TargetSelector.GetTarget(Q2.Range, DamageType.Physical);

            if (t.IsValidTarget())
            {
                if (myHero.GetSpellDamage(t, SpellSlot.Q) > t.Health)
                    Q2.Cast(t);
                else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && myHero.Mana > RMANA + QMANA)
                    Q2.Cast(t);
            }
        }

        private static void LogicW2()
        {
            if (myHero.CountEnemiesInRange(300) > 0 && myHero.Mana > 80)
                W.Cast();
        }

        private static void LogicE2()
        {
            var t = TargetSelector.GetTarget(E2.Range, DamageType.Physical);
            if (t.IsValidTarget() && E2.IsInRange(t) && E2.IsReady() && E2.IsLearned)
            {
                if (GetRealDamage(SpellSlot.E, t) > t.Health && !Range)
                {
                    E2.Cast(t);
                }
                else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && !autoEks && !Player.HasBuff("jaycehyperchargevfx") && !Range)
                    E2.Cast(t);
            }
        }

        private static void LogicR()
        {
            if (Range && autoRm)
            {
                var t = TargetSelector.GetTarget(Q2.Range + 200, DamageType.Physical);
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && Qcd > 0.5 && t.IsValidTarget() && ((!W.IsReady() && !t.IsMelee) || (!W.IsReady() && !Player.HasBuff("jaycehyperchargevfx") && t.IsMelee)))
                {
                    if (Q2cd < 0.5 && t.CountEnemiesInRange(800) < 3)
                        R.Cast();
                    else if (myHero.CountEnemiesInRange(300) > 0 && E2cd < 0.5)
                        R.Cast();
                }
            }
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && autoR)
            {
                var t = TargetSelector.GetTarget(1400, DamageType.Physical);
                if (t.IsValidTarget() && !t.IsValidTarget(Q2.Range + 200) && myHero.GetSpellDamage(t, SpellSlot.Q) * 1.4 > t.Health && Qcd < 0.5 && Ecd < 0.5)
                {
                    R.Cast();
                }

                if (!Q.IsReady() && (!E.IsReady() || autoEks))
                {
                    R.Cast();
                }
            }
        }

        private static void CastQ(Obj_AI_Base t)
        {
            if (!CanUseQE())
            {
                Q.Cast(t);
                return;
            }

            bool cast = true;

            if (QEsplash)
            {
                var poutput = QextCol.GetPrediction(t);

                foreach (var minion in poutput.CollisionObjects.Where(minion => minion.IsEnemy && minion.Distance(poutput.CastPosition) > QEsplashAdjust))
                {
                    cast = false;
                    break;
                }
            }
            else
                cast = false;

            if (cast)
                Qext.Cast(t);
            else
                QextCol.Cast(t);
        }

        private static bool CanUseQE()
        {
            if (E.IsReady() && myHero.Mana > QMANA + EMANA && autoE)
                return true;
            else
                return false;
        }

        private static void SetValue()
        {
            if (Range)
            {
                Qcdt = myHero.Spellbook.GetSpell(SpellSlot.Q).CooldownExpires;
                Wcdt = myHero.Spellbook.GetSpell(SpellSlot.W).CooldownExpires;
                Ecd = myHero.Spellbook.GetSpell(SpellSlot.E).CooldownExpires;

                QMANA = myHero.Spellbook.GetSpell(SpellSlot.Q).SData.Mana;
                WMANA = myHero.Spellbook.GetSpell(SpellSlot.W).SData.Mana;
                EMANA = myHero.Spellbook.GetSpell(SpellSlot.E).SData.Mana;
            }
            else
            {
                Q2cdt = myHero.Spellbook.GetSpell(SpellSlot.Q).CooldownExpires;
                W2cdt = myHero.Spellbook.GetSpell(SpellSlot.W).CooldownExpires;
                E2cdt = myHero.Spellbook.GetSpell(SpellSlot.E).CooldownExpires;

                QMANA2 = myHero.Spellbook.GetSpell(SpellSlot.Q).SData.Mana;
                WMANA2 = myHero.Spellbook.GetSpell(SpellSlot.W).SData.Mana;
                EMANA2 = myHero.Spellbook.GetSpell(SpellSlot.E).SData.Mana;
            }

            Qcd = SetPlus(Qcdt - Game.Time);
            Wcd = SetPlus(Wcdt - Game.Time);
            Ecd = SetPlus(Ecdt - Game.Time);
            Q2cd = SetPlus(Q2cdt - Game.Time);
            W2cd = SetPlus(W2cdt - Game.Time);
            E2cd = SetPlus(E2cdt - Game.Time);
        }

        private static float SetPlus(float valus)
        {
            if (valus < 0)
                return 0;
            else
                return valus;
        }

        private static void OnDraw(EventArgs args)
        {
            if (showcd)
            {
                string msg = " ";

                if (Range)
                {
                    msg = "Q2 :" + (int)Q2cd + " |  W2 : " + (int)W2cd + " |  E2 : " + (int)E2cd;
                    Drawing.DrawText(Drawing.Width * 0.5f - 50, Drawing.Height * 0.3f, System.Drawing.Color.Orange, msg);
                }
                else
                {
                    msg = "Q1 : " + (int)Qcd + " |  W1 :" + (int)Wcd + " |  E1 :" + (int)Ecd;
                    Drawing.DrawText(Drawing.Width * 0.5f - 50, Drawing.Height * 0.3f, System.Drawing.Color.Aqua, msg);
                }
            }


            if (qRange)
            {
                if (onlyRdy)
                {
                    if (Q.IsReady())
                    {
                        if (Range)
                        {
                            if (CanUseQE())
                                Drawing.DrawCircle(myHero.Position, Qext.Range, System.Drawing.Color.Cyan);
                            else
                                Drawing.DrawCircle(myHero.Position, Q.Range, System.Drawing.Color.Cyan);
                        }
                        else
                            Drawing.DrawCircle(myHero.Position, Q2.Range, System.Drawing.Color.Orange);
                    }
                }
                else
                {
                    if (Range)
                    {
                        if (CanUseQE())
                            Drawing.DrawCircle(myHero.Position, Qext.Range, System.Drawing.Color.Cyan);
                        else
                            Drawing.DrawCircle(myHero.Position, Q.Range, System.Drawing.Color.Cyan);
                    }
                    else
                        Drawing.DrawCircle(myHero.Position, Q2.Range, System.Drawing.Color.Orange);
                }
            }
        }

        public static int getELevel()
        {
            var i = 0;

            if (E.Level == 0)
            {
                i = 0;
            }
            else if (E.Level == 1)
            {
                i = 1;
            }
            else if (E.Level == 2)
            {
                i = 2;
            }
            else if (E.Level == 3)
            {
                i = 3;
            }
            else if (E.Level == 4)
            {
                i = 4;
            }
            else if (E.Level == 5)
            {
                i = 5;
            }
            else if (E.Level > 5)
            {
                i = 5;
            }

            return i;
        }

        public static float GetRealDamage(this SpellSlot slot, Obj_AI_Base target)
        {
            var lvl = getELevel();
            float dmg = 0;
            if (!Range)
            {
                dmg = new[] { 0.08f, 0.104f, 0.128f, 0.152f, 0.176f }[lvl - 1] * target.MaxHealth + 1f * Player.Instance.FlatPhysicalDamageMod;
            }

            if (dmg <= 0)
            {
                return 0;
            }

            return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, dmg) - 8;
        }

        private static void OnGapCloser(Obj_AI_Base sender, Gapcloser.GapcloserEventArgs args)
        {
            if (!getGapCloseE || E2cd > 0.1)
                return;

            if (Range && !R.IsReady())
                return;

            var t = sender;

            if (t.IsValidTarget(400))
            {
                if (Range)
                {
                    R.Cast();
                }
                else
                    E.Cast(t);
            }
        }

        private static void OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs args)
        {
            if (!intE || E2cd > 0.1)
                return;

            if (Range && !R.IsReady())
                return;

            if (sender.IsValidTarget(300))
            {
                if (Range)
                {
                    R.Cast();
                }
                else
                    E.Cast(sender);

            }
            else if (Q2cd < 0.2 && sender.IsValidTarget(Q2.Range))
            {
                if (Range)
                {
                    R.Cast();
                }
                else
                {
                    Q.Cast(sender);
                    if (sender.IsValidTarget(E2.Range))
                        E.Cast(sender);
                }
            }
        }

        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.Q)
            {
                if (W.IsReady() && !Range && myHero.Mana > 80)
                    W.Cast();
                if (E.IsReady() && Range && qeset == 1 && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    E.Cast((Vector3)myHero.ServerPosition.Extend(args.EndPosition, 120));
                }
                else if (E.IsReady() && Range && qeset == 3)
                {
                    E.Cast((Vector3)myHero.ServerPosition.Extend(args.EndPosition, 120));
                }
                else if (E.IsReady() && Range && qeset == 4 && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                {
                    E.Cast((Vector3)myHero.ServerPosition.Extend(args.EndPosition, 120));
                }
                else if (E.IsReady() && Range && qeset == 5 && (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)))
                {
                    E.Cast((Vector3)myHero.ServerPosition.Extend(args.EndPosition, 120));
                }
            }
        }

        private static void OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "jayceshockblast")
            {
                if (Range && E.IsReady() && autoE && qeset == 1 && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    EcastPos = (Vector3)myHero.ServerPosition.Extend(args.End, 130 + (Game.Ping / 2));
                    Etick = Environment.TickCount;
                    E.Cast(EcastPos);
                }
                else if (Range && E.IsReady() && autoE && qeset == 3)
                {
                    EcastPos = (Vector3)myHero.ServerPosition.Extend(args.End, 130 + (Game.Ping / 2));
                    Etick = Environment.TickCount;
                    E.Cast(EcastPos);
                }
                else if (Range && E.IsReady() && autoE && qeset == 4 && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                {
                    EcastPos = (Vector3)myHero.ServerPosition.Extend(args.End, 130 + (Game.Ping / 2));
                    Etick = Environment.TickCount;
                    E.Cast(EcastPos);
                }
                else if (Range && E.IsReady() && autoE && qeset == 5 && (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass)))
                {
                    EcastPos = (Vector3)myHero.ServerPosition.Extend(args.End, 130 + (Game.Ping / 2));
                    Etick = Environment.TickCount;
                    E.Cast(EcastPos);
                }
            }
        }

        private static void OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (W.IsReady() && autoW && Range && args.Target is AIHeroClient)
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                    W.Cast();
                else if (args.Target.Position.Distance(myHero.Position) < 500)
                    W.Cast();
            }
        }
    }
}
