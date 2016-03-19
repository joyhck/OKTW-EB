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
using PortAIO.Utility.DZAhri;

namespace PortAIO.Champion.Ahri
{
    class Program
    {
        public static Menu Menu, comboMenu, harassMenu, miscMenu, farmMenu, drawMenu;
        public static readonly Dictionary<SpellSlot, LeagueSharp.Common.Spell> _spells = new Dictionary<SpellSlot, LeagueSharp.Common.Spell>
        {
            { SpellSlot.Q, new LeagueSharp.Common.Spell(SpellSlot.Q, 925f) },
            { SpellSlot.W, new LeagueSharp.Common.Spell(SpellSlot.W, 700f) },
            { SpellSlot.E, new LeagueSharp.Common.Spell(SpellSlot.E, 875f) },
            { SpellSlot.R, new LeagueSharp.Common.Spell(SpellSlot.R, 400f) }
        };

        public static void OnLoad()
        {
            if (ObjectManager.Player.ChampionName != "Ahri")
            {
                return;
            }
            SetUpMenu();
            SetUpSpells();
            SetUpEvents();
        }

        #region Modes Menu
        private static void Combo()
        {
            if (ObjectManager.Player.ManaPercent < getSliderItem(comboMenu, "dz191.ahri.combo.mana") || ObjectManager.Player.IsDead)
            {
                return;
            }
            var comboTarget = TargetSelector.GetTarget(_spells[SpellSlot.E].Range, DamageType.Magical);
            var charmedUnit = HeroManager.Enemies.Find(h => h.HasBuffOfType(BuffType.Charm) && h.IsValidTarget(_spells[SpellSlot.Q].Range));
            AIHeroClient target = comboTarget;
            if (charmedUnit != null)
            {
                target = charmedUnit;
            }
            if (target.IsValidTarget())
            {
                switch (getSliderItem(comboMenu, "dz191.ahri.combo.mode"))
                {
                    case 1:
                        if (!target.IsCharmed() && getCheckBoxItem(comboMenu, "dz191.ahri.combo.usee") && _spells[SpellSlot.E].IsReady() && _spells[SpellSlot.Q].IsReady())
                        {
                            _spells[SpellSlot.E].CastIfHitchanceEquals(target, LeagueSharp.Common.HitChance.High);
                        }
                        if (getCheckBoxItem(comboMenu, "dz191.ahri.combo.useq") && _spells[SpellSlot.Q].IsReady() && (!_spells[SpellSlot.E].IsReady() || ObjectManager.Player.ManaPercent <= 25))
                        {
                            _spells[SpellSlot.Q].CastIfHitchanceEquals(target, LeagueSharp.Common.HitChance.High);
                        }
                        if (getCheckBoxItem(comboMenu, "dz191.ahri.combo.usew") && _spells[SpellSlot.W].IsReady() && ObjectManager.Player.Distance(target) <= _spells[SpellSlot.W].Range && (target.IsCharmed() || (_spells[SpellSlot.W].GetDamage(target) + _spells[SpellSlot.Q].GetDamage(target) > target.Health + 25)))
                        {
                            _spells[SpellSlot.W].Cast();
                        }
                        break;
                    case 2:
                        if (!target.IsCharmed() && getCheckBoxItem(comboMenu, "dz191.ahri.combo.usee") && _spells[SpellSlot.E].IsReady() && _spells[SpellSlot.Q].IsReady())
                        {
                            _spells[SpellSlot.E].CastIfHitchanceEquals(target, LeagueSharp.Common.HitChance.High);
                        }
                        if (getCheckBoxItem(comboMenu, "dz191.ahri.combo.useq") && _spells[SpellSlot.Q].IsReady())
                        {
                            _spells[SpellSlot.Q].CastIfHitchanceEquals(target, LeagueSharp.Common.HitChance.High);
                        }
                        if (getCheckBoxItem(comboMenu, "dz191.ahri.combo.usew") && _spells[SpellSlot.W].IsReady() && ObjectManager.Player.Distance(target) <= _spells[SpellSlot.W].Range && ((_spells[SpellSlot.W].GetDamage(target) + _spells[SpellSlot.Q].GetDamage(target) > target.Health + 25)))
                        {
                            _spells[SpellSlot.W].Cast();
                        }
                        break;

                }
                HandleRCombo(target);
            }
        }

