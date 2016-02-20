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

namespace KhaZix
{
    public static class Program
    {

        private static Spell.Skillshot W, E, WE;
        private static Spell.Active R;
        private static Spell.Targeted Q;

        private static Menu Menu;

        internal const float Wangle = 22 * (float)Math.PI / 180;
        internal static bool EvolvedQ, EvolvedW, EvolvedE, EvolvedR;
        internal static SpellSlot IgniteSlot;
        internal static List<AIHeroClient> HeroList;
        internal static List<Vector3> EnemyTurretPositions = new List<Vector3>();
        internal static Vector3 NexusPosition;
        internal static Vector3 Jumppoint1, Jumppoint2;
        internal static bool Jumping;

        private static AIHeroClient myHero
        {
            get { return Player.Instance; }
        }

        public static void Main()
        {
            Loading.OnLoadingComplete += OnLoad;
        }

        #region Menu Items
        public static bool UseQCombo { get { return Menu["UseQCombo"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseWCombo { get { return Menu["UseWCombo"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseECombo { get { return Menu["UseECombo"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseRCombo { get { return Menu["UseRCombo"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseEGapclose { get { return Menu["UseEGapclose"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseEGapcloseW { get { return Menu["UseEGapcloseW"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseRGapcloseW { get { return Menu["UseRGapcloseW"].Cast<CheckBox>().CurrentValue; } }
        public static bool djumpenabled { get { return Menu["djumpenabled"].Cast<CheckBox>().CurrentValue; } }
        public static bool save { get { return Menu["save"].Cast<CheckBox>().CurrentValue; } }
        public static bool noauto { get { return Menu["noauto"].Cast<CheckBox>().CurrentValue; } }
        public static bool jcursor { get { return Menu["jcursor"].Cast<CheckBox>().CurrentValue; } }
        public static bool jcursor2 { get { return Menu["jcursor2"].Cast<CheckBox>().CurrentValue; } }
        public static bool SafetyEnabled { get { return Menu["Safety.Enabled"].Cast<CheckBox>().CurrentValue; } }
        public static bool SafetyCountCheck { get { return Menu["Safety.CountCheck"].Cast<CheckBox>().CurrentValue; } }
        public static bool SafetyTowerJump { get { return Menu["Safety.TowerJump"].Cast<CheckBox>().CurrentValue; } }

        public static int JEDelay { get { return Menu["JEDelay"].Cast<Slider>().CurrentValue; } }
        public static int delayQ { get { return Menu["delayQ"].Cast<Slider>().CurrentValue; } }
        public static int jumpmode { get { return Menu["jumpmode"].Cast<Slider>().CurrentValue; } }
        public static int SafetyMinHealth { get { return Menu["Safety.MinHealth"].Cast<Slider>().CurrentValue; } }
        public static int SafetyRatio { get { return Menu["Safety.Ratio"].Cast<Slider>().CurrentValue; } }

        public static bool SafetyOverride { get { return Menu["Safety.Override"].Cast<KeyBind>().CurrentValue; } }
        #endregion

        private static void OnLoad(EventArgs args)
        {
            if (myHero.Hero != Champion.Khazix)
            {
                return;
            }

            Menu = MainMenu.AddMenu("Seph KhaZix", "khazix");
            Menu.AddLabel("Ported from Seph's KhaZix - Berb");
            Menu.AddSeparator();

            Menu.AddGroupLabel("Combo");
            Menu.Add("UseQCombo", new CheckBox("Use Q"));
            Menu.Add("UseWCombo", new CheckBox("Use W"));
            Menu.Add("UseECombo", new CheckBox("Use E"));
            Menu.Add("UseRCombo", new CheckBox("Use R"));
            Menu.AddSeparator();
            Menu.Add("UseEGapclose", new CheckBox("Use E To Gapclose for Q"));
            Menu.Add("UseEGapcloseW", new CheckBox("Use E To Gapclose For W"));
            Menu.Add("UseRGapcloseW", new CheckBox("Use R after long gapcloses"));
            Menu.AddSeparator();

            Menu.AddGroupLabel("Double Jump");
            Menu.Add("djumpenabled", new CheckBox("Enabled"));
            Menu.Add("delayQ", new Slider("Delay on Q", 1, 1, 5));
            Menu.Add("JEDelay", new Slider("Delay on jumps", 250, 250, 500));
            Menu.Add("jumpmode", new Slider("1 : Default (jumps towards your nexus) | 2 : Custom - Settings below", 1, 1, 2));
            Menu.AddSeparator();
            Menu.Add("save", new CheckBox("Save Double Jump Abilities"));
            Menu.Add("noauto", new CheckBox("Wait for Q instead of autos"));
            Menu.Add("jcursor", new CheckBox("Jump to Cursor (true) or false for script logic"));
            Menu.Add("jcursor2", new CheckBox("Second Jump to Cursor (true) or false for script logic"));
            Menu.AddSeparator();

            Menu.AddGroupLabel("Safety");
            Menu.Add("Safety.Enabled", new CheckBox("Enable Safety Checks"));
            Menu.Add("Safety.CountCheck", new CheckBox("Min Ally ratio to Enemies to jump"));
            Menu.Add("Safety.TowerJump", new CheckBox("Avoid Tower Diving"));
            Menu.Add("Safety.Override", new KeyBind("Safety Override Key", false, KeyBind.BindTypes.HoldActive, 'T'));
            Menu.Add("Safety.MinHealth", new Slider("Healthy %", 15, 0, 100));
            Menu.Add("Safety.Ratio", new Slider("Ally:Enemy Ratio (/5)", 1, 0, 5));
            Menu.AddSeparator();


            Q = new Spell.Targeted(SpellSlot.Q, 325);
            W = new Spell.Skillshot(SpellSlot.W, 1000, SkillShotType.Linear, 225, 828, 80);
            WE = new Spell.Skillshot(SpellSlot.W, 1000, SkillShotType.Linear, 225, 828, 100);
            E = new Spell.Skillshot(SpellSlot.E, 700, SkillShotType.Circular, 250, 1000, 100);
            R = new Spell.Active(SpellSlot.R, 0);

            foreach (var t in ObjectManager.Get<Obj_AI_Turret>().Where(t => t.IsEnemy))
            {
                EnemyTurretPositions.Add(t.ServerPosition);
            }

            var shop = ObjectManager.Get<Obj_Shop>().FirstOrDefault(o => o.IsAlly);
            if (shop != null)
            {
                NexusPosition = shop.Position;
            }

            HeroList = EntityManager.Heroes.AllHeroes;

            Game.OnTick += OnTick;
            Game.OnTick += DoubleJump;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
        }

        internal static bool PointUnderEnemyTurret(this Vector2 Point)
        {
            var EnemyTurrets = ObjectManager.Get<Obj_AI_Turret>().Any(t => t.IsEnemy && Vector2.Distance(t.Position.To2D(), Point) < 950f);
            return EnemyTurrets != null;
        }

        internal static bool PointUnderEnemyTurret(this Vector3 Point)
        {
            var EnemyTurrets = ObjectManager.Get<Obj_AI_Turret>().Where(t => t.IsEnemy && Vector3.Distance(t.Position, Point) < 900f + myHero.BoundingRadius);
            return EnemyTurrets.Any();
        }

        static internal bool Override
        {
            get
            {
                return SafetyOverride;
            }
        }

        static internal bool ShouldJump(Vector3 position)
        {
            if (!SafetyEnabled || Override)
            {
                return true;
            }
            if (SafetyTowerJump && position.PointUnderEnemyTurret())
            {
                return false;
            }
            else if (SafetyEnabled)
            {
                if (myHero.HealthPercent < SafetyMinHealth)
                {
                    return false;
                }

                if (SafetyCountCheck)
                {
                    var enemies = position.CountEnemiesInRange(400);
                    var allies = position.CountAlliesInRange(400);

                    var ec = enemies;
                    var ac = allies;
                    float setratio = SafetyRatio / 5;


                    if (ec != 0 && !(ac / ec >= setratio))
                    {
                        return false;
                    }
                }
                return true;
            }
            return true;
        }

        static Vector3 GetJumpPoint(AIHeroClient Qtarget, bool firstjump = true)
        {
            if (myHero.ServerPosition.PointUnderEnemyTurret())
            {
                return (Vector3)myHero.ServerPosition.Extend(NexusPosition, E.Range);
            }

            if (jumpmode == 1)
            {
                return (Vector3)myHero.ServerPosition.Extend(NexusPosition, E.Range);
            }

            if (firstjump && jcursor)
            {
                return Game.CursorPos;
            }

            if (!firstjump && jcursor2)
            {
                return Game.CursorPos;
            }

            Vector3 Position = new Vector3();
            var jumptarget = IsHealthy
                  ? HeroList
                      .FirstOrDefault(x => x.IsValidTarget() && !x.IsZombie && x != Qtarget &&
                              Vector3.Distance(myHero.ServerPosition, x.ServerPosition) < E.Range)
                  :
              HeroList
                  .FirstOrDefault(x => x.IsAlly && !x.IsZombie && !x.IsDead && !x.IsMe &&
                          Vector3.Distance(myHero.ServerPosition, x.ServerPosition) < E.Range);

            if (jumptarget != null)
            {
                Position = jumptarget.ServerPosition;
            }
            if (jumptarget == null)
            {
                return (Vector3)myHero.ServerPosition.Extend(NexusPosition, E.Range);
            }
            return Position;
        }

        static internal bool IsHealthy
        {
            get
            {
                return myHero.HealthPercent >= SafetyMinHealth;
            }
        }

        static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (args.Target.Type == GameObjectType.obj_AI_Base && djumpenabled && noauto)
            {
                if (args.Target.Health < GetQDamage((AIHeroClient)args.Target) &&myHero.ManaPercent > 15)
                {
                    args.Process = false;
                }
            }
        }

        static internal void EvolutionCheck()
        {
            if (!EvolvedQ && myHero.HasBuff("khazixqevo"))
            {
                Q = new Spell.Targeted(SpellSlot.Q, 325);
                EvolvedQ = true;
            }
            if (!EvolvedW && myHero.HasBuff("khazixwevo"))
            {
                EvolvedW = true;
                W = new Spell.Skillshot(SpellSlot.W, 1000, SkillShotType.Linear, 225, 828, 100);
            }

            if (!EvolvedE && myHero.HasBuff("khazixeevo"))
            {
                E = new Spell.Skillshot(SpellSlot.E, 1000, SkillShotType.Circular, 250, 1000, 100);
                EvolvedE = true;
            }
        }

        static internal List<AIHeroClient> GetIsolatedTargets()
        {
            var validtargets = HeroList.Where(h => h.IsValidTarget(E.Range) && h.IsIsolated()).ToList();
            return validtargets;
        }

        static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!EvolvedE || !save)
            {
                return;
            }

            if (args.Slot.Equals(SpellSlot.Q) && args.Target is AIHeroClient && djumpenabled)
            {
                var target = args.Target as AIHeroClient;
                var qdmg = GetQDamage(target);
                var dmg = (myHero.GetAutoAttackDamage(target) * 2) + qdmg;
                if (target.Health < dmg && target.Health > qdmg)
                {
                    args.Process = false;
                }
            }
        }

        internal static bool IsIsolated(this Obj_AI_Base target)
        {
            return !ObjectManager.Get<Obj_AI_Base>().Any(x => x.NetworkId != target.NetworkId && x.Team == target.Team && x.Distance(target) <= 500 && (x.Type == GameObjectType.obj_AI_Base || x.Type == GameObjectType.obj_AI_Minion || x.Type == GameObjectType.obj_AI_Turret));
        }

        internal static double GetQDamage(Obj_AI_Base target)
        {
            if (Q.Range < 326)
            {
                return 0.984 * myHero.GetSpellDamage(target, SpellSlot.Q, target.IsIsolated() ? DamageLibrary.SpellStages.Empowered : DamageLibrary.SpellStages.Default);
            }
            if (Q.Range > 325)
            {
                var isolated = target.IsIsolated();
                if (isolated)
                {
                    return 0.984 * myHero.GetSpellDamage(target, SpellSlot.Q, DamageLibrary.SpellStages.Empowered);
                }
                return myHero.GetSpellDamage(target, SpellSlot.Q, DamageLibrary.SpellStages.Default);
            }
            return 0;
        }

        private static void DoubleJump(EventArgs args)
        {
            if (!E.IsReady() || !EvolvedE || !djumpenabled || myHero.IsDead || myHero.IsRecalling())
            {
                return;
            }

            if (Q.IsReady() && E.IsReady())
            {
                var Targets = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                if (Targets == null)
                {
                    return;
                }

                var CheckQKillable = Vector3.Distance(myHero.ServerPosition, Targets.ServerPosition) < Q.Range - 25 && myHero.GetSpellDamage(Targets, SpellSlot.Q) > Targets.Health;

                if (CheckQKillable)
                {
                    Console.WriteLine("TRYING");
                    Jumping = true;
                    Jumppoint1 = GetJumpPoint(Targets);
                    Core.DelayAction(delegate { E.Cast(Jumppoint1); }, 0);
                    Core.DelayAction(delegate { Q.Cast(Targets); }, delayQ);
                    Console.WriteLine("TRYING Q");
                    var oldpos = myHero.ServerPosition;
                    Core.DelayAction(delegate
                    {
                        if (E.IsReady())
                        {
                            Jumppoint2 = GetJumpPoint(Targets, false);
                            E.Cast(Jumppoint2);
                        }
                        Jumping = false;
                    }, JEDelay + Game.Ping);
                }
            }
        }

        static int CountHits(Vector2 position, List<Vector2> points, List<int> hitBoxes)
        {
            int result = 0;

            Vector2 startPoint = myHero.ServerPosition.To2D();
            Vector2 originalDirection = W.Range * (position - startPoint).Normalized();
            Vector2 originalEndPoint = startPoint + originalDirection;

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 point = points[i];

                for (int k = 0; k < 3; k++)
                {
                    var endPoint = new Vector2();
                    if (k == 0)
                        endPoint = originalEndPoint;
                    if (k == 1)
                        endPoint = startPoint + originalDirection.Rotated(Wangle);
                    if (k == 2)
                        endPoint = startPoint + originalDirection.Rotated(-Wangle);

                    if (point.Distance(startPoint, endPoint, true, true) <
                        (W.Width + hitBoxes[i]) * (W.Width + hitBoxes[i]))
                    {
                        result++;
                        break;
                    }
                }
            }
            return result;
        }

        static void Combo()
        {
            var target = TargetSelector.GetTarget(W.Range, DamageType.Physical);

            if ((target != null))
            {
                var dist = myHero.Distance(target);

                // Normal abilities
                if (Q.IsReady() && dist <= Q.Range && UseQCombo)
                {
                    Q.Cast(target);
                }

                if (W.IsReady() && !EvolvedW && dist <= W.Range && UseWCombo)
                {
                    var pred = W.GetPrediction(target);
                    if (pred.HitChance >= HitChance.High)
                    {
                        W.Cast(pred.CastPosition);
                    }
                }

                if (E.IsReady() && dist <= E.Range && UseECombo && dist > Q.Range + (0.7 * myHero.MoveSpeed))
                {
                    PredictionResult pred = E.GetPrediction(target);
                    if (target.IsValid && !target.IsDead && ShouldJump(pred.CastPosition))
                    {
                        E.Cast(pred.CastPosition);
                    }
                }

                // Use EQ AND EW Synergy
                if ((dist <= E.Range + Q.Range + (0.7 * myHero.MoveSpeed) && dist > Q.Range && E.IsReady() && UseEGapclose) || (dist <= E.Range + W.Range && dist > Q.Range && E.IsReady() && W.IsReady() && UseEGapcloseW))
                {
                    PredictionResult pred = E.GetPrediction(target);
                    if (target.IsValid && !target.IsDead && ShouldJump(pred.CastPosition))
                    {
                        E.Cast(pred.CastPosition);
                    }
                    if (UseRGapcloseW && R.IsReady())
                    {
                        //R.CastOnUnit(Khazix);
                        R.Cast();
                    }
                }


                // Ult Usage
                if (R.IsReady() && !Q.IsReady() && !W.IsReady() && !E.IsReady() && UseRCombo)
                {
                    R.Cast();
                }
                // Evolved

                if (W.IsReady() && EvolvedW && dist <= WE.Range && UseWCombo)
                {
                    PredictionResult pred = WE.GetPrediction(target);
                    if (pred.HitChance >= HitChance.High)
                    {
                        CastWE(target, pred.UnitPosition.To2D(), 0, HitChance.High);
                    }
                    if (pred.HitChance >= HitChance.Collision)
                    {
                        List<Obj_AI_Base> PCollision = pred.CollisionObjects.ToList();
                        var x = PCollision.Where(PredCollisionChar => PredCollisionChar.Distance(target) <= 30).FirstOrDefault();
                        if (x != null)
                        {
                            W.Cast(x.Position);
                        }
                    }
                }

                if (dist <= E.Range + (0.7 * myHero.MoveSpeed) && dist > Q.Range && UseECombo && E.IsReady())
                {
                    PredictionResult pred = E.GetPrediction(target);
                    if (target.IsValid && !target.IsDead && ShouldJump(pred.CastPosition))
                    {
                        E.Cast(pred.CastPosition);
                    }
                }

                //if (Config.GetBool("UseItems"))
                //{
                    //UseItems(target);
                //}
            }
        }

        static internal void CastWE(Obj_AI_Base unit, Vector2 unitPosition, int minTargets = 0, HitChance hc = HitChance.Medium)
        {
            var points = new List<Vector2>();
            var hitBoxes = new List<int>();

            Vector2 startPoint = myHero.ServerPosition.To2D();
            Vector2 originalDirection = W.Range * (unitPosition - startPoint).Normalized();

            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
            {
                if (enemy.IsValidTarget() && enemy.NetworkId != unit.NetworkId)
                {
                    PredictionResult pos = W.GetPrediction(enemy);
                    if (pos.HitChance >= HitChance.Medium)
                    {
                        points.Add(pos.UnitPosition.To2D());
                        hitBoxes.Add((int)enemy.BoundingRadius + 275);
                    }
                }
            }

            var posiblePositions = new List<Vector2>();

            for (int i = 0; i < 3; i++)
            {
                if (i == 0)
                    posiblePositions.Add(unitPosition + originalDirection.Rotated(0));
                if (i == 1)
                    posiblePositions.Add(startPoint + originalDirection.Rotated(Wangle));
                if (i == 2)
                    posiblePositions.Add(startPoint + originalDirection.Rotated(-Wangle));
            }


            if (startPoint.Distance(unitPosition) < 900)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 pos = posiblePositions[i];
                    Vector2 direction = (pos - startPoint).Normalized().Perpendicular();
                    float k = (2 / 3 * (unit.BoundingRadius + W.Width));
                    posiblePositions.Add(startPoint - k * direction);
                    posiblePositions.Add(startPoint + k * direction);
                }
            }

            var bestPosition = new Vector2();
            int bestHit = -1;

            foreach (Vector2 position in posiblePositions)
            {
                int hits = CountHits(position, points, hitBoxes);
                if (hits > bestHit)
                {
                    bestPosition = position;
                    bestHit = hits;
                }
            }

            if (bestHit <= minTargets)
                return;

            W.Cast(bestPosition.To3D());
        }

        private static void OnTick(EventArgs args)
        {
            if (myHero.IsDead || myHero.IsRecalling())
            {
                return;
            }

            EvolutionCheck();

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
        }
    }
}
