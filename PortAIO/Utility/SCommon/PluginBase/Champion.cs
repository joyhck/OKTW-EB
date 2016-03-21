using System;
using System.Runtime.CompilerServices;
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
using LeagueSharp.Common;
using LeagueSharp.Common.Data;
using SCommon.Evade;
using SPrediction;
using SharpDX;
using SharpDX.Direct3D9;
//typedefs
using Prediction = SPrediction.Prediction;
using Geometry = SCommon.Maths.Geometry;
using Color = System.Drawing.Color;
using TargetSelector = SCommon.TS.TargetSelector;

namespace SCommon.PluginBase
{
    public abstract class Champion : IChampion
    {
        public const int Q = 0, W = 1, E = 2, R = 3;

        public static Menu ConfigMenu, DrawingMenu;
        public static LeagueSharp.Common.Spell[] Spells = new LeagueSharp.Common.Spell[4];
        public Font Text;

        public delegate void dVoidDelegate();
        public dVoidDelegate OnUpdate, OnDraw, OnCombo, OnHarass, OnLaneClear, OnLastHit;

        /// <summary>
        /// Champion constructor
        /// </summary>
        /// <param name="szChampName">The champion name.</param>
        /// <param name="szMenuName">The menu name.</param>
        /// <param name="enableRangeDrawings">if <c>true</c>, enables the spell range drawings</param>
        /// <param name="enableEvader">if <c>true</c>, enables the spell evader if the champion is supported</param>
        public Champion(string szChampName, string szMenuName, bool enableRangeDrawings = true, bool enableEvader = true)
        {
            Text = new Font(Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Malgun Gothic",
                    Height = 15,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearTypeNatural
                });

            TargetSelector.Initialize(ConfigMenu);

            SetSpells();

            if (enableEvader)
            {
                Menu evaderMenu = null;
                Evader evader;
                switch (szChampName.ToLower())
                {
                    case "ezreal":
                        evader = new Evader(out evaderMenu, Database.EvadeMethods.Blink, Spells[E]);
                        break;
                    case "sivir":
                    case "morgana":
                        evader = new Evader(out evaderMenu, Database.EvadeMethods.SpellShield, Spells[E]);
                        break;
                    case "fizz":
                        evader = new Evader(out evaderMenu, Database.EvadeMethods.Dash, Spells[E]);
                        break;
                    case "lissandra":
                        evader = new Evader(out evaderMenu, Database.EvadeMethods.Invulnerability, Spells[R]);
                        break;
                    case "nocturne":
                        evader = new Evader(out evaderMenu, Database.EvadeMethods.SpellShield, Spells[W]);
                        break;
                    case "vladimir":
                        evader = new Evader(out evaderMenu, Database.EvadeMethods.Invulnerability, Spells[W]);
                        break;
                    case "graves":
                    case "gnar":
                    case "lucian":
                    case "riven":
                    case "shen":
                        evader = new Evader(out evaderMenu, Database.EvadeMethods.Dash, Spells[E]);
                        break;
                    case "zed":
                    case "leblanc":
                    case "corki":
                        evader = new Evader(out evaderMenu, Database.EvadeMethods.Dash, Spells[W]);
                        break;
                    case "vayne":
                        evader = new Evader(out evaderMenu, Database.EvadeMethods.Dash, Spells[Q]);
                        break;
                }
            }

            #region Events
            Game.OnUpdate += this.Game_OnUpdate;
            Drawing.OnDraw += this.Drawing_OnDraw;
            Orbwalker.OnPreAttack += this.OrbwalkingEvents_BeforeAttack;
            Orbwalker.OnPostAttack += this.OrbwalkingEvents_AfterAttack;
            AntiGapcloser.OnEnemyGapcloser += this.AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += this.Interrupter_OnPossibleToInterrupt;
            Obj_AI_Base.OnBuffGain += this.Obj_AI_Base_OnBuffAdd;
            Obj_AI_Base.OnProcessSpellCast += this.Obj_AI_Base_OnProcessSpellCast;
            CustomEvents.Unit.OnDash += this.Unit_OnDash;
            TargetedSpellDetector.OnDetected += this.TargetedSpellDetector_OnDetected;
            #endregion

