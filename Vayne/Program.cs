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
            var direction = (Game.CursorPos - myHero.ServerPosition).Normalized().To2D();

            var list = new List<Vector3>();
            for (var i = -70; i <= 70; i += currentStep)
            {
                var angleRad = DegreeToRadian(i);
                var rotatedPosition = myHero.Position.To2D() + (300f * direction.Rotated(angleRad));
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
            var BestPosition = myHero.ServerPosition.Extend(Game.CursorPos, 300f);
            var AverageDistanceWeight = .60f;
            var ClosestDistanceWeight = .40f;

            var bestWeightedAvg = 0f;

            var highHealthEnemiesNear = EntityManager.Heroes.Enemies.Where(m => !m.IsMelee && m.IsValidTarget(1300f) && m.HealthPercent > 7).ToList();

            var alliesNear = EntityManager.Heroes.Allies.Count(ally => !ally.IsMe && ally.IsValidTarget(1500f, false));

            var enemiesNear = EntityManager.Heroes.Enemies.Where(m => m.IsValidTarget(myHero.GetAutoAttackRange() + 300f + 65f)).ToList();
            #endregion

            #region 1 Enemy around only
            if (myHero.CountEnemiesInRange(1500f) <= 1)
            {
                //Logic for 1 enemy near
                var backwardsPosition = (myHero.ServerPosition.To2D() + 300f * myHero.Direction.To2D()).To3D();

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

        public static Vector3 GetSmartQPosition()
        {
            if (!E.IsReady())
            {
                return Vector3.Zero;
            }

            const int currentStep = 30;
            var direction = myHero.Direction.To2D().Perpendicular();
            for (var i = 0f; i < 360f; i += currentStep)
            {
                var angleRad = EloBuddy.SDK.Geometry.DegreeToRadian(i);
                var rotatedPosition = myHero.Position.To2D() + (300f * direction.Rotated(angleRad));
                var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                if (target.IsValidTarget() && rotatedPosition.To3D().IsSafe())
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
        public static bool dynamicqsafety { get { return Menu["dynamicqsafety"].Cast<CheckBox>().CurrentValue; } }
        public static bool qspam { get { return Menu["qspam"].Cast<CheckBox>().CurrentValue; } }
        public static bool noqenemies { get { return Menu["noqenemies"].Cast<CheckBox>().CurrentValue; } }
        public static bool noqenemiesold { get { return Menu["noqenemiesold"].Cast<CheckBox>().CurrentValue; } }
        public static bool antiMelee { get { return Menu["antiMelee"].Cast<CheckBox>().CurrentValue; } }
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

            Menu.AddGroupLabel("VHR Q Settings");
            Menu.AddLabel("YOU HAVE TO HAVE OPTION 3 SELECTED TO USE THIS");
            Menu.Add("dynamicqsafety", new CheckBox("Use dynamic Q Safety Distance", true)); // dynamicqsafety
            Menu.Add("qspam", new CheckBox("Ignore Q checks", true)); // qspam
            Menu.Add("noqenemies", new CheckBox("Don't Q into enemies", true)); // noqenemies
            Menu.Add("noqenemiesold", new CheckBox("Use Old Don't Q into enemies", true)); // noqenemiesold
            Menu.AddSeparator();

            Menu.AddGroupLabel("Sharpshooter Settings");
            Menu.AddLabel("YOU HAVE TO HAVE OPTION 4 SELECTED TO USE THIS");
            Menu.Add("antiMelee", new CheckBox("Use Anti-Melee (Q)", true)); // antiMelee
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
