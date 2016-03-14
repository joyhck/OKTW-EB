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
using Geometry = Vayne.Geometry;

namespace Vayne
{
    public static class Program
    {
        #region Instance Variables
        private static Spell.Skillshot Q, E2;
        private static Spell.Targeted E;
        private static Spell.Active W, R, cleanse;
        #endregion

        #region Init
        public static void Main()
        {
            Loading.OnLoadingComplete += OnLoad;
        }

        private static AIHeroClient myHero
        {
            get { return Player.Instance; }
        }
        #endregion

        #region ctor
        private static void OnLoad(EventArgs args)
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 300, SkillShotType.Linear);
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Targeted(SpellSlot.E, 550);
            R = new Spell.Active(SpellSlot.R);
            E2 = new Spell.Skillshot(SpellSlot.E, 550, SkillShotType.Linear, (int)0.42f, 1300, 50);

            var clean = Player.Spells.FirstOrDefault(o => o.SData.Name == "SummonerBoost");
            if (clean != null)
            {
                SpellSlot cleanses = EloBuddy.SDK.Extensions.GetSpellSlotFromName(myHero, "SummonerBoost");
                cleanse = new Spell.Active(cleanses);
            }

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

        private static void Clean()
        {
            if (Item.CanUseItem(ItemId.Quicksilver_Sash))
            {
                Core.DelayAction(delegate { Item.UseItem(ItemId.Quicksilver_Sash); }, CSSDelay);
            }
            else if (Item.CanUseItem(ItemId.Mercurial_Scimitar))
            {
                Core.DelayAction(delegate { Item.UseItem(ItemId.Mercurial_Scimitar); }, CSSDelay);
            }
            else if (Item.CanUseItem(ItemId.Dervish_Blade))
            {
                Core.DelayAction(delegate { Item.UseItem(ItemId.Dervish_Blade); }, CSSDelay);
            }
            else if (cleanse != null && cleanse.IsReady())
            {
                Core.DelayAction(delegate { cleanse.Cast(); }, CSSDelay);
            }
        }

        private static void Cleansers()
        {
            if (!_Clean)
            {
                return;
            }

            var target = TargetSelector.GetTarget(550, DamageType.Physical);

            if (target == null) { return; }

            if (!Item.CanUseItem(ItemId.Quicksilver_Sash) && !Item.CanUseItem(ItemId.Mikaels_Crucible) && !Item.CanUseItem(ItemId.Mercurial_Scimitar) && !Item.CanUseItem(ItemId.Dervish_Blade))
            {
                return;
            }

            if (!Item.HasItem(ItemId.Quicksilver_Sash) && !Item.HasItem(ItemId.Mikaels_Crucible) && !Item.HasItem(ItemId.Mercurial_Scimitar) && !Item.HasItem(ItemId.Dervish_Blade))
            {
                return;
            }

            if (myHero.HealthPercent >= cleanHP)
            {
                return;
            }

            if (myHero.HasBuff("zedrdeathmark") || myHero.HasBuff("FizzMarinerDoom") || myHero.HasBuff("MordekaiserChildrenOfTheGrave") || myHero.HasBuff("PoppyDiplomaticImmunity") || myHero.HasBuff("VladimirHemoplague"))
            {
                Clean();
            }

            if (Item.CanUseItem(ItemId.Mikaels_Crucible) && Item.HasItem(ItemId.Mikaels_Crucible))
            {
                foreach (var ally in EntityManager.Heroes.Allies.Where(ally => ally.IsValid && !ally.IsDead && ItemMenu["MikaelsAlly" + ally.ChampionName].Cast<CheckBox>().CurrentValue && myHero.Distance(ally.Position) < 750 && ally.HealthPercent < (float)cleanHP))
                {
                    if (ally.HasBuff("zedrdeathmark") || ally.HasBuff("FizzMarinerDoom") || ally.HasBuff("MordekaiserChildrenOfTheGrave") || ally.HasBuff("PoppyDiplomaticImmunity") || ally.HasBuff("VladimirHemoplague"))
                        Item.UseItem(ItemId.Mikaels_Crucible, ally);
                    if (ally.HasBuffOfType(BuffType.Stun) && Stun)
                        Item.UseItem(ItemId.Mikaels_Crucible, ally);
                    if (ally.HasBuffOfType(BuffType.Snare) && Snare)
                        Item.UseItem(ItemId.Mikaels_Crucible, ally);
                    if (ally.HasBuffOfType(BuffType.Charm) && Charm)
                        Item.UseItem(ItemId.Mikaels_Crucible, ally);
                    if (ally.HasBuffOfType(BuffType.Fear) && Fear)
                        Item.UseItem(ItemId.Mikaels_Crucible, ally);
                    if (ally.HasBuffOfType(BuffType.Stun) && Stun)
                        Item.UseItem(ItemId.Mikaels_Crucible, ally);
                    if (ally.HasBuffOfType(BuffType.Taunt) && Taunt)
                        Item.UseItem(ItemId.Mikaels_Crucible, ally);
                    if (ally.HasBuffOfType(BuffType.Suppression) && Suppression)
                        Item.UseItem(ItemId.Mikaels_Crucible, ally);
                    if (ally.HasBuffOfType(BuffType.Blind) && Blind)
                        Item.UseItem(ItemId.Mikaels_Crucible, ally);
                }
            }

            if (myHero.HasBuffOfType(BuffType.Stun) && Stun)
                Clean();
            if (myHero.HasBuffOfType(BuffType.Snare) && Snare)
                Clean();
            if (myHero.HasBuffOfType(BuffType.Charm) && Charm)
                Clean();
            if (myHero.HasBuffOfType(BuffType.Fear) && Fear)
                Clean();
            if (myHero.HasBuffOfType(BuffType.Stun) && Stun)
                Clean();
            if (myHero.HasBuffOfType(BuffType.Taunt) && Taunt)
                Clean();
            if (myHero.HasBuffOfType(BuffType.Suppression) && Suppression)
                Clean();
            if (myHero.HasBuffOfType(BuffType.Blind) && Blind)
                Clean();
        }