        private static void Harass()
        {
            if (ObjectManager.Player.ManaPercent < getSliderItem(harassMenu, "dz191.ahri.harass.mana") || ObjectManager.Player.IsDead)
            {
                return;
            }
            var comboTarget = TargetSelector.GetTarget(_spells[SpellSlot.E].Range, DamageType.Magical);
            var charmedUnit = HeroManager.Enemies.Find(h => h.IsCharmed() && h.IsValidTarget(_spells[SpellSlot.Q].Range));
            AIHeroClient target = comboTarget;
            if (charmedUnit != null)
            {
                target = charmedUnit;
            }
            if (target.IsValidTarget())
            {
                if (!target.IsCharmed() && getCheckBoxItem(harassMenu, "dz191.ahri.harass.usee") && _spells[SpellSlot.E].IsReady() && _spells[SpellSlot.Q].IsReady())
                {
                    _spells[SpellSlot.E].CastIfHitchanceEquals(target, LeagueSharp.Common.HitChance.High);
                }
                if (getCheckBoxItem(harassMenu, "dz191.ahri.harass.useq") && _spells[SpellSlot.Q].IsReady())
                {
                    if (getCheckBoxItem(harassMenu, "dz191.ahri.harass.onlyqcharm") && !target.IsCharmed())
                    {
                        return;
                    }
                    _spells[SpellSlot.Q].CastIfHitchanceEquals(target, LeagueSharp.Common.HitChance.High);
                }
                if (getCheckBoxItem(harassMenu, "dz191.ahri.harass.usew") && _spells[SpellSlot.W].IsReady() && ObjectManager.Player.Distance(target) <= _spells[SpellSlot.W].Range)
                {
                    _spells[SpellSlot.W].Cast();
                }
            }
        }

        private static void LastHit()
        {
            if (ObjectManager.Player.ManaPercent < getSliderItem(farmMenu, "dz191.ahri.farm.mana"))
            {
                return;
            }
            if (getCheckBoxItem(farmMenu, "dz191.ahri.farm.qlh"))
            {
                var minionInQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _spells[SpellSlot.Q].Range).FindAll(m => _spells[SpellSlot.Q].GetDamage(m) >= m.Health).ToList();
                var killableMinions = _spells[SpellSlot.Q].GetLineFarmLocation(minionInQ);
                if (killableMinions.MinionsHit > 0)
                {
                    _spells[SpellSlot.Q].Cast(killableMinions.Position);
                }
            }
        }

