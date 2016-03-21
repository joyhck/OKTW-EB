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
using SebbyLib;

namespace PortAIO.Champion.Annie
{
    class Program
    {
        private static Menu Config = SebbyLib.Program.Config;
        public static SebbyLib.Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        public static LeagueSharp.Common.Spell Q, W, E, R, FR;
        private static Menu drawMenu, QMenu, WMenu, EMenu, RMenu, FarmMenu;
        public static float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;
        private static SpellSlot flash;
        public static GameObject Tibbers;
        public static float TibbersTimer = 0;
        private static bool HaveStun = false;
        private static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }

        public static bool getCheckBoxItem(Menu m, string item)
        {
            return m[item].Cast<CheckBox>().CurrentValue;
        }

        public static int getSliderItem(Menu m, string item)
        {
            return m[item].Cast<Slider>().CurrentValue;
        }

        public static void LoadOKTW()
        {
            Q = new LeagueSharp.Common.Spell(SpellSlot.Q, 625f);
            W = new LeagueSharp.Common.Spell(SpellSlot.W, 580f);
            E = new LeagueSharp.Common.Spell(SpellSlot.E);
            R = new LeagueSharp.Common.Spell(SpellSlot.R, 625f);
            FR = new LeagueSharp.Common.Spell(SpellSlot.R, 1000f);

            Q.SetTargetted(0.25f, 1400f);
            W.SetSkillshot(0.30f, 100f, float.MaxValue, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.20f, 250f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            FR.SetSkillshot(0.20f, 250f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            flash = Player.GetSpellSlot("summonerflash");

            drawMenu = Config.AddSubMenu("Draw");
            drawMenu.Add("qRange", new CheckBox("Q range"));
            drawMenu.Add("wRange", new CheckBox("W range"));
            drawMenu.Add("rRange", new CheckBox("R range"));
            drawMenu.Add("onlyRdy", new CheckBox("Draw only ready spells"));

            QMenu = Config.AddSubMenu("Q Config");
            QMenu.Add("autoQ", new CheckBox("Auto Q"));
            QMenu.Add("harrasQ", new CheckBox("Harass Q"));

            WMenu = Config.AddSubMenu("W Config");
            WMenu.Add("autoW", new CheckBox("Auto W"));
            WMenu.Add("harrasW", new CheckBox("Harass W"));

            EMenu = Config.AddSubMenu("E Config");
            EMenu.Add("autoE", new CheckBox("Auto E stack stun"));

            RMenu = Config.AddSubMenu("R Config");
            RMenu.AddLabel("0 : Normal");
            RMenu.AddLabel("1 : Always");
            RMenu.AddLabel("2 : Never");
            RMenu.AddLabel("3 : Always Stun");
            foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(enemy => enemy.IsEnemy))
            {
                RMenu.Add("UM" + enemy.ChampionName, new Slider(enemy.ChampionName, 0, 0, 3));
            }
            RMenu.AddSeparator();
            RMenu.Add("autoRks", new CheckBox("Auto R KS"));
            RMenu.Add("autoRcombo", new CheckBox("Auto R Combo if stun is ready"));
            RMenu.Add("rCount", new Slider("Auto R x enemies", 3, 2, 5));
            RMenu.Add("tibers", new CheckBox("Tibbers Auto Pilot"));

            if (flash != SpellSlot.Unknown)
            {
                RMenu.Add("rCountFlash", new Slider("Auto flash + R stun x enemies", 4, 2, 5));
            }

            FarmMenu = Config.AddSubMenu("Farm");
            FarmMenu.Add("farmQ", new CheckBox("Farm Q"));
            FarmMenu.Add("farmW", new CheckBox("Lane clear W"));
            FarmMenu.Add("Mana", new Slider("LaneClear Mana", 60, 0, 100));

            Game.OnUpdate += Game_OnGameUpdate;
            GameObject.OnCreate += Obj_AI_Base_OnCreate;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Obj_AI_Base_OnCreate(GameObject obj, EventArgs args)
        {
            if (obj.IsValid && obj.IsAlly && obj.Type == GameObjectType.obj_AI_Minion && obj.Name == "Tibbers")
            {
                Tibbers = obj;
                SebbyLib.Program.debug("" + obj.Type);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.HasBuff("Recall"))
                return;

            HaveStun = Player.HasBuff("pyromania_particle");

            SetMana();

            if (R.IsReady() && SebbyLib.Program.LagFree(1) && !HaveTibers)
            {
                var realRange = R.Range;

                if (flash.IsReady())
                    realRange = FR.Range;

                foreach (var enemy in SebbyLib.Program.Enemies.Where(enemy => enemy.IsValidTarget(realRange) && OktwCommon.ValidUlt(enemy)))
                {
                    if (enemy.IsValidTarget(R.Range))
                    {
                        int Rmode = getSliderItem(RMenu, "UM" + enemy.ChampionName);

                        if (Rmode == 2)
                            continue;

                        var poutput = R.GetPrediction(enemy, true);
                        var aoeCount = poutput.AoeTargetsHitCount;

                        if (Rmode == 1)
                            R.Cast(poutput.CastPosition);

                        if (Rmode == 3 && HaveStun)
                            R.Cast(poutput.CastPosition);

                        if (aoeCount >= getSliderItem(RMenu, "rCount") && getSliderItem(RMenu, "rCount") > 0)
                            R.Cast(poutput.CastPosition);
                        else if (SebbyLib.Program.Combo && HaveStun && getCheckBoxItem(RMenu, "autoRcombo"))
                            R.Cast(poutput.CastPosition);
                        else if (getCheckBoxItem(RMenu, "autoRks"))
                        {
                            var comboDmg = OktwCommon.GetKsDamage(enemy, R);

                            if (W.IsReady() && RMANA + WMANA < Player.Mana)
                                comboDmg += W.GetDamage(enemy);

                            if (Q.IsReady() && RMANA + WMANA + QMANA < Player.Mana)
                                comboDmg += Q.GetDamage(enemy);

                            if (enemy.Health < comboDmg)
                                R.Cast(poutput.CastPosition);
                        }
                    }
                    else if (HaveStun && flash.IsReady())
                    {
                        var poutputFlas = FR.GetPrediction(enemy, true);
                        var aoeCountFlash = poutputFlas.AoeTargetsHitCount;
                        if (HaveStun && aoeCountFlash >= getSliderItem(RMenu, "rCountFlash") && getSliderItem(RMenu, "rCountFlash") > 0)
                        {
                            Player.Spellbook.CastSpell(flash, poutputFlas.CastPosition);
                            R.Cast(poutputFlas.CastPosition);
                        }
                    }
                }
            }

            var t = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            if (t.IsValidTarget())
            {
                if (W.IsReady() && SebbyLib.Program.LagFree(2) && getCheckBoxItem(WMenu, "autoW"))
                {
                    var poutput = W.GetPrediction(t, true);
                    var aoeCount = poutput.AoeTargetsHitCount;

                    if (SebbyLib.Program.Combo && RMANA + WMANA < Player.Mana)
                        W.Cast(poutput.CastPosition);
                    else if (SebbyLib.Program.Farm && RMANA + WMANA + QMANA < Player.Mana && getCheckBoxItem(WMenu, "harrasW"))
                        W.Cast(poutput.CastPosition);
                    else
                    {
                        var wDmg = OktwCommon.GetKsDamage(t, W);
                        var qDmg = Q.GetDamage(t);
                        if (wDmg > t.Health)
                            W.Cast(poutput.CastPosition);
                        else if (qDmg + wDmg > t.Health && Player.Mana > QMANA + WMANA)
                            W.Cast(poutput.CastPosition);
                    }
                }

                if (Q.IsReady() && SebbyLib.Program.LagFree(3) && getCheckBoxItem(QMenu, "autoQ"))
                {
                    if (SebbyLib.Program.Combo && RMANA + WMANA < Player.Mana)
                        Q.Cast(t);
                    else if (SebbyLib.Program.Farm && RMANA + WMANA + QMANA < Player.Mana && getCheckBoxItem(QMenu, "harrasQ"))
                        Q.Cast(t);
                    else
                    {
                        var qDmg = OktwCommon.GetKsDamage(t, Q);
                        var wDmg = W.GetDamage(t);
                        if (qDmg > t.Health)
                            Q.Cast(t);
                        else if (qDmg + wDmg > t.Health && Player.Mana > QMANA + WMANA)
                            Q.Cast(t);
                    }
                }
            }
            else if (Q.IsReady() || W.IsReady())
            {
                if (getCheckBoxItem(FarmMenu, "farmQ"))
                {
                    if (SebbyLib.Program.getCheckBoxItem("supportMode"))
                    {
                        if (SebbyLib.Program.LaneClear && Player.Mana > RMANA + QMANA)
                            farm();
                    }
                    else
                    {
                        if ((!HaveStun || SebbyLib.Program.LaneClear) && SebbyLib.Program.Farm)
                            farm();
                    }
                }
            }

            if (SebbyLib.Program.LagFree(3))
            {
                if (!HaveStun)
                {
                    if (E.IsReady() && !SebbyLib.Program.LaneClear && getCheckBoxItem(EMenu, "autoE") && Player.Mana > RMANA + EMANA + QMANA + WMANA)
                        E.Cast();
                    else if (W.IsReady() && Player.InFountain())
                        W.Cast(Player.Position);
                }
                if (R.IsReady())
                {
                    if (getCheckBoxItem(RMenu, "tibers") && HaveTibers)
                    {
                        var BestEnemy = TargetSelector.GetTarget(2000, DamageType.Magical);
                        if (BestEnemy.IsValidTarget(2000) && Game.Time - TibbersTimer > 2)
                        {
                            EloBuddy.Player.IssueOrder(GameObjectOrder.MovePet, BestEnemy.Position);
                            R.CastOnUnit(BestEnemy);
                            TibbersTimer = Game.Time;
                        }
                    }
                    else
                    {
                        Tibbers = null;
                    }
                }
            }
        }

        private static void farm()
        {
            if (SebbyLib.Program.LaneClear)
            {
                var mobs = Cache.GetMinions(Player.ServerPosition, Q.Range, MinionTeam.Neutral);
                if (mobs.Count > 0)
                {
                    var mob = mobs[0];
                    if (W.IsReady())
                        W.Cast(mob);
                    else if (Q.IsReady())
                        Q.Cast(mob);
                }
            }

            var minionsList = Cache.GetMinions(Player.ServerPosition, Q.Range);
            if (Q.IsReady())
            {
                var minion = minionsList.Where(x => SebbyLib.HealthPrediction.LaneClearHealthPrediction(x, 250, 50) < Q.GetDamage(x) && x.Health > Player.GetAutoAttackDamage(x)).FirstOrDefault();
                Q.Cast(minion);
            }
            else if (SebbyLib.Program.LaneClear && W.IsReady() && Player.ManaPercent > getSliderItem(FarmMenu, "Mana") && getCheckBoxItem(FarmMenu, "farmW"))
            {
                var farmLocation = W.GetCircularFarmLocation(minionsList, W.Width);
                if (farmLocation.MinionsHit > 1)
                    W.Cast(farmLocation.Position);
            }
        }

        private static bool HaveTibers
        {
            get { return Player.HasBuff("infernalguardiantimer"); }
        }

        private static void SetMana()
        {
            if ((SebbyLib.Program.getCheckBoxItem("manaDisable") && SebbyLib.Program.Combo) || Player.HealthPercent < 20)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
                return;
            }

            QMANA = Q.Instance.SData.Mana;
            WMANA = W.Instance.SData.Mana;
            EMANA = E.Instance.SData.Mana;

            if (!R.IsReady() || HaveTibers)
                RMANA = 0;
            else
                RMANA = R.Instance.SData.Mana;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {

            if (getCheckBoxItem(drawMenu, "qRange"))
            {
                if (getCheckBoxItem(drawMenu, "onlyRdy"))
                {
                    if (Q.IsReady())
                        LeagueSharp.Common.Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
                }
                else
                    LeagueSharp.Common.Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
            }
            if (getCheckBoxItem(drawMenu, "wRange"))
            {
                if (getCheckBoxItem(drawMenu, "onlyRdy"))
                {
                    if (W.IsReady())
                        LeagueSharp.Common.Utility.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1, 1);
                }
                else
                    LeagueSharp.Common.Utility.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1, 1);
            }

            if (getCheckBoxItem(drawMenu, "rRange"))
            {
                if (getCheckBoxItem(drawMenu, "onlyRdy"))
                {
                    if (R.IsReady())
                        LeagueSharp.Common.Utility.DrawCircle(ObjectManager.Player.Position, R.Range + R.Width / 2, System.Drawing.Color.Gray, 1, 1);
                }
                else
                    LeagueSharp.Common.Utility.DrawCircle(ObjectManager.Player.Position, R.Range + R.Width / 2, System.Drawing.Color.Gray, 1, 1);
            }

        }
    }
}