        public static void OnUpdate(EventArgs args)
        {
            if (_Clean)
            {
                Cleansers();
            }

            if (Item.CanUseItem(ItemId.Blade_of_the_Ruined_King) && useBotrk)
            {
                var t = TargetSelector.GetTarget(550, DamageType.Physical);
                if (t.IsValidTarget() && t != null)
                {
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                    {
                        Item.UseItem(ItemId.Blade_of_the_Ruined_King, t);
                    }
                }
            }

            if (Item.CanUseItem(ItemId.Bilgewater_Cutlass) && useCutlass)
            {
                var t = TargetSelector.GetTarget(550, DamageType.Magical);
                if (t.IsValidTarget() && t != null)
                {
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                    {
                        Item.UseItem(ItemId.Bilgewater_Cutlass, t);
                    }
                }
            }

            if (Item.CanUseItem(ItemId.Youmuus_Ghostblade) && useGhostBlade)
            {
                var t = TargetSelector.GetTarget(750, DamageType.Magical);

                if (t.IsValidTarget() && t is AIHeroClient && t != null)
                {
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                    {
                        Item.UseItem(ItemId.Youmuus_Ghostblade);
                    }
                }
            }

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
            if (QModeStringList == 4)
            {
                if (sender != null)
                {
                    if (args.Target != null)
                    {
                        if (args.Target.IsMe)
                        {
                            if (sender.Type == GameObjectType.AIHeroClient)
                            {
                                if (sender.IsEnemy)
                                {
                                    if (sender.Distance(myHero) < 190)
                                    {
                                        if (antiMelee)
                                        {
                                            if (Q.IsReady())
                                            {
                                                myHero.Spellbook.CastSpell(SpellSlot.Q, (Vector3)ObjectManager.Player.Position.Extend(sender.Position, -Q.Range));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

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

        public static bool IsPositionSafe(Vector2 position)
        {
            var myPos = ObjectManager.Player.Position.To2D();
            var newPos = (position - myPos);
            newPos.Normalize();

            var checkPos = position + newPos * (Q.Range - Vector2.Distance(position, myPos));
            var enemy = EntityManager.Heroes.Enemies.Find(e => e.Distance(checkPos) < 350);
            return enemy == null;
        }

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
                            case 1: // Prada
                                tumblePosition = GetTumblePos(tg);
                                break;
                            case 2: // Marksman
                                if (tg.Distance(ObjectManager.Player.Position) > myHero.GetAutoAttackRange() && IsPositionSafe(tg.Position.To2D()))
                                {
                                    tumblePosition = tg.Position;
                                }
                                else if (IsPositionSafe(Game.CursorPos.To2D()))
                                {
                                    tumblePosition = Game.CursorPos;
                                }
                                Orbwalker.ForcedTarget = tg;
                                break;
                            case 3: // VHR
                                var smartQPosition = GetSmartQPosition();
                                var smartQCheck = smartQPosition != Vector3.Zero;
                                var QPosition = smartQCheck ? smartQPosition : Game.CursorPos;
                                var QPosition2 = GetQPosition() != Vector3.Zero ? GetQPosition() : QPosition;

                                if (!QPosition2.UnderTurret(true) || (QPosition2.UnderTurret(true) && myHero.UnderTurret(true)))
                                {
                                    tumblePosition = QPosition2;
                                }
                                break;
                            case 4: // sharpshooter
                                if (target.Type == GameObjectType.AIHeroClient)
                                {
                                    if (UseQBool)
                                    {
                                        if (Q.IsReady())
                                        {
                                            if (ObjectManager.Player.Position.Extend(Game.CursorPos, 700).CountEnemiesInRange(700) <= 1)
                                            {
                                                tumblePosition = Game.CursorPos;
                                            }
                                        }
                                    }
                                }
                                break;
                            case 5: // Synx Auto Carry
                                if (target is AIHeroClient)
                                {
                                    if (Q.IsReady())
                                    {
                                        Vector3 pos = FindTumblePosition(target as AIHeroClient);

                                        if (pos.IsValid())
                                        {
                                            tumblePosition = pos;
                                        }
                                    }
                                }
                                break;
                            default:
                                tumblePosition = Game.CursorPos;
                                break;
                        }
                        if ((tumblePosition.Distance(myHero.Position) > 2000 || IsDangerousPosition(tumblePosition)))
                        {
                            if (mode != 3)
                            {
                                return;
                            }
                        }
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

        public static List<Vector3> GetRotatedQPositions()
        {
            const int currentStep = 30;
            // var direction = ObjectManager.Player.Direction.To2D().Perpendicular();
            var direction = (Game.CursorPos - ObjectManager.Player.ServerPosition).Normalized().To2D();

            var list = new List<Vector3>();
            for (var i = -70; i <= 70; i += currentStep)
            {
                var angleRad = DegreeToRadian(i);
                var rotatedPosition = ObjectManager.Player.Position.To2D() + (300f * direction.Rotated(angleRad));
                list.Add(rotatedPosition.To3D());
            }
            return list;
        }

        public static bool IsSafeEx(this Vector3 Position)
        {
            if (Position.UnderTurret(true) && !myHero.UnderTurret())
            {
                return false;
            }
            var range = 1000f;
            var lowHealthAllies = EntityManager.Heroes.Allies.Where(a => a.IsValidTarget(range, false) && a.HealthPercent < 10 && !a.IsMe);
            var lowHealthEnemies = EntityManager.Heroes.Allies.Where(a => a.IsValidTarget(range) && a.HealthPercent < 10);
            var enemies = myHero.CountEnemiesInRange(range);
            var allies = myHero.CountAlliesInRange(range);
            var enemyTurrets = EntityManager.Turrets.Enemies.Where(m => m.IsValidTarget(975f));
            var allyTurrets = EntityManager.Turrets.Allies.Where(m => m.IsValidTarget(975f, false));

            return (allies - lowHealthAllies.Count() + allyTurrets.Count() * 2 + 1 >= enemies - lowHealthEnemies.Count() + (!myHero.UnderTurret(true) ? enemyTurrets.Count() * 2 : 0));
        }

        public static float DegreeToRadian(double angle)
        {
            return (float)(Math.PI * angle / 180.0);
        }

        public static AIHeroClient GetClosestEnemy(Vector3 from)
        {
            if (TargetSelector.SelectedTarget is AIHeroClient)
            {
                var owAI = TargetSelector.SelectedTarget as AIHeroClient;
                if (owAI.IsValidTarget(myHero.GetAutoAttackRange() + 120f, true, from))
                {
                    return owAI;
                }
            }

            return null;
        }

        public static Vector3 GetQPosition()
        {
            #region The Required Variables
            var positions = GetRotatedQPositions();
            var enemyPositions = GetEnemyPoints();
            var safePositions = positions.Where(pos => !enemyPositions.Contains(pos.To2D())).ToList();
            var BestPosition = ObjectManager.Player.ServerPosition.Extend(Game.CursorPos, 300f);
            var AverageDistanceWeight = .60f;
            var ClosestDistanceWeight = .40f;

            var bestWeightedAvg = 0f;

            var highHealthEnemiesNear = EntityManager.Heroes.Enemies.Where(m => !m.IsMelee && m.IsValidTarget(1300f) && m.HealthPercent > 7).ToList();

            var alliesNear = EntityManager.Heroes.Allies.Count(ally => !ally.IsMe && ally.IsValidTarget(1500f, false));

            var enemiesNear = EntityManager.Heroes.Enemies.Where(m => m.IsValidTarget(m.GetAutoAttackRange() + 300f + 65f)).ToList();
            #endregion

            #region 1 Enemy around only
            if (ObjectManager.Player.CountEnemiesInRange(1500f) <= 1)
            {
                //Logic for 1 enemy near
                var backwardsPosition = (ObjectManager.Player.ServerPosition.To2D() + 300f * ObjectManager.Player.Direction.To2D()).To3D();

                if (!backwardsPosition.UnderTurret(true))
                {
                    return backwardsPosition;
                }

            }
            #endregion

            if (enemiesNear.Any(t => t.Health + 15 < myHero.GetAutoAttackDamage(t) * 2 + myHero.GetSpellDamage(t, SpellSlot.Q) && t.Distance(myHero) < t.GetAutoAttackRange() + 80f))
            {
                var QPosition = myHero.ServerPosition.Extend(enemiesNear.OrderBy(t => t.Health).First().ServerPosition, 300f).To3D();

                if (!QPosition.UnderTurret(true))
                {
                    return QPosition;
                }
            }

            #region Alone, 2 Enemies, 1 Killable
            if (enemiesNear.Count() <= 2)
            {
                if (enemiesNear.Any(t => t.Health + 15 < myHero.GetAutoAttackDamage(t) + myHero.GetSpellDamage(t, SpellSlot.Q) && t.Distance(myHero) < t.GetAutoAttackRange() + 80f))
                {
                    var QPosition =
                        myHero.ServerPosition.Extend(
                            highHealthEnemiesNear.OrderBy(t => t.Health).First().ServerPosition, 300f).To3D();

                    if (!QPosition.UnderTurret(true))
                    {
                        return QPosition;
                    }
                }
            }
            #endregion

            #region Alone, 2 Enemies, None Killable
            if (alliesNear == 0 && highHealthEnemiesNear.Count() <= 2 && !enemiesNear.Any(m => m.HealthPercent <= 10))
            {
                var backwardsPosition = (myHero.ServerPosition.To2D() + 300f * myHero.Direction.To2D()).To3D();

                if (!backwardsPosition.UnderTurret(true))
                {
                }
            }
            #endregion

            #region Already in an enemy's attack range.
            var closeNonMeleeEnemy = GetClosestEnemy((Vector3)myHero.ServerPosition.Extend(Game.CursorPos, 300f));

            if (closeNonMeleeEnemy != null
                && myHero.Distance(closeNonMeleeEnemy) <= closeNonMeleeEnemy.AttackRange - 85
                && !closeNonMeleeEnemy.IsMelee)
            {
                return myHero.ServerPosition.Extend(Game.CursorPos, 300f).To3D().IsSafeEx() ? myHero.ServerPosition.Extend(Game.CursorPos, 300f).To3D() : Vector3.Zero;
            }
            #endregion

            #region Logic for multiple enemies / allies around.
            foreach (var position in safePositions)
            {
                var enemy = GetClosestEnemy(position);
                if (!enemy.IsValidTarget())
                {
                    continue;
                }

                var avgDist = GetAvgDistance(position);

                if (avgDist > -1)
                {
                    var closestDist = myHero.ServerPosition.Distance(enemy.ServerPosition);
                    var weightedAvg = closestDist * ClosestDistanceWeight + avgDist * AverageDistanceWeight;
                    if (weightedAvg > bestWeightedAvg && position.IsSafeEx())
                    {
                        bestWeightedAvg = weightedAvg;
                        BestPosition = position.To2D();
                    }
                }
            }
            #endregion

            var endPosition = (BestPosition.To3D().IsSafe()) ? BestPosition.To3D() : Vector3.Zero;

            #region Couldn't find a suitable position, tumble to nearest ally logic
            if (endPosition == Vector3.Zero)
            {
                //Try to find another suitable position. This usually means we are already near too much enemies turrets so just gtfo and tumble
                //to the closest ally ordered by most health.
                var alliesClose = EntityManager.Heroes.Allies.Where(ally => !ally.IsMe && ally.IsValidTarget(1500f, false)).ToList();
                if (alliesClose.Any() && enemiesNear.Any())
                {
                    var closestMostHealth =
                    alliesClose.OrderBy(m => m.Distance(myHero)).ThenByDescending(m => m.Health).FirstOrDefault();

                    if (closestMostHealth != null
                        && closestMostHealth.Distance(enemiesNear.OrderBy(m => m.Distance(myHero)).FirstOrDefault())
                        > myHero.Distance(enemiesNear.OrderBy(m => m.Distance(myHero)).FirstOrDefault()))
                    {
                        var tempPosition = myHero.ServerPosition.Extend(closestMostHealth.ServerPosition, 300f).To3D();
                        if (tempPosition.IsSafeEx())
                        {
                            endPosition = tempPosition;
                        }
                    }

                }

            }
            #endregion

            #region Couldn't even tumble to ally, just go to mouse
            if (endPosition == Vector3.Zero)
            {
                var mousePosition = myHero.ServerPosition.Extend(Game.CursorPos, 300f).To3D();
                if (mousePosition.IsSafe())
                {
                    endPosition = mousePosition;
                }
            }
            #endregion

            return endPosition;
        }

        public static float GetAvgDistance(Vector3 from)
        {
            var numberOfEnemies = from.CountEnemiesInRange(1200f);
            if (numberOfEnemies != 0)
            {
                var enemies = EntityManager.Heroes.Enemies.Where(en => en.IsValidTarget(1200f, true, from) && en.Health > ObjectManager.Player.GetAutoAttackDamage(en) * 3 + myHero.GetSpellDamage(en, SpellSlot.W) + myHero.GetSpellDamage(en, SpellSlot.Q)).ToList();
                var enemiesEx = EntityManager.Heroes.Enemies.Where(en => en.IsValidTarget(1200f, true, from)).ToList();
                var LHEnemies = enemiesEx.Count() - enemies.Count();

                var totalDistance = (LHEnemies > 1 && enemiesEx.Count() > 2) ?
                    enemiesEx.Sum(en => en.Distance(ObjectManager.Player.ServerPosition)) :
                    enemies.Sum(en => en.Distance(ObjectManager.Player.ServerPosition));

                return totalDistance / numberOfEnemies;
            }
            return -1;
        }

        public static Obj_AI_Base GetCondemnTarget(Vector3 fromPosition)
        {
            return GetTargetVHR(fromPosition);
        }

        public static Obj_AI_Base GetTargetVHR(Vector3 fromPosition)
        {
            var HeroList = EntityManager.Heroes.Enemies.Where(h => h.IsValidTarget(E.Range) && !h.HasBuffOfType(BuffType.SpellShield) && !h.HasBuffOfType(BuffType.SpellImmunity));

            var MinChecksPercent = 25;
            var PushDistance = EPushDistanceSlider;

            if (PushDistance >= 410)
            {
                var PushEx = PushDistance;
                PushDistance -= (10 + (PushEx - 410) / 2);
            }

            if (ObjectManager.Player.ServerPosition.UnderTurret(true))
            {
                return null;
            }

            foreach (var Hero in HeroList)
            {
                if (Hero.NetworkId != TargetSelector.SelectedTarget.NetworkId)
                {
                    continue;
                }

                if (Hero.Health + 10 <= ObjectManager.Player.GetAutoAttackDamage(Hero) * 2)
                {
                    continue;
                }

                var targetPosition = E2.GetPrediction(Hero).UnitPosition;
                var finalPosition = targetPosition.Extend(ObjectManager.Player.ServerPosition, -PushDistance);
                var finalPosition_ex = Hero.ServerPosition.Extend(ObjectManager.Player.ServerPosition, -PushDistance);

                var condemnRectangle = new VHRPolygon(VHRPolygon.Rectangle(targetPosition.To2D(), finalPosition, Hero.BoundingRadius));
                var condemnRectangle_ex = new VHRPolygon(VHRPolygon.Rectangle(Hero.ServerPosition.To2D(), finalPosition_ex, Hero.BoundingRadius));

                if (IsBothNearWall(Hero))
                {
                    return null;
                }

                if (condemnRectangle.Points.Count(point => NavMesh.GetCollisionFlags(point.X, point.Y).HasFlag(CollisionFlags.Wall)) >= condemnRectangle.Points.Count() * (MinChecksPercent / 100f) && condemnRectangle_ex.Points.Count(point => NavMesh.GetCollisionFlags(point.X, point.Y).HasFlag(CollisionFlags.Wall)) >= condemnRectangle_ex.Points.Count() * (MinChecksPercent / 100f))
                {
                    return Hero;
                }
            }
            return null;
        }

        private static Vector3[] GetWallQPositions(Obj_AI_Base player, float Range)
        {
            Vector3[] vList =
            {
                (player.ServerPosition.To2D() + Range * player.Direction.To2D()).To3D(),
                (player.ServerPosition.To2D() - Range * player.Direction.To2D()).To3D()

            };
            return vList;
        }

        private static bool IsBothNearWall(Obj_AI_Base target)
        {
            var positions = GetWallQPositions(target, 110).ToList().OrderBy(pos => pos.Distance(target.ServerPosition, true));
            var positions_ex = GetWallQPositions(ObjectManager.Player, 110).ToList().OrderBy(pos => pos.Distance(ObjectManager.Player.ServerPosition, true));

            if (positions.Any(p => p.IsWall()) && positions_ex.Any(p => p.IsWall()))
            {
                return true;
            }
            return false;
        }

        public static Vector3 GetSmartQPosition()
        {
            if (!smartq || !E.IsReady())
            {
                return Vector3.Zero;
            }

            const int currentStep = 30;
            var direction = ObjectManager.Player.Direction.To2D().Perpendicular();
            for (var i = 0f; i < 360f; i += currentStep)
            {
                var angleRad = DegreeToRadian(i);
                var rotatedPosition = ObjectManager.Player.Position.To2D() + (300f * direction.Rotated(angleRad));
                if (GetCondemnTarget(rotatedPosition.To3D()).IsValidTarget() && rotatedPosition.To3D().IsSafe())
                {
                    return rotatedPosition.To3D();
                }
            }

            return Vector3.Zero;
        }

        public static List<AIHeroClient> GetLhEnemiesNear(this Vector3 position, float range, float healthpercent)
        {
            return EntityManager.Heroes.Enemies.Where(hero => hero.IsValidTarget(range, true, position) && hero.HealthPercent <= healthpercent).ToList();
        }

        public static bool UnderAllyTurret_Ex(this Vector3 position)
        {
            return ObjectManager.Get<Obj_AI_Turret>().Any(t => t.IsAlly && !t.IsDead);
        }

        public static IEnumerable<AIHeroClient> EnemiesClose
        {
            get
            {
                return EntityManager.Heroes.Enemies.Where(m => m.Distance(myHero, true) <= Math.Pow(1000, 2) && m.IsValidTarget(1500, false) && m.CountEnemiesInRange(m.IsMelee ? m.AttackRange * 1.5f : m.AttackRange + 20 * 1.5f) > 0);
            }
        }

        public static List<Vector2> GetEnemyPoints(bool dynamic = true)
        {
            var staticRange = 360f;
            var polygonsList = EnemiesClose.Select(enemy => new Geometry.Circle(enemy.ServerPosition.To2D(), (dynamic ? (enemy.IsMelee ? enemy.AttackRange * 1.5f : enemy.AttackRange) : staticRange) + enemy.BoundingRadius + 20).ToPolygon()).ToList();
            var pathList = Geometry.ClipPolygons(polygonsList);
            var pointList = pathList.SelectMany(path => path, (path, point) => new Vector2(point.X, point.Y)).Where(currentPoint => !currentPoint.IsWall()).ToList();
            return pointList;
        }

        public static bool IsSafe(this Vector3 position, bool noQIntoEnemiesCheck = false)
        {
            if (position.UnderTurret(true) && !myHero.UnderTurret(true))
            {
                return false;
            }

            var allies = position.CountAlliesInRange(myHero.AttackRange);
            var enemies = position.CountEnemiesInRange(myHero.AttackRange);
            var lhEnemies = position.GetLhEnemiesNear(myHero.AttackRange, 15).Count();

            if (enemies <= 1) ////It's a 1v1, safe to assume I can Q
            {
                return true;
            }

            if (position.UnderAllyTurret_Ex())
            {
                var nearestAllyTurret = ObjectManager.Get<Obj_AI_Turret>().Where(a => a.IsAlly).OrderBy(d => d.Distance(position, true)).FirstOrDefault();

                if (nearestAllyTurret != null)
                {
                    ////We're adding more allies, since the turret adds to the firepower of the team.
                    allies += 2;
                }
            }

            ////Adding 1 for my Player
            var normalCheck = (allies + 1 > enemies - lhEnemies);
            var QEnemiesCheck = true;

            if (noqenemies && noQIntoEnemiesCheck)
            {
                if (!noqenemiesold)
                {
                    var Vector2Position = position.To2D();
                    var enemyPoints = dynamicqsafety ? GetEnemyPoints() : GetEnemyPoints(false);
                    if (enemyPoints.Contains(Vector2Position) && !qspam)
                    {
                        QEnemiesCheck = false;
                    }

                    var closeEnemies = EntityManager.Heroes.Enemies.FindAll(en => en.IsValidTarget(1500f) && !(en.Distance(myHero.ServerPosition) < en.AttackRange + 65f)).OrderBy(en => en.Distance(position));

                    if (!closeEnemies.All(enemy => position.CountEnemiesInRange(dynamicqsafety ? enemy.AttackRange : 405f) <= 1))
                    {
                        QEnemiesCheck = false;
                    }
                }
                else
                {
                    var closeEnemies = EntityManager.Heroes.Enemies.FindAll(en => en.IsValidTarget(1500f)).OrderBy(en => en.Distance(position));
                    if (closeEnemies.Any())
                    {
                        QEnemiesCheck = !closeEnemies.All(enemy => position.CountEnemiesInRange(dynamicqsafety ? enemy.AttackRange : 405f) <= 1);
                    }
                }
            }

            return normalCheck && QEnemiesCheck;
        }

        public static bool UnderAllyTurret(Vector3 pos)
        {
            return ObjectManager.Get<Obj_AI_Turret>().Any(t => t.IsAlly && !t.IsDead && pos.Distance(t) <= 900);
        }

        #endregion

        #region Menu Items
        public static bool UseQBool { get { return ComboMenu["useq"].Cast<CheckBox>().CurrentValue; } }
        public static int QModeStringList { get { return QSettings["qmode"].Cast<Slider>().CurrentValue; } }
        public static int UseQAntiGapcloserStringList { get { return ComboMenu["qantigc"].Cast<Slider>().CurrentValue; } }
        public static bool TryToFocus2WBool { get { return ComboMenu["focus2w"].Cast<CheckBox>().CurrentValue; } }
        public static bool DontAttackWhileInvisibleAndMeelesNearBool { get { return ComboMenu["dontattackwhileinvisible"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseRBool { get { return ComboMenu["user"].Cast<KeyBind>().CurrentValue; } }
        public static bool UseEBool { get { return CondemnSettings["usee"].Cast<CheckBox>().CurrentValue; } }
        public static int EModeStringList { get { return CondemnSettings["emode"].Cast<Slider>().CurrentValue; } }
        public static bool UseEInterruptBool { get { return CondemnSettings["useeinterrupt"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseEAntiGapcloserBool { get { return CondemnSettings["useeantigapcloser"].Cast<CheckBox>().CurrentValue; } }
        public static int EPushDistanceSlider { get { return CondemnSettings["epushdist"].Cast<Slider>().CurrentValue; } }
        public static int EHitchanceSlider { get { return CondemnSettings["ehitchance"].Cast<Slider>().CurrentValue; } }
        public static bool SemiAutomaticCondemnKey { get { return CondemnSettings["semiautoekey"].Cast<KeyBind>().CurrentValue; } }
        public static bool UseEAs3rdWProcBool { get { return ExtraMenu["usee3rdwproc"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseQBonusOnEnemiesNotCS { get { return ExtraMenu["useqonenemiesnotcs"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseQOnlyAt2WStacksBool { get { return ExtraMenu["useqonlyon2stackedenemies"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseQFarm { get { return FarmSettings["useqfarm"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseEJungleFarm { get { return FarmSettings["useejgfarm"].Cast<CheckBox>().CurrentValue; } }
        public static bool DrawWStacksBool { get { return DrawingMenu["drawwstacks"].Cast<CheckBox>().CurrentValue; } }
        public static int GetAutoR { get { return ComboMenu["GetAutoR"].Cast<Slider>().CurrentValue; } }
        public static bool dynamicqsafety { get { return QSettings["dynamicqsafety"].Cast<CheckBox>().CurrentValue; } }
        public static bool qspam { get { return QSettings["qspam"].Cast<CheckBox>().CurrentValue; } }
        public static bool noqenemies { get { return QSettings["noqenemies"].Cast<CheckBox>().CurrentValue; } }
        public static bool noqenemiesold { get { return QSettings["noqenemiesold"].Cast<CheckBox>().CurrentValue; } }
        public static bool antiMelee { get { return QSettings["antiMelee"].Cast<CheckBox>().CurrentValue; } }
        public static int Accuracy { get { return ESettings["Accuracy"].Cast<Slider>().CurrentValue; } }
        public static int TumbleCondemnCount { get { return ESettings["TumbleCondemnCount"].Cast<Slider>().CurrentValue; } }
        public static int sacMode { get { return QSettings["sacMode"].Cast<Slider>().CurrentValue; } }
        public static bool TumbleCondemn { get { return ESettings["TumbleCondemn"].Cast<CheckBox>().CurrentValue; } }
        public static bool TumbleCondemnSafe { get { return ESettings["TumbleCondemnSafe"].Cast<CheckBox>().CurrentValue; } }
        public static bool DontCondemnTurret { get { return ESettings["DontCondemnTurret"].Cast<CheckBox>().CurrentValue; } }
        public static bool DontSafeCheck { get { return QSettings["DontSafeCheck"].Cast<CheckBox>().CurrentValue; } }
        public static bool DontQIntoEnemies { get { return QSettings["DontQIntoEnemies"].Cast<CheckBox>().CurrentValue; } }
        public static bool Wall { get { return QSettings["Wall"].Cast<CheckBox>().CurrentValue; } }
        public static bool Only2W { get { return QSettings["Only2W"].Cast<CheckBox>().CurrentValue; } }
        public static int CSSDelay { get { return ItemMenu["CSSDelay"].Cast<Slider>().CurrentValue; } }
        public static int cleanHP { get { return ItemMenu["cleanHP"].Cast<Slider>().CurrentValue; } }
        public static bool CleanSpells { get { return ItemMenu["CleanSpells"].Cast<CheckBox>().CurrentValue; } }
        public static bool Stun { get { return ItemMenu["Stun"].Cast<CheckBox>().CurrentValue; } }
        public static bool Snare { get { return ItemMenu["Snare"].Cast<CheckBox>().CurrentValue; } }
        public static bool Charm { get { return ItemMenu["Charm"].Cast<CheckBox>().CurrentValue; } }
        public static bool Fear { get { return ItemMenu["Fear"].Cast<CheckBox>().CurrentValue; } }
        public static bool Suppression { get { return ItemMenu["Suppression"].Cast<CheckBox>().CurrentValue; } }
        public static bool Taunt { get { return ItemMenu["Taunt"].Cast<CheckBox>().CurrentValue; } }
        public static bool Blind { get { return ItemMenu["Blind"].Cast<CheckBox>().CurrentValue; } }
        public static bool _Clean { get { return ItemMenu["Clean"].Cast<CheckBox>().CurrentValue; } }
        public static bool useBotrk { get { return ItemMenu["useBotrk"].Cast<CheckBox>().CurrentValue; } }
        public static bool useCutlass { get { return ItemMenu["useCutlass"].Cast<CheckBox>().CurrentValue; } }
        public static bool useGhostBlade { get { return ItemMenu["useGhostBlade"].Cast<CheckBox>().CurrentValue; } }
        public static bool smartq { get { return QSettings["smartq"].Cast<CheckBox>().CurrentValue; } }
        #endregion

        #region Menu

        private static Menu Menu, ComboMenu, QSettings, CondemnSettings, ESettings, FarmSettings, ExtraMenu, DrawingMenu, ItemMenu;

        private static void InitMenu()
        {
            Menu = MainMenu.AddMenu("Vayne", "Vayne");
            Menu.AddLabel("Base Ported from Challenger Series & features ported from many other assemblies on L# - Berb");
            Menu.AddSeparator();

            ComboMenu = Menu.AddSubMenu("Combo Settings", "combo");
            ComboMenu.AddGroupLabel("Combo");
            ComboMenu.AddSeparator();
            ComboMenu.Add("useq", new CheckBox("Use Q")); // UseQBool
            ComboMenu.AddSeparator();
            ComboMenu.AddLabel("1 : Never | 2 : E-Not-Ready | 3 : Always");
            ComboMenu.Add("qantigc", new Slider("Use Q Antigapcloser:", 3, 1, 3)); // UseQAntiGapcloserStringList
            ComboMenu.AddSeparator();
            ComboMenu.Add("focus2w", new CheckBox("Try To Focus 2W", false)); // TryToFocus2WBool
            ComboMenu.Add("dontattackwhileinvisible", new CheckBox("Smart Invisible Attacking")); // DontAttackWhileInvisibleAndMeelesNearBool
            ComboMenu.AddSeparator();
            ComboMenu.Add("user", new KeyBind("Use R In Combo", false, KeyBind.BindTypes.PressToggle, 'A')); // UseRBool
            ComboMenu.Add("GetAutoR", new Slider("R if >= X enemies : ", 2, 1, 5)); // GetAutoR
            ComboMenu.AddSeparator();

            QSettings = Menu.AddSubMenu("Q Settings", "qsettings");
            QSettings.AddGroupLabel("Q Settings");
            QSettings.AddSeparator();
            QSettings.AddLabel("1 : Prada | 2 : Marksman | 3 : VHR | 4 : Sharpshooter | 5 : SAC");
            QSettings.Add("qmode", new Slider("Q Mode:", 5, 1, 5)); // QModeStringList
            QSettings.AddSeparator();
            QSettings.AddGroupLabel("VHR Q Settings");
            QSettings.AddLabel("YOU HAVE TO HAVE OPTION 3 SELECTED TO USE THIS");
            QSettings.Add("dynamicqsafety", new CheckBox("Use dynamic Q Safety Distance", true)); // dynamicqsafety
            QSettings.Add("qspam", new CheckBox("Ignore Q checks", true)); // qspam
            QSettings.Add("noqenemies", new CheckBox("Don't Q into enemies", true)); // noqenemies
            QSettings.Add("noqenemiesold", new CheckBox("Use Old Don't Q into enemies", true)); // noqenemiesold
            QSettings.Add("smartq", new CheckBox("Try to QE when possible", true)); // noqenemiesold
            QSettings.AddSeparator();
            QSettings.AddGroupLabel("Sharpshooter Q Settings");
            QSettings.AddLabel("YOU HAVE TO HAVE OPTION 4 SELECTED TO USE THIS");
            QSettings.Add("antiMelee", new CheckBox("Use Anti-Melee (Q)", true)); // antiMelee
            QSettings.AddSeparator();
            QSettings.AddGroupLabel("Synx Auto Carry Q Settings");
            QSettings.AddLabel("YOU HAVE TO HAVE OPTION 5 SELECTED TO USE THIS");
            QSettings.AddLabel("1 : Auto Position | 2 : Mouse Cursor");
            QSettings.Add("sacMode", new Slider("Q Mode: ", 1, 1, 2)); // sacMode
            QSettings.Add("DontSafeCheck", new CheckBox("Dont check tumble position is safe", true)); // DontSafeCheck
            QSettings.Add("DontQIntoEnemies", new CheckBox("Dont Q Into Enemies", true)); // DontQIntoEnemies
            QSettings.Add("Wall", new CheckBox("Always Tumble to wall if possible", true)); // Wall
            QSettings.Add("Only2W", new CheckBox("Tumble only when enemy has 2 w stacks", false)); // Only2W
            QSettings.AddSeparator();

            CondemnSettings = Menu.AddSubMenu("Condemn Settings", "condemnsettings");
            CondemnSettings.AddGroupLabel("Condemn Menu");
            CondemnSettings.AddSeparator();
            CondemnSettings.Add("usee", new CheckBox("Auto E")); // UseEBool
            CondemnSettings.AddSeparator();
            CondemnSettings.AddLabel("1 : Prada Smart | 2 : Prada Perfect | 3 : Marksman");
            CondemnSettings.AddLabel("4 : Sharpshooter | 5 : Gosu | 6 : VHR");
            CondemnSettings.AddLabel("7 : Prada Legacy | 8 : Fastest | 9 : Old Prada");
            CondemnSettings.AddLabel("10 : Synx Auto Carry");
            CondemnSettings.Add("emode", new Slider("E Mode: ", 10, 1, 10)); // EModeStringList
            CondemnSettings.AddSeparator();
            CondemnSettings.Add("useeinterrupt", new CheckBox("Use E To Interrupt")); // UseEInterruptBool
            CondemnSettings.Add("useeantigapcloser", new CheckBox("Use E AntiGapcloser")); // UseEAntiGapcloserBool
            CondemnSettings.AddSeparator();
            CondemnSettings.Add("epushdist", new Slider("E Push Distance: ", 425, 300, 475)); // EPushDistanceSlider
            CondemnSettings.AddSeparator();
            CondemnSettings.Add("ehitchance", new Slider("Condemn Hitchance", 50, 0, 100)); // EHitchanceSlider
            CondemnSettings.AddSeparator();
            CondemnSettings.Add("semiautoekey", new KeyBind("Semi Automatic Condemn", false, KeyBind.BindTypes.PressToggle, 'E')); // SemiAutomaticCondemnKey
            CondemnSettings.AddSeparator();


            ESettings = Menu.AddSubMenu("E Settings", "esettings");
            ESettings.AddGroupLabel("SAC Condemn Settings");
            ESettings.AddSeparator();
            ESettings.AddLabel("YOU HAVE TO HAVE OPTION 10 SELECTED TO USE THIS");
            ESettings.Add("Accuracy", new Slider("Accuracy", 12, 2, 12)); // Accuracy
            ESettings.Add("TumbleCondemnCount", new Slider("Q->E Position Check Count", 12, 2, 12)); // TumbleCondemnCount
            ESettings.Add("TumbleCondemn", new CheckBox("Q->E when possible")); // TumbleCondemn
            ESettings.AddSeparator();
            ESettings.Add("TumbleCondemnSafe", new CheckBox("Only Q->E when tumble position is safe", false)); // TumbleCondemnSafe
            ESettings.Add("DontCondemnTurret", new CheckBox("Don't Condemn under turret?", true)); // TumbleCondemnSafe
            ESettings.AddSeparator();

            FarmSettings = Menu.AddSubMenu("Farm Settings", "farm");
            FarmSettings.AddGroupLabel("Farm Menu");
            FarmSettings.AddSeparator();
            FarmSettings.Add("useqfarm", new CheckBox("Use Q Farm/Jungle")); // UseQFarm
            FarmSettings.Add("useejgfarm", new CheckBox("Use E Jungle")); // UseEJungleFarm
            FarmSettings.AddSeparator();

            ExtraMenu = Menu.AddSubMenu("Extra Settings", "extra");
            ExtraMenu.AddGroupLabel("Extra Settings");
            ExtraMenu.AddSeparator();
            ExtraMenu.Add("usee3rdwproc", new CheckBox("Use E as 3rd W Proc Before LVL: ", false)); // UseEAs3rdWProcBool
            ExtraMenu.Add("useqonenemiesnotcs", new CheckBox("Use Q Bonus On ENEMY not CS", false)); // UseQBonusOnEnemiesNotCS
            ExtraMenu.Add("useqonlyon2stackedenemies", new CheckBox("Use Q If Enemy Have 2W Stacks", false)); // UseQOnlyAt2WStacksBool
            ExtraMenu.AddSeparator();

            ItemMenu = Menu.AddSubMenu("Activator", "item");
            ItemMenu.Add("useBotrk", new CheckBox("Use Blade of the Ruined King?"));
            ItemMenu.Add("useCutlass", new CheckBox("Use Bilgewater Cutlass?"));
            ItemMenu.Add("useGhostBlade", new CheckBox("Use GhostBlade?"));
            ItemMenu.AddSeparator();
            ItemMenu.Add("Clean", new CheckBox("Auto QSS/Mercurial/Dervish/Mikaels/Cleanse"));
            ItemMenu.AddSeparator();
            ItemMenu.Add("CSSDelay", new Slider("QSS Delay", 0, 0, 1000)); // CSSDelay
            ItemMenu.AddSeparator();
            foreach (var ally in ObjectManager.Get<AIHeroClient>().Where(ally => ally.IsAlly))
            {
                ItemMenu.Add("MikaelsAlly" + ally.ChampionName, new CheckBox("Mikael : " + ally.ChampionName + "?"));
            }
            ItemMenu.AddSeparator();
            ItemMenu.Add("cleanHP", new Slider("Use only under % HP", 95, 0, 100));  // cleanHP
            ItemMenu.AddSeparator();
            ItemMenu.Add("CleanSpells", new CheckBox("Cleanse Dangerous (Zed R etc.)"));
            ItemMenu.Add("Stun", new CheckBox("Stun"));
            ItemMenu.Add("Snare", new CheckBox("Snare"));
            ItemMenu.Add("Charm", new CheckBox("Charm"));
            ItemMenu.Add("Fear", new CheckBox("Fear"));
            ItemMenu.Add("Suppression", new CheckBox("Suppression"));
            ItemMenu.Add("Taunt", new CheckBox("Taunt"));
            ItemMenu.Add("Blind", new CheckBox("Blind"));
            ItemMenu.AddSeparator();

            DrawingMenu = Menu.AddSubMenu("Draw Settings", "draw");
            DrawingMenu.AddGroupLabel("Drawing Menu");
            DrawingMenu.AddSeparator();
            DrawingMenu.Add("drawwstacks", new CheckBox("Draw W Stacks")); // DrawWStacksBool
            DrawingMenu.AddSeparator();
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

            if (mode == 10)
            {
                if (IsValidTarget(hero))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsValidTarget(AIHeroClient target)
        {
            var targetPosition = Geometry.PositionAfter(target.GetWaypoints(), 300, (int)target.MoveSpeed);

            if (target.Distance(ObjectManager.Player.ServerPosition) < 650f && IsCondemnable(ObjectManager.Player.ServerPosition.To2D(), targetPosition, target.BoundingRadius))
            {
                if (target.Path.Length == 0)
                {
                    var outRadius = (0.3f * target.MoveSpeed) / (float)Math.Cos(2 * Math.PI / Accuracy);
                    int count = 0;
                    for (int i = 1; i <= Accuracy; i++)
                    {
                        if (count + (Accuracy - i) < Accuracy / 3)
                            return false;

                        var angle = i * 2 * Math.PI / Accuracy;
                        float x = target.Position.X + outRadius * (float)Math.Cos(angle);
                        float y = target.Position.Y + outRadius * (float)Math.Sin(angle);
                        if (IsCondemnable(ObjectManager.Player.ServerPosition.To2D(), new Vector2(x, y), target.BoundingRadius))
                            count++;
                    }
                    return count >= Accuracy / 3;
                }
                else
                    return true;
            }
            else
            {
                if (TumbleCondemn && Q.IsReady())
                {
                    var outRadius = 300 / (float)Math.Cos(2 * Math.PI / TumbleCondemnCount);

                    for (int i = 1; i <= TumbleCondemnCount; i++)
                    {
                        var angle = i * 2 * Math.PI / TumbleCondemnCount;
                        float x = ObjectManager.Player.Position.X + outRadius * (float)Math.Cos(angle);
                        float y = ObjectManager.Player.Position.Y + outRadius * (float)Math.Sin(angle);
                        targetPosition = Geometry.PositionAfter(target.GetWaypoints(), 300, (int)target.MoveSpeed);
                        var vec = new Vector2(x, y);
                        if (targetPosition.Distance(vec) < 550f && IsCondemnable(vec, targetPosition, target.BoundingRadius, 300f))
                        {
                            if (!TumbleCondemnSafe || IsSafe(target, vec.To3D(), false).IsValid())
                            {
                                myHero.Spellbook.CastSpell(SpellSlot.Q, (Vector3)vec);
                                break;
                            }
                        }
                    }

                    return false;
                }
            }

            return false;
        }

        internal static Vector2 Deviation(Vector2 point1, Vector2 point2, double angle)
        {
            angle *= Math.PI / 180.0;
            Vector2 temp = Vector2.Subtract(point2, point1);
            Vector2 result = new Vector2(0);
            result.X = (float)(temp.X * Math.Cos(angle) - temp.Y * Math.Sin(angle)) / 4;
            result.Y = (float)(temp.X * Math.Sin(angle) + temp.Y * Math.Cos(angle)) / 4;
            result = Vector2.Add(result, point1);
            return result;
        }

        public static Vector3 IsSafe(AIHeroClient target, Vector3 vec, bool checkTarget = true)
        {
            if (DontSafeCheck)
                return vec;

            if (checkTarget)
            {
                if (target.ServerPosition.To2D().Distance(vec) <= target.AttackRange)
                {
                    if (vec.CountEnemiesInRange(1000) > 1)
                        return Vector3.Zero;
                    else if (target.ServerPosition.To2D().Distance(vec) <= target.AttackRange / 2f)
                        return Deviation(ObjectManager.Player.ServerPosition.To2D(), target.ServerPosition.To2D(), 60).To3D();
                }

                if (((DontQIntoEnemies || target.IsMelee) && EntityManager.Heroes.Enemies.Any(p => p.ServerPosition.To2D().Distance(vec) <= p.AttackRange + ObjectManager.Player.BoundingRadius + (p.IsMelee ? 100 : 0))) || vec.UnderTurret(true))
                    return Vector3.Zero;
            }
            if (EntityManager.Heroes.Enemies.Any(p => p.NetworkId != target.NetworkId && p.ServerPosition.To2D().Distance(vec) <= p.AttackRange + (p.IsMelee ? 50 : 0)) || vec.UnderTurret(true))
                return Vector3.Zero;

            return vec;
        }

        public static Vector3 FindTumblePosition(AIHeroClient target)
        {
            if ((Only2W) && target.GetBuffCount("vaynesilvereddebuff") == 1) // == 1 cuz calling this after attack which is aa missile still flying
                return Vector3.Zero;

            if (Wall)
            {
                var outRadius = ObjectManager.Player.BoundingRadius / (float)Math.Cos(2 * Math.PI / 8);

                for (var i = 1; i <= 8; i++)
                {
                    var angle = i * 2 * Math.PI / 8;
                    float x = ObjectManager.Player.Position.X + outRadius * (float)Math.Cos(angle);
                    float y = ObjectManager.Player.Position.Y + outRadius * (float)Math.Sin(angle);
                    var colFlags = NavMesh.GetCollisionFlags(x, y);
                    if (colFlags.HasFlag(CollisionFlags.Wall) || colFlags.HasFlag(CollisionFlags.Building))
                        return new Vector3(x, y, 0);
                }
            }

            if (sacMode == 0)
            {
                Vector3 vec = target.ServerPosition;

                if (target.Path.Length > 0)
                {
                    if (ObjectManager.Player.Distance(vec) < ObjectManager.Player.Distance(target.Path.Last()))
                        return IsSafe(target, Game.CursorPos);
                    else
                        return IsSafe(target, Game.CursorPos.To2D().Rotated(DegreeToRadian((vec - ObjectManager.Player.ServerPosition).To2D().AngleBetween((Game.CursorPos - ObjectManager.Player.ServerPosition).To2D()) % 90)).To3D());
                }
                else
                {
                    if (target.IsMelee)
                        return IsSafe(target, Game.CursorPos);
                }

                return IsSafe(target, ObjectManager.Player.ServerPosition + (target.ServerPosition - ObjectManager.Player.ServerPosition).Normalized().To2D().Rotated(DegreeToRadian(90 - (vec - ObjectManager.Player.ServerPosition).To2D().AngleBetween((Game.CursorPos - ObjectManager.Player.ServerPosition).To2D()))).To3D() * 300f);
            }
            else if (sacMode == 1)
            {
                return Game.CursorPos;
            }

            return Vector3.Zero;
        }

        private static bool IsCondemnable(Vector2 from, Vector2 targetPosition, float boundingRadius, float pushRange = -1)
        {
            if (pushRange == -1)
                pushRange = EPushDistanceSlider - 20f;

            var pushDirection = (targetPosition - from).Normalized();
            for (int i = 0; i < pushRange; i += 20)
            {
                var lastPost = targetPosition + (pushDirection * i);
                if (!lastPost.To3D().UnderTurret(true) || !DontCondemnTurret)
                {
                    var colFlags = NavMesh.GetCollisionFlags(lastPost.X, lastPost.Y);
                    if (colFlags.HasFlag(CollisionFlags.Wall) || colFlags.HasFlag(CollisionFlags.Building))
                    {
                        var sideA = lastPost + pushDirection * 20f + (pushDirection.Perpendicular() * boundingRadius);
                        var sideB = lastPost + pushDirection * 20f - (pushDirection.Perpendicular() * boundingRadius);

                        var flagsA = NavMesh.GetCollisionFlags(sideA.X, sideA.Y);
                        var flagsB = NavMesh.GetCollisionFlags(sideB.X, sideB.Y);

                        if ((flagsA.HasFlag(CollisionFlags.Wall) || flagsA.HasFlag(CollisionFlags.Building)) && (flagsB.HasFlag(CollisionFlags.Wall) || flagsB.HasFlag(CollisionFlags.Building)))
                            return true;
                    }
                }
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

    class VHRPolygon
    {
        public List<Vector2> Points;

        public VHRPolygon(List<Vector2> p)
        {
            Points = p;
        }

        public void Add(Vector2 vec)
        {
            Points.Add(vec);
        }

        public int Count()
        {
            return Points.Count;
        }

        public bool Contains(Vector2 point)
        {
            var result = false;
            var j = Count() - 1;
            for (var i = 0; i < Count(); i++)
            {
                if (Points[i].Y < point.Y && Points[j].Y >= point.Y || Points[j].Y < point.Y && Points[i].Y >= point.Y)
                {
                    if (Points[i].X +
                        (point.Y - Points[i].Y) / (Points[j].Y - Points[i].Y) * (Points[j].X - Points[i].X) < point.X)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }
        public static List<Vector2> Rectangle(Vector2 startVector2, Vector2 endVector2, float radius)
        {
            var points = new List<Vector2>();

            var v1 = endVector2 - startVector2;
            var to1Side = Vector2.Normalize(v1).Perpendicular() * radius;

            points.Add(startVector2 + to1Side);
            points.Add(startVector2 - to1Side);
            points.Add(endVector2 - to1Side);
            points.Add(endVector2 + to1Side);
            return points;
        }
    }
}
