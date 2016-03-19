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
using LeagueSharp.Common;
using LeagueSharp.Common.Data;

namespace PortAIO.Champion.Alistar
{
    class Program
    {
        #region Properties

        /// <summary>
        ///     Gets or sets the E spell
        /// </summary>
        /// <value>
        ///     The E spell
        /// </value>
        private static LeagueSharp.Common.Spell E { get; set; }

        /// <summary>
        ///     Gets or sets the menu
        /// </summary>
        /// <value>
        ///     The menu
        /// </value>
        private static Menu Menu { get; set; }
        private static Menu comboMenu { get; set; }
        private static Menu flashMenu { get; set; }
        private static Menu healMenu { get; set; }
        private static Menu interrupterMenu { get; set; }
        private static Menu miscellaneousMenu { get; set; }
        /// <summary>
        ///     Gets the player.
        /// </summary>
        /// <value>
        ///     The player.
        /// </value>
        private static AIHeroClient Player
        {
            get
            {
                return ObjectManager.Player;
            }
        }

        /// <summary>
        ///     Gets or sets the Q spell
        /// </summary>
        /// <value>
        ///     The Q spell
        /// </value>
        private static LeagueSharp.Common.Spell Q { get; set; }

        /// <summary>
        ///     Gets or sets the R spell.
        /// </summary>
        /// <value>
        ///     The R spell
        /// </value>
        private static LeagueSharp.Common.Spell R { get; set; }

        /// <summary>
        ///     Gets or sets the W spell
        /// </summary>
        /// <value>
        ///     The W spell
        /// </value>
        private static LeagueSharp.Common.Spell W { get; set; }

        /// <summary>
        ///     Gets or sets the slot.
        /// </summary>
        /// <value>
        ///     The IgniteSpell
        /// </value>
        public static LeagueSharp.Common.Spell IgniteSpell { get; set; }

