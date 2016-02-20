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

namespace Champ
{
    public static class Program
    {

        //private static Spell.Skillshot Q, E, R;
        //private static Spell.Active W;

        private static Menu Menu;

        private static AIHeroClient myHero
        {
            get { return Player.Instance; }
        }

        public static void Main()
        {
            Loading.OnLoadingComplete += OnLoad;
        }

        #region Menu Items
        //public static bool useQ { get { return Menu["useQ"].Cast<CheckBox>().CurrentValue; } }

        #endregion

        private static void OnLoad(EventArgs args)
        {
            if (myHero.Hero != Champion.Ashe)
            {
                return;
            }

            Menu = MainMenu.AddMenu("", "");
            Menu.AddLabel("Ported from - Berb");
            Menu.AddSeparator();
            Menu.AddGroupLabel("Combo");
            //Menu.Add("useQ", new CheckBox("Use Q"));
            Menu.AddSeparator();
			
			/*
            Q = new Spell.Skillshot(SpellSlot.Q, 950, SkillShotType.Linear, 250, 1650, 70);
            W = new Spell.Active(SpellSlot.W, (uint)myHero.GetAutoAttackRange());
            E = new Spell.Skillshot(SpellSlot.E, 650, SkillShotType.Linear, 500, 1400, 120);
            R = new Spell.Skillshot(SpellSlot.R, 1260, SkillShotType.Circular, 1500, int.MaxValue, 225);
			*/

            Game.OnTick += OnTick;
        }

        private static void OnTick(EventArgs args)
        {
        }
    }
}
