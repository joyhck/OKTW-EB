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

namespace Jinx
{
    public static class Program
    {

        private static Spell.Skillshot W, E, R;
        private static Spell.Active Q;

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
        //public static bool Rjungle { get { return Menu["Rjungle"].Cast<CheckBox>().CurrentValue; } }
        #endregion

        private static void OnLoad(EventArgs args)
        {
            if (myHero.Hero != Champion.Jinx)
            {
                return;
            }

            Menu = MainMenu.AddMenu("SS Jinx", "jinx");
            Menu.AddLabel("Ported from SharpShooter - Berb");
            Menu.AddSeparator();
            Menu.AddGroupLabel("Combo");
            //Menu.Add("autoQ", new CheckBox("Use Q"));
            Menu.AddSeparator();


            Q = new Spell.Active(SpellSlot.Q);
            W = new Spell.Skillshot(SpellSlot.W, 1450, SkillShotType.Linear, 600, 3300, 60);
            E = new Spell.Skillshot(SpellSlot.E, 650, SkillShotType.Circular, 1100, 1750, 1);
            R = new Spell.Skillshot(SpellSlot.R, 3000, SkillShotType.Linear, 600, 1700, 140);

            Game.OnTick += OnTick;
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
            Orbwalker.OnPreAttack += OnPreAttack;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            Gapcloser.OnGapcloser += OnGapCloser;
        }

        private static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
        }

        private static void OnTick(EventArgs args)
        {
        }


        private static void OnGapCloser(Obj_AI_Base sender, Gapcloser.GapcloserEventArgs args)
        {
        }

        private static void OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
        }

        private static void OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
        }
    }
}