        /// <summary>
        ///     FlashSlot
        /// </summary>
        public static SpellSlot FlashSlot;


        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Fired when the game loads.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        public static void OnGameLoad()
        {
            try
            {
                if (Player.ChampionName != "Alistar")
                {
                    return;
                }

                var igniteSlot = Player.GetSpell(SpellSlot.Summoner1).Name.ToLower().Contains("summonerdot") ? SpellSlot.Summoner1 : Player.GetSpell(SpellSlot.Summoner2).Name.ToLower().Contains("summonerdot") ? SpellSlot.Summoner2 : SpellSlot.Unknown;

                if (igniteSlot != SpellSlot.Unknown)
                {
                    IgniteSpell = new LeagueSharp.Common.Spell(igniteSlot, 600f);
                }

                FlashSlot = Player.GetSpellSlot("summonerflash");

                Q = new LeagueSharp.Common.Spell(SpellSlot.Q, 365f);
                W = new LeagueSharp.Common.Spell(SpellSlot.W, 650f);
                E = new LeagueSharp.Common.Spell(SpellSlot.E, 575f);
                R = new LeagueSharp.Common.Spell(SpellSlot.R);

                GenerateMenu();
                Game.OnUpdate += OnUpdate;
                Drawing.OnDraw += OnDraw;
                AttackableUnit.OnDamage += AttackableUnit_OnDamage;
                Interrupter2.OnInterruptableTarget += OnInterruptableTarget;
                AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        #endregion

        #region Methods

        public static bool getCheckBoxItem(Menu m, string item)
        {
            return m[item].Cast<CheckBox>().CurrentValue;
        }

        public static int getSliderItem(Menu m, string item)
        {
            return m[item].Cast<Slider>().CurrentValue;
        }

        public static bool getKeyBindItem(Menu m, string item)
        {
            return m[item].Cast<KeyBind>().CurrentValue;
        }

        /// <summary>
        ///     Creates the menu
        /// </summary>
        /// <value>
        ///     Creates the menu
        /// </value>
        private static void GenerateMenu()
        {
            try
            {
                Menu = MainMenu.AddMenu("ElAlistar", "ElAlistar");

                comboMenu = Menu.AddSubMenu("Combo Settings", "Combo");
                comboMenu.Add("ElAlistar.Combo.Q", new CheckBox("Use Q"));
                comboMenu.Add("ElAlistar.Combo.W", new CheckBox("Use W"));
                comboMenu.Add("ElAlistar.Combo.R", new CheckBox("Use R"));
                comboMenu.Add("ElAlistar.Combo.RHeal.HP", new Slider("R on Health percentage", 60, 0, 100));
                comboMenu.Add("ElAlistar.Combo.RHeal.Damage", new Slider("R on damage dealt %", 60, 0, 100));


                flashMenu = Menu.AddSubMenu("Flash Settings", "Flash");
                flashMenu.Add("ElAlistar.Flash.Click", new CheckBox("Left Click [on] TS [off]"));
                flashMenu.Add("ElAlistar.Combo.FlashQ", new KeyBind("Flash Q", false, KeyBind.BindTypes.HoldActive, 'T'));


                healMenu = Menu.AddSubMenu("Heal Settings", "Heal");
                healMenu.Add("ElAlistar.Heal.E", new CheckBox("Use heal"));
                healMenu.Add("Heal.HP", new Slider("Health percentage", 80, 0, 100));
                healMenu.Add("Heal.Damage", new Slider("Heal on damage dealt %", 80, 0, 100));
                healMenu.Add("ElAlistar.Heal.Mana", new Slider("Minimum mana", 20, 0, 100));
                healMenu.AddSeparator();
                foreach (var x in ObjectManager.Get<AIHeroClient>().Where(x => x.IsAlly))
                {
                    healMenu.Add("healon" + x.ChampionName, new CheckBox("Use for " + x.ChampionName));
                }


                interrupterMenu = Menu.AddSubMenu("Interrupter Settings", "Interrupter");
                interrupterMenu.Add("ElAlistar.Interrupter.Q", new CheckBox("Use Q"));
                interrupterMenu.Add("ElAlistar.Interrupter.W", new CheckBox("Use W"));
                interrupterMenu.Add("ElAlistar.GapCloser", new CheckBox("Anti gapcloser"));

                miscellaneousMenu = Menu.AddSubMenu("Miscellaneous", "Misc");
                miscellaneousMenu.Add("ElAlistar.Ignite", new CheckBox("Use Ignite"));
                miscellaneousMenu.Add("ElAlistar.Drawings.W", new CheckBox("Draw W range"));

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        /// <summary>
        ///     Called when the game draws itself.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        private static void OnDraw(EventArgs args)
        {
            try
            {
                if (getCheckBoxItem(miscellaneousMenu, "ElAlistar.Drawings.W"))
                {
                    if (W.Level > 0)
                    {
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, Color.DeepSkyBlue);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }


        /// <summary>
        ///     The ignite killsteal logic
        /// </summary>
        private static void HandleIgnite()
        {
            try
            {
                var kSableEnemy = HeroManager.Enemies.FirstOrDefault(hero => hero.IsValidTarget(550) && ShieldCheck(hero) && !hero.HasBuff("summonerdot") && !hero.IsZombie && Player.GetSummonerSpellDamage(hero, DamageLibrary.SummonerSpells.Ignite) >= hero.Health);

                if (kSableEnemy != null && IgniteSpell.Slot != SpellSlot.Unknown)
                {
                    Player.Spellbook.CastSpell(IgniteSpell.Slot, kSableEnemy);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        /// <summary>
        ///     The shield checker
        /// </summary>
        private static bool ShieldCheck(Obj_AI_Base hero)
        {
            try
            {
                return !hero.HasBuff("summonerbarrier") || !hero.HasBuff("BlackShield")
                       || !hero.HasBuff("SivirShield") || !hero.HasBuff("BansheesVeil")
                       || !hero.HasBuff("ShroudofDarkness");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            return false;
        }


        /// <summary>
        ///     Returns the mana
        /// </summary>
        private static bool HasEnoughMana()
        {
            return Player.Mana > Player.Spellbook.GetSpell(SpellSlot.Q).SData.Mana + Player.Spellbook.GetSpell(SpellSlot.W).SData.Mana;
        }

        /// <summary>
        ///     Combo logic
        /// </summary>
        private static void OnCombo()
        {
            try
            {
                var target = TargetSelector.GetTarget(W.Range, DamageType.Magical);
                if (target == null)
                {
                    return;
                }
                if (getCheckBoxItem(comboMenu, "ElAlistar.Combo.Q") && getCheckBoxItem(comboMenu, "ElAlistar.Combo.W") && Q.IsReady() && W.IsReady())
                {
                    if (target.IsValidTarget(W.Range) && HasEnoughMana())
                    {
                        if (target.IsValidTarget(Q.Range))
                        {
                            Q.Cast();
                            return;
                        }

                        if (W.Cast(target).IsCasted())
                        {
                            var comboTime = Math.Max(0, Player.Distance(target) - 365) / 1.2f - 25;
                            LeagueSharp.Common.Utility.DelayAction.Add((int)comboTime, () => Q.Cast());
                        }
                    }
                }

                if (getCheckBoxItem(comboMenu, "ElAlistar.Combo.Q") && target.IsValidTarget(Q.Range))
                {
                    Q.Cast();
                }

                if (getCheckBoxItem(comboMenu, "ElAlistar.Combo.W"))
                {
                    if (target.IsValidTarget(W.Range) && W.GetDamage(target) > target.Health)
                    {
                        W.Cast(target);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private static void OnInterruptableTarget(
            AIHeroClient sender,
            Interrupter2.InterruptableTargetEventArgs args)
        {
            if (args.DangerLevel != Interrupter2.DangerLevel.High || sender.Distance(Player) > W.Range)
            {
                return;
            }

            if (sender.IsValidTarget(Q.Range) && Q.IsReady() && getCheckBoxItem(interrupterMenu, "ElAlistar.Interrupter.Q"))
            {
                Q.Cast();
            }

            if (sender.IsValidTarget(W.Range) && W.IsReady() && getCheckBoxItem(interrupterMenu, "ElAlistar.Interrupter.W"))
            {
                W.Cast(sender);
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (getCheckBoxItem(interrupterMenu, "ElAlistar.GapCloser"))
            {
                if (Q.IsReady()
                    && gapcloser.Sender.Distance(Player) < Q.Range)
                {
                    Q.Cast();
                }

                if (W.IsReady() && gapcloser.Sender.Distance(Player) < W.Range)
                {
                    W.Cast(gapcloser.Sender);
                }
            }
        }

        private static void AttackableUnit_OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            var obj = ObjectManager.GetUnitByNetworkId<GameObject>((uint)args.Target.NetworkId);

            if (obj.Type != GameObjectType.AIHeroClient)
            {
                return;
            }

            var hero = (AIHeroClient)obj;

            if (hero.IsEnemy)
            {
                return;
            }

            if (getCheckBoxItem(comboMenu, "ElAlistar.Combo.R"))
            {
                if (ObjectManager.Get<AIHeroClient>().Any(x => x.IsAlly && x.IsMe && !x.IsDead && ((int)(args.Damage / x.MaxHealth * 100) > getSliderItem(comboMenu, "ElAlistar.Combo.RHeal.Damage") || x.HealthPercent < getSliderItem(comboMenu, "ElAlistar.Combo.RHeal.HP") && x.CountEnemiesInRange(1000) >= 1)))
                {
                    R.Cast();
                }
            }

            if (getCheckBoxItem(healMenu, "ElAlistar.Heal.E") && Player.ManaPercent > getSliderItem(healMenu, "ElAlistar.Heal.Mana"))
            {
                if (ObjectManager.Get<AIHeroClient>().Any(x => x.IsAlly && !x.IsDead && getCheckBoxItem(healMenu, string.Format("healon{0}", x.ChampionName)) && ((int)(args.Damage / x.MaxHealth * 100) > getSliderItem(healMenu, "Heal.Damage") || x.HealthPercent < getSliderItem(healMenu, "Heal.HP")) && x.Distance(Player) < E.Range && x.CountEnemiesInRange(1000) >= 1))
                {
                    E.Cast();
                }
            }
        }

        /// <summary>
        ///     Called when the game updates
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        private static void OnUpdate(EventArgs args)
        {
            try
            {
                if (Player.IsDead || Player.IsRecalling() || Player.InFountain())
                {
                    return;
                }

                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    OnCombo();
                }

                if (getCheckBoxItem(miscellaneousMenu, "ElAlistar.Ignite"))
                {
                    HandleIgnite();
                }

                if (getKeyBindItem(flashMenu, "ElAlistar.Combo.FlashQ") && Q.IsReady())
                {
                    EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

                    var target = getCheckBoxItem(flashMenu, "ElAlistar.Flash.Click") ? TargetSelector.SelectedTarget : TargetSelector.GetTarget(W.Range, DamageType.Magical);

                    if (!target.IsValidTarget(W.Range))
                    {
                        return;
                    }

                    Player.Spellbook.CastSpell(FlashSlot, target.ServerPosition);
                    LeagueSharp.Common.Utility.DelayAction.Add(50, () => Q.Cast());
                }

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        #endregion
    }
}