            Prediction.Initialize();
        }

        /// <summary>
        /// Sets spell values of the hero.
        /// </summary>
        public virtual void SetSpells()
        {
            Spells[Q] = new LeagueSharp.Common.Spell(SpellSlot.Q);
            Spells[W] = new LeagueSharp.Common.Spell(SpellSlot.W);
            Spells[E] = new LeagueSharp.Common.Spell(SpellSlot.E);
            Spells[R] = new LeagueSharp.Common.Spell(SpellSlot.R);
        }

        public virtual void Game_OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsRecalling() || args == null)
                return;

            if (OnUpdate != null)
                OnUpdate();

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)) {
                if (OnCombo != null)
                    OnCombo();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                if (OnHarass != null)
                    OnHarass();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                if (OnLaneClear != null)
                    OnLaneClear();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
                if (OnLastHit != null)
                    OnLastHit();
            }
        }

        public virtual void Drawing_OnDraw(EventArgs args)
        {
            if (OnDraw != null)
                OnDraw();

            //foreach (MenuItem it in DrawingMenu.Items)
            //{
            //Circle c = it.GetValue<Circle>();
            //if (c.Active)
            //Render.Circle.DrawCircle(ObjectManager.Player.Position, c.Radius, c.Color, 2);
            //}
        }

        /// <summary>
        /// The BeforeAttack event which called by orbwalker.
        /// </summary>
        /// <param name="args">The args.</param>
        private void OrbwalkingEvents_BeforeAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            //
        }

        /// <summary>
        /// The AfterAttack event which called by orbwalker.
        /// </summary>
        /// <param name="args">The args.</param>
        private void OrbwalkingEvents_AfterAttack(AttackableUnit target, System.EventArgs args)
        {
            //
        }

        /// <summary>
        /// The AntiGapCloser event.
        /// </summary>
        /// <param name="gapcloser">The gapcloser.</param>
        protected virtual void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            //
        }

        /// <summary>
        /// The OnPossibleToInterrupt event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The args.</param>
        protected virtual void Interrupter_OnPossibleToInterrupt(AIHeroClient sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            //
        }

        /// <summary>
        /// The OnBuffAdd event
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The args.</param>
        protected virtual void Obj_AI_Base_OnBuffAdd(Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs args)
        {
            //
        }

        /// <summary>
        /// The OnProcessSpellCast event
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The args.</param>
        protected virtual void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            //
        }

        /// <summary>
        /// The OnDash event
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The args.</param>
        protected virtual void Unit_OnDash(Obj_AI_Base sender, LeagueSharp.Common.Dash.DashItem args)
        {
            //
        }

        protected virtual void TargetedSpellDetector_OnDetected(DetectedTargetedSpellArgs data)
        {
            //
        }

        /// <summary>
        /// Checks if combo is ready
        /// </summary>
        /// <returns>true if combo is ready</returns>
        public bool ComboReady()
        {
            return Spells[Q].IsReady() && Spells[W].IsReady() && Spells[E].IsReady() && Spells[R].IsReady();
        }

        #region Damage Calculation Funcitons
        /// <summary>
        /// Calculates combo damage to given target
        /// </summary>
        /// <param name="target">Target</param>
        /// <param name="aacount">Auto Attack Count</param>
        /// <returns>Combo damage</returns>
        public double CalculateComboDamage(AIHeroClient target, int aacount = 2)
        {
            return CalculateSpellDamage(target) + CalculateSummonersDamage(target) + CalculateItemsDamage(target) + CalculateAADamage(target, aacount);
        }

        /// <summary>
        /// Calculates Spell Q damage to given target
        /// </summary>
        /// <param name="target">Target</param>
        /// <returns>Spell Q Damage</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual double CalculateDamageQ(AIHeroClient target)
        {
            if (Spells[Q].IsReady())
                return ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q);

            return 0.0;
        }

        /// <summary>
        /// Calculates Spell W damage to given target
        /// </summary>
        /// <param name="target">Target</param>
        /// <returns>Spell W Damage</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual double CalculateDamageW(AIHeroClient target)
        {
            if (Spells[W].IsReady())
                return ObjectManager.Player.GetSpellDamage(target, SpellSlot.W);

            return 0.0;
        }

        /// <summary>
        /// Calculates Spell E damage to given target
        /// </summary>
        /// <param name="target">Target</param>
        /// <returns>Spell E Damage</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual double CalculateDamageE(AIHeroClient target)
        {
            if (Spells[E].IsReady())
                return ObjectManager.Player.GetSpellDamage(target, SpellSlot.E);

            return 0.0;
        }

        /// <summary>
        /// Calculates Spell R damage to given target
        /// </summary>
        /// <param name="target">Target</param>
        /// <returns>Spell R Damage</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual double CalculateDamageR(AIHeroClient target)
        {
            if (Spells[R].IsReady())
                return ObjectManager.Player.GetSpellDamage(target, SpellSlot.R);

            return 0.0;
        }

        /// <summary>
        /// Calculates all spell's damage to given target
        /// </summary>
        /// <param name="target">Target</param>
        /// <returns>All spell's damage</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double CalculateSpellDamage(AIHeroClient target)
        {
            return CalculateDamageQ(target) + CalculateDamageW(target) + CalculateDamageE(target) + CalculateDamageR(target);
        }

        /// <summary>
        /// Calculates summoner spell damages to given target
        /// </summary>
        /// <param name="target">Target</param>
        /// <returns>Summoner spell damage</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double CalculateSummonersDamage(AIHeroClient target)
        {
            var ignite = ObjectManager.Player.GetSpellSlot("summonerdot");
            if (ignite != SpellSlot.Unknown && ObjectManager.Player.Spellbook.CanUseSpell(ignite) == SpellState.Ready && ObjectManager.Player.Distance(target, false) < 550)
                return ObjectManager.Player.GetSummonerSpellDamage(target, LeagueSharp.Common.Damage.SummonerSpell.Ignite); //ignite

            return 0.0;
        }

        /// <summary>
        /// Calculates Item's active damages to given target
        /// </summary>
        /// <param name="target">Target</param>
        /// <returns>Item's damage</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual double CalculateItemsDamage(AIHeroClient target)
        {
            double dmg = 0.0;

            if (Items.CanUseItem(3144) && ObjectManager.Player.Distance(target, false) < 550)
                dmg += ObjectManager.Player.GetItemDamage(target, LeagueSharp.Common.Damage.DamageItems.Bilgewater); //bilgewater cutlass

            if (Items.CanUseItem(3153) && ObjectManager.Player.Distance(target, false) < 550)
                dmg += ObjectManager.Player.GetItemDamage(target, LeagueSharp.Common.Damage.DamageItems.Botrk); //botrk

            if (Items.HasItem(3057))
                dmg += ObjectManager.Player.CalcDamage(target, DamageType.Magical, ObjectManager.Player.BaseAttackDamage); //sheen

            if (Items.HasItem(3100))
                dmg += ObjectManager.Player.CalcDamage(target, DamageType.Magical, (0.75 * ObjectManager.Player.BaseAttackDamage) + (0.50 * ObjectManager.Player.FlatMagicDamageMod)); //lich bane

            if (Items.HasItem(3285))
                dmg += ObjectManager.Player.CalcDamage(target, DamageType.Magical, 100 + (0.1 * ObjectManager.Player.FlatMagicDamageMod)); //luden

            return dmg;

        }

        /// <summary>
        /// Calculates Auto Attack damage to given target
        /// </summary>
        /// <param name="target">Targetparam>
        /// <param name="aacount">Auto Attack count</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual double CalculateAADamage(AIHeroClient target, int aacount = 2)
        {
            return Damage.AutoAttack.GetDamage(target) * aacount;
        }
        #endregion
    }
}
