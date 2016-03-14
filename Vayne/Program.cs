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
using Geometry = Vayne.Geometry;

namespace Vayne
{
    public static class Program
    {

        private static Spell.Skillshot Q, E2;
        private static Spell.Targeted E;
        private static Spell.Active W, R;

        public static void Main()
        {
            Loading.OnLoadingComplete += OnLoad;
        }

        private static AIHeroClient myHero
        {
            get { return Player.Instance; }
        }

        #region ctor
        private static void OnLoad(EventArgs args)
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 300, SkillShotType.Linear);
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Targeted(SpellSlot.E, 550);
            R = new Spell.Active(SpellSlot.R);
            E2 = new Spell.Skillshot(SpellSlot.E, 550, SkillShotType.Linear, (int)0.42f, 1300, 50);

            InitMenu();
            Game.OnUpdate += OnUpdate;
            Orbwalker.OnPostAttack += Orbwalker_OnPostAttack;
            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
        }
        #endregion

        #region Events

        public static void OnUpdate(EventArgs args)
        {
            if (myHero.CountEnemiesInRange(550 + 200) >= GetAutoR && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                R.Cast();
            }

            if (UseEBool)
            {
                foreach (var enemy in EntityManager.Heroes.Enemies.Where(e => e.IsValidTarget(550)))
                {
                    if (IsCondemnable(enemy))
                    {
                        E.Cast(enemy);
                    }
                }
            }

            if (SemiAutomaticCondemnKey)
            {
                foreach (var hero in EntityManager.Heroes.Enemies.Where(h => h.ServerPosition.Distance(myHero.ServerPosition) < 550))
                {
                    var prediction = E2.GetPrediction(hero);
                    Vector2 a = prediction.UnitPosition.To2D();
                    for (var i = 40; i < 425; i += 125)
                    {
                        var flags = NavMesh.GetCollisionFlags(a.Extend(myHero.ServerPosition, (float)-i));
                        if (flags.HasFlag(CollisionFlags.Wall) || flags.HasFlag(CollisionFlags.Building))
                        {
                            E.Cast(hero);
                            return;
                        }
                    }
                }
            }
        }

        static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (UseEInterruptBool)
            {
                var possibleChannelingTarget = EntityManager.Heroes.Enemies.FirstOrDefault(a => a.ServerPosition.Distance(myHero.ServerPosition) < 550 && sender.IsValidTarget() && sender.IsEnemy && !sender.IsZombie);
                if (possibleChannelingTarget.IsValidTarget())
                {
                    E.Cast(possibleChannelingTarget);
                }
            }
        }

        public static void OnProcessSpellCast(GameObject sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender is AIHeroClient && sender.IsEnemy)
            {
                if (args.SData.Name == "summonerflash" && args.End.Distance(myHero.ServerPosition) < 350)
                {
                    E.Cast((AIHeroClient)sender);
                }

                var sdata = Evade.EvadeSpellDatabase.GetByName(args.SData.Name);

                if (sdata != null)
                {
                    if (UseEAntiGapcloserBool && (myHero.Distance(args.Start.Extend(args.End, sdata.MaxRange)) < 350 || args.Target.IsMe) && sdata.IsBlink || sdata.IsDash)
                    {
                        if (E.IsReady())
                        {
                            E.Cast((AIHeroClient)sender);
                        }
                        if (Q.IsReady())
                        {
                            switch (UseQAntiGapcloserStringList)
                            {
                                case 3:
                                    {
                                        if (args.End.Distance(myHero.ServerPosition) < 350)
                                        {
                                            var pos = myHero.ServerPosition.Extend(args.End, -300).To3D();
                                            if (!IsDangerousPosition(pos))
                                            {
                                                myHero.Spellbook.CastSpell(SpellSlot.Q, pos);
                                            }
                                        }
                                        if (sender.Distance(myHero) < 350)
                                        {
                                            var pos = myHero.ServerPosition.Extend(sender.Position, -300).To3D();
                                            if (!IsDangerousPosition(pos))
                                            {
                                                myHero.Spellbook.CastSpell(SpellSlot.Q, pos);
                                            }
                                        }
                                        break;
                                    }
                                case 2:
                                    {
                                        if (!E.IsReady())
                                        {
                                            if (args.End.Distance(myHero.ServerPosition) < 350)
                                            {
                                                var pos = myHero.ServerPosition.Extend(args.End, -300).To3D();
                                                if (!IsDangerousPosition(pos))
                                                {
                                                    myHero.Spellbook.CastSpell(SpellSlot.Q, pos);
                                                }
                                            }
                                            if (sender.Distance(myHero) < 350)
                                            {
                                                var pos = myHero.ServerPosition.Extend(sender.Position, -300).To3D();
                                                if (!IsDangerousPosition(pos))
                                                {
                                                    myHero.Spellbook.CastSpell(SpellSlot.Q, pos);
                                                }
                                            }
                                        }
                                        break;
                                    }
                            }
                        }
                    }
                    if (UseEInterruptBool && !sdata.IsInvulnerability && myHero.Distance(sender) < 550)
                    {
                        E.Cast((AIHeroClient)sender);
                    }
                }
            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (DrawWStacksBool)
            {
                var target = EntityManager.Heroes.Enemies.FirstOrDefault(enemy => enemy.HasBuff("vaynesilvereddebuff") && enemy.IsValidTarget(2000));
                if (target.IsValidTarget())
                {
                    var x = target.HPBarPosition.X + 50;
                    var y = target.HPBarPosition.Y - 20;

                    if (W.Level > 0)
                    {
                        int stacks = target.GetBuffCount("vaynesilvereddebuff");
                        if (stacks > -1)
                        {
                            for (var i = 0; i < 3; i++)
                            {
                                Drawing.DrawLine(x + i * 20, y, x + i * 20 + 10, y, 10, stacks <= i ? Color.DarkGray : Color.DeepSkyBlue);
                            }
                        }
                    }
                }
            }
        }

        static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            var possible2WTarget = EntityManager.Heroes.Enemies.FirstOrDefault(h => h.ServerPosition.Distance(myHero.ServerPosition) < 500 && h.GetBuffCount("vaynesilvereddebuff") == 2);
            if (TryToFocus2WBool && possible2WTarget.IsValidTarget())
            {
                Orbwalker.ForcedTarget = possible2WTarget;
            }
            if (myHero.HasBuff("vaynetumblefade") && DontAttackWhileInvisibleAndMeelesNearBool)
            {
                if (EntityManager.Heroes.Enemies.Any(e => e.ServerPosition.Distance(myHero.ServerPosition) < 350 && e.IsMelee))
                {
                    args.Process = false;
                }
            }
            if (myHero.HasBuff("vaynetumblebonus") && args.Target is Obj_AI_Minion && UseQBonusOnEnemiesNotCS)
            {
                var possibleTarget = TargetSelector.GetTarget(-1f, DamageType.Physical);
                if (possibleTarget != null && possibleTarget.IsInAutoAttackRange(myHero))
                {
                    Orbwalker.ForcedTarget = possibleTarget;
                    args.Process = false;
                }
            }
            var possibleNearbyMeleeChampion = EntityManager.Heroes.Enemies.FirstOrDefault(e => e.ServerPosition.Distance(myHero.ServerPosition) < 350);

            if (possibleNearbyMeleeChampion.IsValidTarget())
            {
                if (Q.IsReady() && UseQBool)
                {
                    var pos = myHero.ServerPosition.Extend(possibleNearbyMeleeChampion.ServerPosition, -350).To3D();
                    if (!IsDangerousPosition(pos))
                    {
                        myHero.Spellbook.CastSpell(SpellSlot.Q, pos);
                    }
                    args.Process = false;
                }
            }
        }

        private static readonly string[] MobNames = { "SRU_Red", "SRU_Blue", "SRU_Gromp", "SRU_Murkwolf", "SRU_Razorbeak", "SRU_Krug", "Sru_Crab", "TT_Spiderboss", "TTNGolem", "TTNWolf", "TTNWraith" };

        static void Orbwalker_OnPostAttack(AttackableUnit target, System.EventArgs args)
        {
            Orbwalker.ForcedTarget = null;
            var possible2WTarget = EntityManager.Heroes.Enemies.FirstOrDefault(h => h.ServerPosition.Distance(myHero.ServerPosition) < 500 && h.GetBuffCount("vaynesilvereddebuff") == 2);
            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                if (possible2WTarget.IsValidTarget() && UseEAs3rdWProcBool && possible2WTarget.Path.LastOrDefault().Distance(myHero.ServerPosition) < 1000)
                {
                    E.Cast(possible2WTarget);
                }
            }
            if (target is AIHeroClient && UseQBool)
            {
                if (Q.IsReady())
                {
                    var tg = target as AIHeroClient;
                    if (tg != null)
                    {
                        var mode = QModeStringList;
                        var tumblePosition = Game.CursorPos;
                        switch (mode)
                        {
                            case 1:
                                tumblePosition = GetTumblePos(tg);
                                break;
                            default:
                                tumblePosition = Game.CursorPos;
                                break;
                        }
                        if (tumblePosition.Distance(myHero.Position) > 2000 || IsDangerousPosition(tumblePosition)) return;
                        myHero.Spellbook.CastSpell(SpellSlot.Q, tumblePosition);
                    }
                }
            }
            if (target is Obj_AI_Minion && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                var tg = target as Obj_AI_Minion;
                if (E.IsReady())
                {
                    if (MobNames.Contains((tg as Obj_AI_Base).CharData.BaseSkinName) && tg.IsValidTarget() && UseEJungleFarm)
                    {
                        E.Cast(tg);
                    }
                }
                if (UseQFarm && Q.IsReady())
                {
                    if (tg.Name.Contains("SRU_") && !IsDangerousPosition(Game.CursorPos))
                    {
                        myHero.Spellbook.CastSpell(SpellSlot.Q, Game.CursorPos);
                    }
                    if (EntityManager.MinionsAndMonsters.EnemyMinions.Count(m => m.Position.Distance(myHero.Position) < 550 && m.Health < myHero.GetAutoAttackDamage(m) + myHero.GetSpellDamage(m, SpellSlot.Q)) > 1 && !IsDangerousPosition(Game.CursorPos))
                    {
                        myHero.Spellbook.CastSpell(SpellSlot.Q, Game.CursorPos);
                    }
                    if (UnderAllyTurret(myHero.Position))
                    {
                        if (EntityManager.MinionsAndMonsters.EnemyMinions.Count(m => m.Position.Distance(myHero.Position) < 550 && m.Health < myHero.GetAutoAttackDamage(m) + myHero.GetSpellDamage(m, SpellSlot.Q)) > 0 && !IsDangerousPosition(Game.CursorPos))
                        {
                            myHero.Spellbook.CastSpell(SpellSlot.Q, Game.CursorPos);
                        }
                    }
                }
            }
            if (UseQOnlyAt2WStacksBool && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && possible2WTarget.IsValidTarget())
            {
                myHero.Spellbook.CastSpell(SpellSlot.Q, GetTumblePos(possible2WTarget));
            }
        }

        public static bool UnderAllyTurret(Vector3 pos)
        {
            return ObjectManager.Get<Obj_AI_Turret>().Any(t => t.IsAlly && !t.IsDead && pos.Distance(t) <= 900);
        }

        #endregion

        #region Menu Items
        public static bool UseQBool { get { return Menu["useq"].Cast<CheckBox>().CurrentValue; } }
        public static int QModeStringList { get { return Menu["qmode"].Cast<Slider>().CurrentValue; } }
        public static int UseQAntiGapcloserStringList { get { return Menu["qantigc"].Cast<Slider>().CurrentValue; } }
        public static bool TryToFocus2WBool { get { return Menu["focus2w"].Cast<CheckBox>().CurrentValue; } }
        public static bool DontAttackWhileInvisibleAndMeelesNearBool { get { return Menu["dontattackwhileinvisible"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseRBool { get { return Menu["user"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseEBool { get { return Menu["usee"].Cast<CheckBox>().CurrentValue; } }
        public static int EModeStringList { get { return Menu["emode"].Cast<Slider>().CurrentValue; } }
        public static bool UseEInterruptBool { get { return Menu["useeinterrupt"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseEAntiGapcloserBool { get { return Menu["useeantigapcloser"].Cast<CheckBox>().CurrentValue; } }
        public static int EPushDistanceSlider { get { return Menu["epushdist"].Cast<Slider>().CurrentValue; } }
        public static int EHitchanceSlider { get { return Menu["ehitchance"].Cast<Slider>().CurrentValue; } }
        public static bool SemiAutomaticCondemnKey { get { return Menu["semiautoekey"].Cast<KeyBind>().CurrentValue; } }
        public static bool UseEAs3rdWProcBool { get { return Menu["usee3rdwproc"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseQBonusOnEnemiesNotCS { get { return Menu["useqonenemiesnotcs"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseQOnlyAt2WStacksBool { get { return Menu["useqonlyon2stackedenemies"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseQFarm { get { return Menu["useqfarm"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseEJungleFarm { get { return Menu["useejgfarm"].Cast<CheckBox>().CurrentValue; } }
        public static bool DrawWStacksBool { get { return Menu["drawwstacks"].Cast<CheckBox>().CurrentValue; } }
        public static int GetAutoR { get { return Menu["GetAutoR"].Cast<Slider>().CurrentValue; } }
        #endregion

        #region Menu

        private static EloBuddy.SDK.Menu.Menu Menu;

        private static void InitMenu()
        {
            Menu = MainMenu.AddMenu("Vayne", "Vayne");
            Menu.AddLabel("Ported from Challenger Series - Berb");
            Menu.AddSeparator();

            Menu.AddGroupLabel("Combo");
            Menu.Add("useq", new CheckBox("Auto Q")); // UseQBool
            Menu.AddSeparator();
            Menu.AddLabel("1 : Prada | 2 : Marksman | 3 : VHR | 4 : Sharpshooter");
            Menu.Add("qmode", new Slider("Q Mode:", 1, 1, 4)); // QModeStringList
            Menu.AddSeparator();
            Menu.AddLabel("1 : Never | 2 : E-Not-Ready | 3 : Always");
            Menu.Add("qantigc", new Slider("Use Q Antigapcloser:", 1, 1, 3)); // UseQAntiGapcloserStringList
            Menu.AddSeparator();
            Menu.Add("focus2w", new CheckBox("Try To Focus 2W", false)); // TryToFocus2WBool
            Menu.Add("dontattackwhileinvisible", new CheckBox("Smart Invisible Attacking")); // DontAttackWhileInvisibleAndMeelesNearBool
            Menu.Add("user", new CheckBox("Use R In Combo", false)); // UseRBool
            Menu.Add("GetAutoR", new Slider("R if >= X enemies : ", 2, 1, 5)); // GetAutoR
            Menu.AddSeparator();

            Menu.AddGroupLabel("Condemn Menu");
            Menu.Add("usee", new CheckBox("Auto E")); // UseEBool
            Menu.AddSeparator();
            Menu.AddLabel("1 : Prada Smart | 2 : Prada Perfect | 3 : Marksman");
            Menu.AddLabel("4 : Sharpshooter | 5 : Gosu | 6 : VHR");
            Menu.AddLabel("7 : Prada Legacy | 8 : Fastest | 9 : Old Prada");
            Menu.Add("emode", new Slider("E Mode: ", 1, 1, 9)); // EModeStringList
            Menu.AddSeparator();
            Menu.Add("useeinterrupt", new CheckBox("Use E To Interrupt")); // UseEInterruptBool
            Menu.Add("useeantigapcloser", new CheckBox("Use E AntiGapcloser")); // UseEAntiGapcloserBool
            Menu.AddSeparator();
            Menu.Add("epushdist", new Slider("E Push Distance: ", 425, 300, 475)); // EPushDistanceSlider
            Menu.AddSeparator();
            Menu.Add("ehitchance", new Slider("Condemn Hitchance", 50, 0, 100)); // EHitchanceSlider
            Menu.AddSeparator();
            Menu.Add("semiautoekey", new KeyBind("Semi Automatic Condemn", false, KeyBind.BindTypes.PressToggle, 'E')); // SemiAutomaticCondemnKey
            Menu.AddSeparator();

            Menu.Add("usee3rdwproc", new CheckBox("Use E as 3rd W Proc Before LVL: ", false)); // UseEAs3rdWProcBool
            Menu.Add("useqonenemiesnotcs", new CheckBox("Use Q Bonus On ENEMY not CS", false)); // UseQBonusOnEnemiesNotCS
            Menu.Add("useqonlyon2stackedenemies", new CheckBox("Use Q If Enemy Have 2W Stacks", false)); // UseQOnlyAt2WStacksBool
            Menu.AddSeparator();

            Menu.AddGroupLabel("Farm Menu");
            Menu.Add("useqfarm", new CheckBox("Use Q")); // UseQFarm
            Menu.Add("useejgfarm", new CheckBox("Use E Jungle")); // UseEJungleFarm
            Menu.AddSeparator();

            Menu.AddGroupLabel("Drawing Menu");
            Menu.Add("drawwstacks", new CheckBox("Draw W Stacks")); // DrawWStacksBool
            Menu.AddSeparator();
        }

        #endregion Menu

        #region ChampionLogic

        public static bool IsCondemnable(AIHeroClient hero)
        {
            if (!hero.IsValidTarget(550f) || hero.HasBuffOfType(BuffType.SpellShield) ||
                hero.HasBuffOfType(BuffType.SpellImmunity) || hero.IsDashing()) return false;

            //values for pred calc pP = player position; p = enemy position; pD = push distance
            var pP = myHero.ServerPosition;
            var p = hero.ServerPosition;
            var pD = EPushDistanceSlider;
            var mode = EModeStringList;

            if (mode == 1 && (IsCollisionable((Vector3)p.Extend(pP, -pD)) || IsCollisionable((Vector3)p.Extend(pP, -pD / 2f)) || IsCollisionable((Vector3)p.Extend(pP, -pD / 3f))))
            {
                if (!hero.CanMove) { return true; }

                var enemiesCount = myHero.CountEnemiesInRange(1200);
                if (enemiesCount > 1 && enemiesCount <= 3)
                {
                    var prediction = E2.GetPrediction(hero);
                    for (var i = 15; i < pD; i += 75)
                    {
                        if (i > pD)
                        {
                            var lastPosFlags = NavMesh.GetCollisionFlags(prediction.UnitPosition.To2D().Extend(pP.To2D(), -pD).To3D());
                            if (lastPosFlags.HasFlag(CollisionFlags.Wall) || lastPosFlags.HasFlag(CollisionFlags.Building))
                            {
                                return true;
                            }
                            return false;
                        }
                        var posFlags = NavMesh.GetCollisionFlags(prediction.UnitPosition.To2D().Extend(pP.To2D(), -i));
                        if (posFlags.HasFlag(CollisionFlags.Wall) || posFlags.HasFlag(CollisionFlags.Building))
                        {
                            return true;
                        }
                    }
                    return false;
                }
                else
                {
                    var hitchance = EHitchanceSlider;
                    var angle = 0.20 * hitchance;
                    const float travelDistance = 0.5f;
                    var alpha = new Vector2((float)(p.X + travelDistance * Math.Cos(Math.PI / 180 * angle)),
                        (float)(p.X + travelDistance * Math.Sin(Math.PI / 180 * angle)));
                    var beta = new Vector2((float)(p.X - travelDistance * Math.Cos(Math.PI / 180 * angle)),
                        (float)(p.X - travelDistance * Math.Sin(Math.PI / 180 * angle)));

                    for (var i = 15; i < pD; i += 100)
                    {
                        if (i > pD) return false;
                        if (IsCollisionable(pP.To2D().Extend(alpha, i).To3D()) && IsCollisionable(pP.To2D().Extend(beta, i).To3D())) return true;
                    }
                    return false;
                }
            }

            if (mode == 2 && (IsCollisionable(p.Extend(pP, -pD).To3D()) || IsCollisionable(p.Extend(pP, -pD / 2f).To3D()) || IsCollisionable(p.Extend(pP, -pD / 3f).To3D())))
            {
                if (!hero.CanMove)
                {
                    return true;
                }

                var hitchance = EHitchanceSlider;
                var angle = 0.20 * hitchance;
                const float travelDistance = 0.5f;
                var alpha = new Vector2((float)(p.X + travelDistance * Math.Cos(Math.PI / 180 * angle)),
                    (float)(p.X + travelDistance * Math.Sin(Math.PI / 180 * angle)));
                var beta = new Vector2((float)(p.X - travelDistance * Math.Cos(Math.PI / 180 * angle)),
                    (float)(p.X - travelDistance * Math.Sin(Math.PI / 180 * angle)));

                for (var i = 15; i < pD; i += 100)
                {
                    if (i > pD)
                    {
                        return IsCollisionable(alpha.Extend(pP.To2D(), -pD).To3D()) && IsCollisionable(beta.Extend(pP.To2D(), -pD).To3D());
                    }
                    if (IsCollisionable(alpha.Extend(pP.To2D(), -i).To3D()) && IsCollisionable(beta.Extend(pP.To2D(), -i).To3D())) return true;
                }
                return false;
            }

            if (mode == 9)
            {
                if (!hero.CanMove)
                    return true;

                var hitchance = EHitchanceSlider;
                var angle = 0.20 * hitchance;
                const float travelDistance = 0.5f;
                var alpha = new Vector2((float)(p.X + travelDistance * Math.Cos(Math.PI / 180 * angle)), (float)(p.X + travelDistance * Math.Sin(Math.PI / 180 * angle)));
                var beta = new Vector2((float)(p.X - travelDistance * Math.Cos(Math.PI / 180 * angle)), (float)(p.X - travelDistance * Math.Sin(Math.PI / 180 * angle)));

                for (var i = 15; i < pD; i += 100)
                {
                    if (IsCollisionable(pP.To2D().Extend(alpha, i).To3D()) || IsCollisionable(pP.To2D().Extend(beta, i).To3D())) return true;
                }
                return false;
            }

            if (mode == 3)
            {
                var prediction = E2.GetPrediction(hero);
                return NavMesh.GetCollisionFlags(prediction.UnitPosition.To2D().Extend(pP.To2D(), -pD).To3D()).HasFlag(CollisionFlags.Wall) || NavMesh.GetCollisionFlags(prediction.UnitPosition.To2D().Extend(pP.To2D(), -pD / 2f).To3D()).HasFlag(CollisionFlags.Wall);
            }

            if (mode == 4)
            {
                var prediction = E2.GetPrediction(hero);
                for (var i = 15; i < pD; i += 100)
                {
                    if (i > pD) return false;
                    var posCF = NavMesh.GetCollisionFlags(prediction.UnitPosition.To2D().Extend(pP.To2D(), -i).To3D());
                    if (posCF.HasFlag(CollisionFlags.Wall) || posCF.HasFlag(CollisionFlags.Building))
                    {
                        return true;
                    }
                }
                return false;
            }

            if (mode == 5)
            {
                var prediction = E2.GetPrediction(hero);
                for (var i = 15; i < pD; i += 75)
                {
                    var posCF = NavMesh.GetCollisionFlags(prediction.UnitPosition.To2D().Extend(pP.To2D(), -i).To3D());
                    if (posCF.HasFlag(CollisionFlags.Wall) || posCF.HasFlag(CollisionFlags.Building))
                    {
                        return true;
                    }
                }
                return false;
            }

            if (mode == 6)
            {
                var prediction = E2.GetPrediction(hero);
                for (var i = 15; i < pD; i += (int)hero.BoundingRadius) //:frosty:
                {
                    var posCF = NavMesh.GetCollisionFlags(prediction.UnitPosition.To2D().Extend(pP.To2D(), -i).To3D());
                    if (posCF.HasFlag(CollisionFlags.Wall) || posCF.HasFlag(CollisionFlags.Building))
                    {
                        return true;
                    }
                }
                return false;
            }

            if (mode == 7)
            {
                var prediction = E2.GetPrediction(hero);
                for (var i = 15; i < pD; i += 75)
                {
                    var posCF = NavMesh.GetCollisionFlags(prediction.UnitPosition.To2D().Extend(pP.To2D(), -i).To3D());
                    if (posCF.HasFlag(CollisionFlags.Wall) || posCF.HasFlag(CollisionFlags.Building))
                    {
                        return true;
                    }
                }
                return false;
            }

            if (mode == 8 && (IsCollisionable((Vector3)p.Extend(pP, -pD)) || IsCollisionable((Vector3)p.Extend(pP, -pD / 2f)) || IsCollisionable((Vector3)p.Extend(pP, -pD / 3f))))
            {
                return true;
            }

            return false;
        }

        public static Vector3 GetAggressiveTumblePos(Obj_AI_Base target)
        {
            var cursorPos = Game.CursorPos;

            if (!IsDangerousPosition(cursorPos)) return cursorPos;
            //if the target is not a melee and he's alone he's not really a danger to us, proceed to 1v1 him :^ )
            if (!target.IsMelee && myHero.CountEnemiesInRange(800) == 1) return cursorPos;

            var aRC = new Geometry.Circle(myHero.ServerPosition.To2D(), 300).ToPolygon().ToClipperPath();

            var targetPosition = target.ServerPosition;

            foreach (var p in aRC)
            {
                var v3 = new Vector2(p.X, p.Y).To3D();
                var dist = v3.Distance(targetPosition);
                if (dist > 325 && dist < 450)
                {
                    return v3;
                }
            }
            return Vector3.Zero;
        }

        internal static class WaypointTracker
        {
            #region Static Fields
            public static readonly Dictionary<int, List<Vector2>> StoredPaths = new Dictionary<int, List<Vector2>>();
            public static readonly Dictionary<int, int> StoredTick = new Dictionary<int, int>();

            #endregion
        }

        public static List<Vector2> GetWaypoints(this Obj_AI_Base unit)
        {
            var result = new List<Vector2>();

            if (unit.IsVisible)
            {
                result.Add(unit.ServerPosition.To2D());
                result.AddRange(unit.Path.Select(point => point.To2D()));
            }
            else
            {
                List<Vector2> value;
                if (WaypointTracker.StoredPaths.TryGetValue(unit.NetworkId, out value))
                {
                    var path = value;
                    var timePassed = (Environment.TickCount - WaypointTracker.StoredTick[unit.NetworkId]) / 1000f;
                    if (path.GetPathLength() >= unit.MoveSpeed * timePassed)
                    {
                        result = CutPath(path, (int)(unit.MoveSpeed * timePassed));
                    }
                }
            }

            return result;
        }

        public static float GetPathLength(this List<Vector2> path)
        {
            var distance = 0f;

            for (var i = 0; i < path.Count - 1; i++)
            {
                distance += path[i].Distance(path[i + 1]);
            }

            return distance;
        }

        public static List<Vector2> CutPath(this List<Vector2> path, float distance)
        {
            var result = new List<Vector2>();
            for (var i = 0; i < path.Count - 1; i++)
            {
                var dist = path[i].Distance(path[i + 1]);
                if (dist > distance)
                {
                    result.Add(path[i] + (distance * (path[i + 1] - path[i]).Normalized()));

                    for (var j = i + 1; j < path.Count; j++)
                    {
                        result.Add(path[j]);
                    }

                    break;
                }

                distance -= dist;
            }

            return result.Count > 0 ? result : new List<Vector2> { path.Last() };
        }

        public static Vector3 GetTumblePos(Obj_AI_Base target)
        {
            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                return GetAggressiveTumblePos(target);

            var cursorPos = Game.CursorPos;

            if (!target.IsMelee && myHero.CountEnemiesInRange(800) == 1) return cursorPos;

            var targetWaypoints = GetWaypoints(target);

            if (targetWaypoints[targetWaypoints.Count - 1].Distance(myHero.ServerPosition) > 550)
                return Vector3.Zero;

            var aRC = new Geometry.Circle(myHero.ServerPosition.To2D(), 300).ToPolygon().ToClipperPath();
            var targetPosition = target.ServerPosition;
            var pList = (from p in aRC select new Vector2(p.X, p.Y).To3D() into v3 let dist = v3.Distance(targetPosition) where !IsDangerousPosition(v3) && dist < 500 select v3).ToList();

            if (myHero.UnderTurret() || myHero.CountEnemiesInRange(800) == 1 || cursorPos.CountEnemiesInRange(450) <= 1)
            {
                return pList.Count > 1 ? pList.OrderBy(el => el.Distance(cursorPos)).FirstOrDefault() : Vector3.Zero;
            }
            return pList.Count > 1 ? pList.OrderByDescending(el => el.Distance(cursorPos)).FirstOrDefault() : Vector3.Zero;
        }

        public static int VayneWStacks(Obj_AI_Base o)
        {
            if (o == null) return 0;
            if (o.Buffs.FirstOrDefault(b => b.Name.Contains("vaynesilver")) == null || !o.Buffs.Any(b => b.Name.Contains("vaynesilver"))) return 0;
            return o.Buffs.FirstOrDefault(b => b.Name.Contains("vaynesilver")).Count;
        }

        public static Vector3 Randomize(Vector3 pos)
        {
            var r = new Random(Environment.TickCount);
            return new Vector2(pos.X + r.Next(-150, 150), pos.Y + r.Next(-150, 150)).To3D();
        }

        public static bool UnderTurret(this Obj_AI_Base unit)
        {
            return UnderTurret(unit.Position, true);
        }

        public static bool UnderTurret(this Obj_AI_Base unit, bool enemyTurretsOnly)
        {
            return UnderTurret(unit.Position, enemyTurretsOnly);
        }

        public static bool UnderTurret(this Vector3 position, bool enemyTurretsOnly)
        {
            return ObjectManager.Get<Obj_AI_Turret>().Any(turret => turret.IsValidTarget(950));
        }

        public static bool IsDangerousPosition(Vector3 pos)
        {
            return EntityManager.Heroes.Enemies.Any(e => e.IsValidTarget() && (e.Distance(pos) < 375) && (e.GetWaypoints().LastOrDefault().Distance(pos) > 550)) || (pos.UnderTurret(true) && !myHero.UnderTurret(true));
        }

        public static bool IsKillable(AIHeroClient hero)
        {
            return myHero.GetAutoAttackDamage(hero) * 2 < hero.Health;
        }

        public static bool IsCollisionable(Vector3 pos)
        {
            return NavMesh.GetCollisionFlags(pos).HasFlag(CollisionFlags.Wall) || (NavMesh.GetCollisionFlags(pos).HasFlag(CollisionFlags.Building));
        }

        public static bool IsValidState(AIHeroClient target)
        {
            return !target.HasBuffOfType(BuffType.SpellShield) && !target.HasBuffOfType(BuffType.SpellImmunity) && !target.HasBuffOfType(BuffType.Invulnerability);
        }

        public static int CountHerosInRange(AIHeroClient target, bool checkteam, float range = 1200f)
        {
            var objListTeam = ObjectManager.Get<AIHeroClient>().Where(x => x.IsValidTarget(range, false));
            return objListTeam.Count(hero => checkteam ? hero.Team != target.Team : hero.Team == target.Team);
        }

        #endregion
    }
}