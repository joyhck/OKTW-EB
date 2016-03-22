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
using SPrediction;
using OneKeyToWin_AIO_Sebby;
using Core = OneKeyToWin_AIO_Sebby.Core;

namespace SebbyLib
{
    class Program
    {

        public static SebbyLib.Prediction.PredictionOutput DrawSpellPos;
        public static LeagueSharp.Common.Spell Q, W, E, R, DrawSpell;
        public static float JungleTime, DrawSpellTime = 0;
        public static int AIOmode = 0;
        public static Menu Config;
        public static Obj_SpawnPoint enemySpawn;
        public static bool SPredictionLoad = false;
        public static int timer, HitChanceNum = 4, tickNum = 4, tickIndex = 0;
        private static float dodgeRange = 420;
        private static float dodgeTime = Game.Time;
        public static List<AIHeroClient> Enemies = new List<AIHeroClient>(), Allies = new List<AIHeroClient>();
        private static bool IsJungler(AIHeroClient hero) { return hero.Spellbook.Spells.Any(spell => spell.Name.ToLower().Contains("smite")); }
        public static AIHeroClient jungler = ObjectManager.Player;

        public static bool Farm { get { return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass); } }
        public static bool Combo { get { return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo); } }
        public static bool None { get { return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None); } }
        public static bool LaneClear { get { return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear); } }

        private static AIHeroClient Player { get { return ObjectManager.Player; } }

        public static bool getCheckBoxItem(string item)
        {
            return Config[item].Cast<CheckBox>().CurrentValue;
        }

        public static int getSliderItem(string item)
        {
            return Config[item].Cast<Slider>().CurrentValue;
        }

        public static bool getKeyBindItem(string item)
        {
            return Config[item].Cast<KeyBind>().CurrentValue;
        }

        public static void GameOnOnGameLoad()
        {
            enemySpawn = ObjectManager.Get<Obj_SpawnPoint>().FirstOrDefault(x => x.IsEnemy);
            Q = new LeagueSharp.Common.Spell(SpellSlot.Q);
            E = new LeagueSharp.Common.Spell(SpellSlot.E);
            W = new LeagueSharp.Common.Spell(SpellSlot.W);
            R = new LeagueSharp.Common.Spell(SpellSlot.R);

            Config = MainMenu.AddMenu("OneKeyToWin AIO", "OneKeyToWin_AIO" + ObjectManager.Player.ChampionName);

            #region MENU ABOUT OKTW
            Config.Add("debug", new CheckBox("Debug", false));
            Config.Add("debugChat", new CheckBox("Debug Chat", false));
            Config.Add("print", new CheckBox("OKTW NEWS in chat"));
            #endregion

            Config.Add("AIOmode", new Slider("AIO mode (0 : Util & Champ | 1 : Only Champ | 2 : Only Util)", 0, 0, 2));
            AIOmode = getSliderItem("AIOmode");

            if (AIOmode != 2)
            {
                if (Player.ChampionName != "MissFortune")
                {
                    new Core.OktwTs().LoadOKTW();
                }
            }

            if (AIOmode != 1)
            {
                Config.Add("timer", new CheckBox("GankTimer"));
                Config.AddLabel("RED - be careful");
                Config.AddLabel("ORANGE - you have time");
                Config.AddLabel("GREEN - jungler visable");
                Config.AddLabel("CYAN jungler dead - take objectives");
            }

            Config.Add("PredictionMODE", new Slider("Prediction MODE (0 : Common Pred | 1 : OKTW© PREDICTION | 2 : SPediction press F5 if not loaded)", 0, 0, 2));
            Config.Add("HitChance", new Slider("AIO mode (0 : Very High | 1 : High | 2 : Medium)", 0, 0, 2));
            Config.Add("debugPred", new CheckBox("Draw Aiming OKTW© PREDICTION", false));

            if (getSliderItem("PredictionMODE") == 2)
            {
                SPrediction.Prediction.Initialize(Config);
                SPredictionLoad = true;
            }
            else
            {
                Config.AddLabel("SPREDICTION NOT LOADED");
            }

            if (AIOmode != 2)
            {
                Config.Add("supportMode", new CheckBox("Support Mode", false));
                Config.Add("comboDisableMode", new CheckBox("Disable auto-attack in combo mode", false));
                Config.Add("manaDisable", new CheckBox("Disable mana manager in combo", true));
                Config.Add("collAA", new CheckBox("Disable auto-attack if Yasuo wall collision", true));

                #region LOAD CHAMPIONS


                switch (Player.ChampionName)
                {
                    case "Anivia":
                        PortAIO.Champion.Anivia.Program.LoadOKTW();
                        break;
                    case "Annie":
                        PortAIO.Champion.Annie.Program.LoadOKTW();
                        break;
                    case "Ashe":
                        PortAIO.Champion.Ashe.Program.LoadOKTW();
                        break;
                }
            }

                #endregion

            foreach (var hero in HeroManager.Enemies)
            {
                if (hero.IsEnemy && hero.Team != Player.Team)
                {
                    Enemies.Add(hero);
                    if (IsJungler(hero))
                        jungler = hero;
                }
            }

            foreach (var hero in HeroManager.Allies)
            {
                if (hero.IsAlly && hero.Team == Player.Team)
                    Allies.Add(hero);
            }

            if (getCheckBoxItem("debug"))
            {
                new Core.OKTWlab().LoadOKTW();
            }

            if (AIOmode != 1)
            {
                new OneKeyToWin_AIO_Sebby.Activator().LoadOKTW();
                new Core.OKTWward().LoadOKTW();
                new Core.AutoLvlUp().LoadOKTW();
                new Core.OKTWtracker().LoadOKTW();
                new Core.OKTWdraws().LoadOKTW();
            }

            new Core.OKTWtracker().LoadOKTW();

            Config.AddGroupLabel("!!! PRESS F5 TO RELOAD MODE !!!");

            Game.OnUpdate += OnUpdate;
            Orbwalker.OnPreAttack += Orbwalking_BeforeAttack;
            Drawing.OnDraw += OnDraw;

            if (getCheckBoxItem("print"))
            {
                Chat.Print("<font size='30'>OneKeyToWin</font> <font color='#b756c5'>by Sebby</font>");
                Chat.Print("<font color='#b756c5'>OKTW NEWS: </font> Vayne Q fix, Jinx faster E, Thresh new options");
            }
        }

        public static void debug(string msg)
        {
            if (getCheckBoxItem("debug"))
            {
                Console.WriteLine(msg);
            }
            if (getCheckBoxItem("debugChat"))
            {
                Chat.Print(msg);
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            tickIndex++;

            if (tickIndex > 4)
                tickIndex = 0;

            if (!LagFree(0))
                return;

            JunglerTimer();
        }

        public static void JunglerTimer()
        {
            if (AIOmode != 1 && getCheckBoxItem("timer") && jungler != null && jungler.IsValid)
            {
                if (jungler.IsDead)
                {
                    timer = (int)(enemySpawn.Position.Distance(Player.Position) / 370);
                }
                else if (jungler.IsVisible)
                {
                    float Way = 0;
                    var JunglerPath = Player.GetPath(Player.Position, jungler.Position);
                    var PointStart = Player.Position;
                    if (JunglerPath == null)
                        return;
                    foreach (var point in JunglerPath)
                    {
                        var PSDistance = PointStart.Distance(point);
                        if (PSDistance > 0)
                        {
                            Way += PSDistance;
                            PointStart = point;
                        }
                    }
                    timer = (int)(Way / jungler.MoveSpeed);
                }
            }
        }

        public static bool LagFree(int offset)
        {
            if (tickIndex == offset)
                return true;
            else
                return false;
        }

        private static void OnDraw(EventArgs args)
        {
            if (!SPredictionLoad && (int)Game.Time % 2 == 0 && getSliderItem("PredictionMODE") == 2)
            {
                drawText("PRESS F5 TO LOAD SPREDICTION", Player.Position, System.Drawing.Color.Yellow, -300);
            }

            if (AIOmode == 1)
                return;

            if (Game.Time - DrawSpellTime < 0.5 && getCheckBoxItem("debugPred") && getSliderItem("PredictionMODE") == 1)
            {
                if (DrawSpell.Type == SkillshotType.SkillshotLine)
                    OktwCommon.DrawLineRectangle(DrawSpellPos.CastPosition, Player.Position, (int)DrawSpell.Width, 1, System.Drawing.Color.DimGray);
                if (DrawSpell.Type == SkillshotType.SkillshotCircle)
                    Render.Circle.DrawCircle(DrawSpellPos.CastPosition, DrawSpell.Width, System.Drawing.Color.DimGray, 1);

                drawText("Aiming " + DrawSpellPos.Hitchance, Player.Position.Extend(DrawSpellPos.CastPosition, 400).To3D(), System.Drawing.Color.Gray);
            }

            if (AIOmode != 1 && getCheckBoxItem("timer") && jungler != null)
            {
                if (jungler == Player)
                    drawText("Jungler not detected", Player.Position, System.Drawing.Color.Yellow, 100);
                else if (jungler.IsDead)
                    drawText("Jungler dead " + timer, Player.Position, System.Drawing.Color.Cyan, 100);
                else if (jungler.IsVisible)
                    drawText("Jungler visable " + timer, Player.Position, System.Drawing.Color.GreenYellow, 100);
                else
                {
                    if (timer > 0)
                        drawText("Jungler in jungle " + timer, Player.Position, System.Drawing.Color.Orange, 100);
                    else if ((int)(Game.Time * 10) % 2 == 0)
                        drawText("BE CAREFUL " + timer, Player.Position, System.Drawing.Color.OrangeRed, 100);
                    if (Game.Time - JungleTime >= 1)
                    {
                        timer = timer - 1;
                        JungleTime = Game.Time;
                    }
                }
            }
        }

        private static void Orbwalking_BeforeAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (AIOmode == 2)
                return;

            if (Combo && getCheckBoxItem("comboDisableMode"))
            {
                var t = (AIHeroClient)args.Target;
                if (6 * Player.GetAutoAttackDamage(t) < t.Health - OktwCommon.GetIncomingDamage(t) && !t.HasBuff("luxilluminatingfraulein") && !Player.HasBuff("sheen"))
                    args.Process = false;
            }

            if (!Player.IsMelee && OktwCommon.CollisionYasuo(Player.ServerPosition, args.Target.Position) && getCheckBoxItem("collAA"))
            {
                args.Process = false;
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) && getCheckBoxItem("supportMode"))
            {
                if (args.Target.Type == GameObjectType.obj_AI_Minion) args.Process = false;
            }
        }

        public static void drawText(string msg, Vector3 Hero, System.Drawing.Color color, int weight = 0)
        {
            var wts = Drawing.WorldToScreen(Hero);
            Drawing.DrawText(wts[0] - (msg.Length) * 5, wts[1] + weight, color, msg);
        }

        public static void CastSpell(LeagueSharp.Common.Spell QWER, Obj_AI_Base target)
        {
            if (getSliderItem("PredictionMODE") == 1)
            {
                SebbyLib.Prediction.SkillshotType CoreType2 = SebbyLib.Prediction.SkillshotType.SkillshotLine;
                bool aoe2 = false;

                if (QWER.Type == SkillshotType.SkillshotCircle)
                {
                    CoreType2 = SebbyLib.Prediction.SkillshotType.SkillshotCircle;
                    aoe2 = true;
                }

                if (QWER.Width > 80 && !QWER.Collision)
                    aoe2 = true;

                var predInput2 = new SebbyLib.Prediction.PredictionInput
                {
                    Aoe = aoe2,
                    Collision = QWER.Collision,
                    Speed = QWER.Speed,
                    Delay = QWER.Delay,
                    Range = QWER.Range,
                    From = Player.ServerPosition,
                    Radius = QWER.Width,
                    Unit = target,
                    Type = CoreType2
                };
                var poutput2 = SebbyLib.Prediction.Prediction.GetPrediction(predInput2);

                if (QWER.Speed != float.MaxValue && OktwCommon.CollisionYasuo(Player.ServerPosition, poutput2.CastPosition))
                    return;

                if (getSliderItem("HitChance") == 0)
                {
                    if (poutput2.Hitchance >= SebbyLib.Prediction.HitChance.VeryHigh)
                        QWER.Cast(poutput2.CastPosition);
                    else if (predInput2.Aoe && poutput2.AoeTargetsHitCount > 1 && poutput2.Hitchance >= SebbyLib.Prediction.HitChance.High)
                    {
                        QWER.Cast(poutput2.CastPosition);
                    }

                }
                else if (getSliderItem("HitChance") == 1)
                {
                    if (poutput2.Hitchance >= SebbyLib.Prediction.HitChance.High)
                        QWER.Cast(poutput2.CastPosition);

                }
                else if (getSliderItem("HitChance") == 2)
                {
                    if (poutput2.Hitchance >= SebbyLib.Prediction.HitChance.Medium)
                        QWER.Cast(poutput2.CastPosition);
                }
                if (Game.Time - DrawSpellTime > 0.5)
                {
                    DrawSpell = QWER;
                    DrawSpellTime = Game.Time;

                }
                DrawSpellPos = poutput2;
            }
            else if (getSliderItem("PredictionMODE") == 0)
            {
                if (getSliderItem("HitChance") == 0)
                {
                    QWER.CastIfHitchanceEquals(target, LeagueSharp.Common.HitChance.VeryHigh);
                    return;
                }
                else if (getSliderItem("HitChance") == 1)
                {
                    QWER.CastIfHitchanceEquals(target, LeagueSharp.Common.HitChance.High);
                    return;
                }
                else if (getSliderItem("HitChance") == 2)
                {
                    QWER.CastIfHitchanceEquals(target, LeagueSharp.Common.HitChance.Medium);
                    return;
                }
            }
            else if (getSliderItem("PredictionMODE") == 2)
            {

                if (target is AIHeroClient && target.IsValid)
                {
                    var t = target as AIHeroClient;
                    if (getSliderItem("HitChance") == 0)
                    {
                        QWER.SPredictionCast(t, LeagueSharp.Common.HitChance.VeryHigh);
                        return;
                    }
                    else if (getSliderItem("HitChance") == 1)
                    {
                        QWER.SPredictionCast(t, LeagueSharp.Common.HitChance.High);
                        return;
                    }
                    else if (getSliderItem("HitChance") == 2)
                    {
                        QWER.SPredictionCast(t, LeagueSharp.Common.HitChance.Medium);
                        return;
                    }
                }
                else
                {
                    QWER.CastIfHitchanceEquals(target, LeagueSharp.Common.HitChance.High);
                }
            }
        }
    }
}
