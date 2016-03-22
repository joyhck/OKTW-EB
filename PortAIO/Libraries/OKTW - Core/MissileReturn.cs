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

namespace OneKeyToWin_AIO_Sebby.Core
{
    class MissileReturn
    {
        public AIHeroClient Target;
        private static AIHeroClient Player { get { return ObjectManager.Player; } }
        private static Menu Config = Program.Config;
        public static Menu Sub;
        private string MissileName, MissileReturnName;
        private LeagueSharp.Common.Spell QWER;
        public MissileClient Missile;
        private Vector3 MissileEndPos;

        public static bool getCheckBoxItem(string item)
        {
            return Sub[item].Cast<CheckBox>().CurrentValue;
        }

        public MissileReturn(string missile, string missileReturnName, LeagueSharp.Common.Spell qwer)
        {
            Sub = Config.AddSubMenu("Missile Settings");
            Sub.Add("aim", new CheckBox("Auto aim returned missile (" + qwer.Slot + ")"));
            Sub.Add("drawHelper", new CheckBox("Show " + qwer.Slot + " helper"));

            MissileName = missile;
            MissileReturnName = missileReturnName;
            QWER = qwer;

            GameObject.OnCreate += SpellMissile_OnCreateOld;
            GameObject.OnDelete += Obj_SpellMissile_OnDelete;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Missile != null && Missile.IsValid && getCheckBoxItem("drawHelper"))
                OktwCommon.DrawLineRectangle(Missile.Position, Player.Position, (int)QWER.Width, 1, System.Drawing.Color.White);
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (getCheckBoxItem("aim"))
            {
                var posPred = CalculateReturnPos();
                if (posPred != Vector3.Zero)
                    Orbwalker.OrbwalkTo(posPred);
                else
                    Orbwalker.OrbwalkTo(Game.CursorPos);
            }
            
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.Slot == QWER.Slot)
            {
                MissileEndPos = args.End;
            }
        }

        private void SpellMissile_OnCreateOld(GameObject sender, EventArgs args)
        {
            if (sender.IsEnemy || sender.Type != GameObjectType.MissileClient || !sender.IsValid<MissileClient>())
                return;

            MissileClient missile = (MissileClient)sender;

            if (missile.SData.Name != null)
            {
                if (missile.SData.Name.ToLower() == MissileName.ToLower() || missile.SData.Name.ToLower() == MissileReturnName.ToLower())
                {
                    Missile = missile;
                }
            }
        }

        private void Obj_SpellMissile_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.IsEnemy || sender.Type != GameObjectType.MissileClient || !sender.IsValid<MissileClient>())
                return;

            MissileClient missile = (MissileClient)sender;

            if (missile.SData.Name != null)
            {
                if (missile.SData.Name.ToLower() == MissileReturnName.ToLower())
                {
                    Missile = null;
                }
            }
        }

        public Vector3 CalculateReturnPos()
        {
            if (Missile != null && Missile.IsValid && Target.IsValidTarget())
            {
                var finishPosition = Missile.Position;
                if (Missile.SData.Name == MissileName)
                {
                    finishPosition = MissileEndPos;
                }

                var misToPlayer = Player.Distance(finishPosition);
                var tarToPlayer = Player.Distance(Target);

                if (misToPlayer > tarToPlayer)
                {
                    var misToTarget = Target.Distance(finishPosition);

                    if (misToTarget < QWER.Range && misToTarget > 50)
                    {
                        var cursorToTarget = Target.Distance(Player.Position.Extend(Game.CursorPos, 100));
                        var ext = finishPosition.Extend(Target.ServerPosition, cursorToTarget + misToTarget);

                        if (ext.Distance(Player.Position) < 800 && ext.CountEnemiesInRange(400) < 2)
                        {
                            if (getCheckBoxItem("drawHelper"))
                                LeagueSharp.Common.Utility.DrawCircle(ext.To3D(), 100, System.Drawing.Color.White, 1, 1);
                            return ext.To3D();
                        }
                    }
                }
            }
            return Vector3.Zero;
        }
    }
}