        private static void Laneclear()
        {
            if (ObjectManager.Player.ManaPercent < getSliderItem(farmMenu, "dz191.ahri.farm.mana"))
            {
                return;
            }
            if (getCheckBoxItem(farmMenu, "dz191.ahri.farm.qlc"))
            {
                var minionInQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _spells[SpellSlot.Q].Range);
                var killableMinions = _spells[SpellSlot.Q].GetLineFarmLocation(minionInQ);
                if (killableMinions.MinionsHit >= 3)
                {
                    _spells[SpellSlot.Q].Cast(killableMinions.Position);
                }
            }
            if (getCheckBoxItem(farmMenu, "dz191.ahri.farm.wlc"))
            {
                var minionInW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _spells[SpellSlot.W].Range);
                if (minionInW.Count > 0)
                {
                    _spells[SpellSlot.W].Cast();
                }
            }
        }
        private static void HandleRCombo(AIHeroClient target)
        {
            if (_spells[SpellSlot.R].IsReady() && getCheckBoxItem(comboMenu, "dz191.ahri.combo.user"))
            {
                //User chose not to initiate with R.
                if (getCheckBoxItem(comboMenu, "dz191.ahri.combo.initr"))
                {
                    return;
                }
                //Neither Q or E are ready in <= 2 seconds and we can't kill the enemy with 1 R stack. Don't use R
                if ((!_spells[SpellSlot.Q].IsReady(2) && !_spells[SpellSlot.E].IsReady(2)) || !(Helpers.GetComboDamage(target) >= target.Health + 20))
                {
                    return;
                }
                //Set the test position to the Cursor Position
                var testPosition = Game.CursorPos;
                //Extend from out position towards there
                var extendedPosition = ObjectManager.Player.Position.Extend(testPosition, 500f).To3D();
                //Safety checks
                if (extendedPosition.IsSafe())
                {
                    _spells[SpellSlot.R].Cast(extendedPosition);
                }
            }
        }
        #endregion

        #region Event delegates
        static void Game_OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)) {
                Combo();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Harass();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                Laneclear();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
                LastHit();
            }
            if (getCheckBoxItem(miscMenu, "dz191.ahri.misc.userexpire"))
            {
                var rBuff = ObjectManager.Player.Buffs.Find(buff => buff.Name == "AhriTumble");
                if (rBuff != null)
                {
                    //This tryhard tho
                    if (rBuff.EndTime - Game.Time <= 1.0f + (Game.Ping / (2f * 1000f)))
                    {
                        var extendedPosition = ObjectManager.Player.Position.Extend(Game.CursorPos, _spells[SpellSlot.R].Range).To3D();
                        if (extendedPosition.IsSafe())
                        {
                            _spells[SpellSlot.R].Cast(extendedPosition);
                        }
                    }
                }
            }
            if (getCheckBoxItem(miscMenu, "dz191.ahri.misc.autoq"))
            {
                var charmedUnit = HeroManager.Enemies.Find(h => h.IsCharmed() && h.IsValidTarget(_spells[SpellSlot.Q].Range));
                if (charmedUnit != null)
                {
                    _spells[SpellSlot.Q].Cast(charmedUnit);
                }
            }
            if (getCheckBoxItem(miscMenu, "dz191.ahri.misc.autoq2"))
            {
                var qMana = getSliderItem(miscMenu, "dz191.ahri.misc.autoq2mana");
                if (ObjectManager.Player.ManaPercent >= qMana && _spells[SpellSlot.Q].IsReady())
                {
                    var target = TargetSelector.GetTarget(_spells[SpellSlot.Q].Range, DamageType.Magical);
                    if (target != null && ObjectManager.Player.Distance(target) >= _spells[SpellSlot.Q].Range * 0.7f)
                    {
                        _spells[SpellSlot.Q].CastIfHitchanceEquals(target, LeagueSharp.Common.HitChance.High);
                    }
                }
            }
            if (getKeyBindItem(miscMenu, "dz191.ahri.misc.instacharm") && _spells[SpellSlot.E].IsReady())
            {
                var target = TargetSelector.GetTarget(_spells[SpellSlot.E].Range, DamageType.Magical);
                if (target != null)
                {
                    var prediction = _spells[SpellSlot.E].GetPrediction(target);
                    _spells[SpellSlot.E].Cast(prediction.CastPosition);
                }
            }

        }
        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (getCheckBoxItem(miscMenu, "dz191.ahri.misc.egp") && gapcloser.Sender.IsValidTarget(_spells[SpellSlot.E].Range) && _spells[SpellSlot.E].IsReady())
            {
                _spells[SpellSlot.E].Cast(gapcloser.Sender);
            }
            if (getCheckBoxItem(miscMenu, "dz191.ahri.misc.rgap") && !_spells[SpellSlot.E].IsReady() &&
                _spells[SpellSlot.R].IsReady())
            {
                _spells[SpellSlot.R].Cast(ObjectManager.Player.ServerPosition.Extend(gapcloser.End, -400f));
            }
        }
        static void Interrupter2_OnInterruptableTarget(AIHeroClient sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (getCheckBoxItem(miscMenu, "dz191.ahri.misc.eint") && args.DangerLevel >= Interrupter2.DangerLevel.Medium && _spells[SpellSlot.E].IsReady())
            {
                _spells[SpellSlot.E].Cast(sender.ServerPosition);
            }
        }
        static void Drawing_OnDraw(EventArgs args)
        {
            var qItem = getCheckBoxItem(drawMenu, "dz191.ahri.drawings.q");
            var eItem = getCheckBoxItem(drawMenu, "dz191.ahri.drawings.E");
            if (qItem)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, _spells[SpellSlot.Q].Range, Color.Aqua);
            }
            if (eItem)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, _spells[SpellSlot.E].Range, Color.Aqua);
            }
        }

        #endregion

        #region Events, Spells, Menu Init
        private static void SetUpEvents()
        {
            Game.OnUpdate += Game_OnUpdate;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void SetUpSpells()
        {
            _spells[SpellSlot.Q].SetSkillshot(0.25f, 100, 1600, false, SkillshotType.SkillshotLine);
            _spells[SpellSlot.E].SetSkillshot(0.25f, 60, 1200, true, SkillshotType.SkillshotLine);
        }

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

        private static void SetUpMenu()
        {
            Menu = MainMenu.AddMenu("DZAhri", "dz191.ahri");

            comboMenu = Menu.AddSubMenu("[Ahri] Combo", "dz191.ahri.combo");
            comboMenu.Add("dz191.ahri.combo.useq", new CheckBox("Use Q Combo"));
            comboMenu.Add("dz191.ahri.combo.usew", new CheckBox("Use W Combo"));
            comboMenu.Add("dz191.ahri.combo.usee", new CheckBox("Use E Combo"));
            comboMenu.Add("dz191.ahri.combo.user", new CheckBox("Use R Combo"));
            comboMenu.Add("dz191.ahri.combo.initr", new CheckBox("Don't Initiate with R", false));
            comboMenu.Add("dz191.ahri.combo.mana", new Slider("Min Combo Mana", 20, 0, 100));
            comboMenu.Add("dz191.ahri.combo.mode", new Slider("Combo Mode : (1 : Wait For Charm | 2 : Don't Wait for Charm)", 1, 1, 2));
            comboMenu.AddSeparator();

            harassMenu = Menu.AddSubMenu("[Ahri] Harass", "dz191.ahri.harass");
            harassMenu.Add("dz191.ahri.harass.useq", new CheckBox("Use Q Harass"));
            harassMenu.Add("dz191.ahri.harass.usew", new CheckBox("Use W Harass"));
            harassMenu.Add("dz191.ahri.harass.usee", new CheckBox("Use E Harass"));
            harassMenu.Add("dz191.ahri.harass.onlyqcharm", new CheckBox("Use Q Only when charmed"));
            harassMenu.Add("dz191.ahri.harass.mana", new Slider("Min Harass Mana", 20, 0, 100));
            harassMenu.AddSeparator();

            miscMenu = Menu.AddSubMenu("[Ahri] Misc", "dz191.ahri.misc");
            miscMenu.Add("dz191.ahri.misc.egp", new CheckBox("Auto E Gapclosers"));
            miscMenu.Add("dz191.ahri.misc.eint", new CheckBox("Auto E Interrupter"));
            miscMenu.Add("dz191.ahri.misc.rgap", new CheckBox("R away gapclosers if E on CD", false));
            miscMenu.Add("dz191.ahri.misc.autoq", new CheckBox("Auto Q Charmed targets", false));
            miscMenu.Add("dz191.ahri.misc.userexpire", new CheckBox("Use R when about to expire", false));
            miscMenu.Add("dz191.ahri.misc.autoq2", new CheckBox("Auto Q poke (Long range)", false));
            miscMenu.Add("dz191.ahri.misc.autoq2mana", new Slider("Auto Q mana", 25, 0, 100));
            miscMenu.Add("dz191.ahri.misc.instacharm", new KeyBind("Instacharm", false, KeyBind.BindTypes.PressToggle, 'T'));
            miscMenu.AddSeparator();

            farmMenu = Menu.AddSubMenu("[Ahri] Farm", "dz191.ahri.farm");
            farmMenu.Add("dz191.ahri.farm.qlh", new CheckBox("Use Q LastHit", false));
            farmMenu.Add("dz191.ahri.farm.qlc", new CheckBox("Use Q Laneclear", false));
            farmMenu.Add("dz191.ahri.farm.wlc", new CheckBox("Use W Laneclear", false));
            farmMenu.Add("dz191.ahri.farm.mana", new Slider("Min Farm Mana", 20, 0, 100));
            farmMenu.AddSeparator();

            drawMenu = Menu.AddSubMenu("[Ahri] Drawing", "dz191.ahri.drawings");
            drawMenu.Add("dz191.ahri.drawings.q", new CheckBox("Draw Q"));
            drawMenu.Add("dz191.ahri.drawings.e", new CheckBox("Draw E"));
            drawMenu.AddSeparator();
        }
        #endregion
    }
}
