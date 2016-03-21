﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
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
using SCommon.Maths;
using SCommon.Database;
//typedefs
using Geometry = SCommon.Maths.Geometry;

namespace SCommon.Evade
{
    public struct EvadeData
    {
        public Vector2 Position;
        public bool IsTargetted;
        public bool IsSelfCast;
        public AIHeroClient Target;

        public EvadeData(Vector2 v, bool bl1, bool bl2, AIHeroClient obj)
        {
            Position = v;
            IsTargetted = bl1;
            IsSelfCast = bl2;
            Target = obj;
        }
    }

    public class Evader
    {
        private EvadeMethods SpecialMethod;
        private LeagueSharp.Common.Spell EvadeSpell;
        private static Menu evade;
        private Thread m_evade_thread;

        public ObjectPool<DetectedSpellData> m_spell_pool = new ObjectPool<DetectedSpellData>(() => new DetectedSpellData());
        public ConcurrentQueue<DetectedSpellData> m_spell_queue = new ConcurrentQueue<DetectedSpellData>();
        public ConcurrentQueue<EvadeData> m_evade_queue = new ConcurrentQueue<EvadeData>();

        public static bool getCheckBoxItem(string item)
        {
            return evade[item].Cast<CheckBox>().CurrentValue;
        }

        public static int getSliderItem(string item)
        {
            return evade[item].Cast<Slider>().CurrentValue;
        }

        public static bool getKeyBindItem(string item)
        {
            return evade[item].Cast<KeyBind>().CurrentValue;
        }

        /// <summary>
        /// Evader constructor
        /// </summary>
        /// <param name="_evade">The evader menu.</param>
        /// <param name="method">The evade method.</param>
        /// <param name="spl">The evade spell.</param>
        public Evader(out Menu _evade, EvadeMethods method = EvadeMethods.None, LeagueSharp.Common.Spell spl = null)
        {
            SpellDatabase.InitalizeSpellDatabase();
            SpecialMethod = method;
            EvadeSpell = spl;
            evade = MainMenu.AddMenu("Evade", "SCommon.Evade.Root");

            foreach (var enemy in HeroManager.Enemies)
            {
                foreach (var spell in SpellDatabase.EvadeableSpells.Where(p => p.ChampionName == enemy.ChampionName && p.EvadeMethods.HasFlag(method)))
                {
                    evade.Add(String.Format("SCommon.Evade.Spell.{0}", spell.SpellName), new CheckBox(String.Format("{0} ({1})", spell.ChampionName, spell.Slot)));
                }
            }

            evade.Add("EVADEMETHOD", new Slider("Evade Method: (0 : Near Turret | 1 : Less Enemies | 2 : Auti) ", 2, 0, 2));
            evade.Add("EVADEENABLE", new CheckBox("Enabled", true));

            if (ObjectManager.Player.CharData.BaseSkinName == "Morgana")
            {
                evade.AddSeparator();
                evade.AddGroupLabel("Ally Shielding");
                foreach (var ally in HeroManager.Allies)
                {
                    if (!ally.IsMe)
                    {
                        evade.Add("shield" + ally.ChampionName, new CheckBox("Shield " + ally.ChampionName, false));
                    }
                }

                evade.Add("SHIELDENABLED", new CheckBox("Enabled", true));
                evade.AddSeparator();
            }
            
            _evade = evade;
            m_evade_thread = new Thread(new ThreadStart(EvadeThread));
            m_evade_thread.Start();
            Game.OnUpdate += Game_OnUpdate;
            AIHeroClient.OnProcessSpellCast += AIHeroClient_OnProcessSpellCast;
            
            Chat.Print("<font color='#ff3232'>SCommon: </font><font color='#d4d4d4'>Evader loaded for champion {0} !</font>", ObjectManager.Player.ChampionName);
        }

        /// <summary>
        /// Sets evade spell
        /// </summary>
        /// <param name="spl">The evade spell</param>
        public void SetEvadeSpell(LeagueSharp.Common.Spell spl)
        {
            EvadeSpell = spl;
        }

        /// <summary>
        /// OnProcessSpellCast Event which detects skillshots
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The args.</param>
        private void AIHeroClient_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (evade != null && getCheckBoxItem("EVADEENABLE") && sender.Type == GameObjectType.AIHeroClient)
            {
                if (sender.IsEnemy)
                {
                    Vector2 sender_pos = sender.ServerPosition.To2D();

                    if (getCheckBoxItem(String.Format("SCommon.Evade.Spell.{0}", args.SData.Name)))
                    {
                        var spell = SpellDatabase.EvadeableSpells.FirstOrDefault(p => p.SpellName == args.SData.Name);
                        if (spell != null)
                        {
                            if (spell.IsSkillshot)
                            {
                                DetectedSpellData dcspell = m_spell_pool.GetObject();
                                dcspell.Set(spell, sender_pos, args.End.To2D(), sender, args);
                                m_spell_queue.Enqueue(dcspell);
                            }
                        }
                    }

                    //to do: ally check
                    if (args.Target != null && args.Target.IsMe && args != null && args.SData != null && !args.SData.IsAutoAttack() && sender.IsChampion())
                    {
                        if (sender.LSGetSpellDamage(ObjectManager.Player, args.SData.Name) * 2 >= ObjectManager.Player.Health)
                            OnSpellHitDetected(sender_pos, ObjectManager.Player);
                    }
                }
            }
        }

        /// <summary>
        /// The Game.OnUpdate event
        /// </summary>
        /// <param name="args">The args.</param>
        private void Game_OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || args == null)
                return;

            EvadeData edata;
            if (m_evade_queue.TryDequeue(out edata))
            {
                Console.WriteLine("try evade with data Targetted: {0}, SelfCast: {1}, TargetName: {2}", edata.IsTargetted, edata.IsSelfCast, edata.Target.Name);
                if (EvadeSpell.IsReady())
                {
                    if ((ObjectManager.Player.CharData.BaseSkinName == "Zed" && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "ZedShadowDash") || ObjectManager.Player.CharData.BaseSkinName != "Zed")
                    {
                        if (edata.IsSelfCast)
                            EvadeSpell.Cast();
                        else if (edata.IsTargetted && edata.Target != null)
                            EvadeSpell.Cast(edata.Target);
                        else
                            EvadeSpell.Cast(edata.Position);
                    }
                }
            }
        }

        /// <summary>
        /// The callback when called spell hit detected
        /// </summary>
        /// <param name="direction">The skillshot direction.</param>
        /// <param name="target">The target.</param>
        public void OnSpellHitDetected(Vector2 direction, AIHeroClient target)
        {
            EvadeData edata;

            Vector2 evade_direction = direction.Perpendicular();
            Vector2 evade_pos = ObjectManager.Player.ServerPosition.To2D() + evade_direction * EvadeSpell.Range;

            bool position_needed = SpecialMethod.HasFlag(EvadeMethods.Dash) || SpecialMethod.HasFlag(EvadeMethods.Blink);

            if (position_needed)
            {
                switch (getSliderItem("EVADEMETHOD"))
                {
                    case 0: //near turret
                        CorrectNearTurret(ref evade_pos, evade_direction);
                        break;

                    case 1: //less enemies
                        CorrectLessEnemies(ref evade_pos, evade_direction);
                        break;

                    case 2: //both
                        if (!CorrectLessEnemies(ref evade_pos, evade_direction))
                            CorrectNearTurret(ref evade_pos, evade_direction);
                        break;
                }
            }

            edata = new EvadeData
                (evade_pos, 
                ObjectManager.Player.CharData.BaseSkinName == "Morgana" || ObjectManager.Player.CharData.BaseSkinName == "Lissandra",
                ObjectManager.Player.CharData.BaseSkinName == "Sivir" || ObjectManager.Player.CharData.BaseSkinName == "Nocturne" || ObjectManager.Player.CharData.BaseSkinName == "Vladimir", 
                target);

            m_evade_queue.Enqueue(edata);
        }

        /// <summary>
        /// The thread which detects spell hits
        /// </summary>
        public void EvadeThread()
        {
            //TO DO: evade with targetted spells (jax, irelia, master etc..)
            DetectedSpellData dcspell;
            while (true)
            {
                try
                {
                    if (m_spell_queue.TryDequeue(out dcspell))
                    {
                        Vector2 my_pos = ObjectManager.Player.ServerPosition.To2D();
                        Vector2 sender_pos = dcspell.StartPosition;
                        Vector2 end_pos = dcspell.EndPosition;
                        Vector2 direction = (end_pos - sender_pos).Normalized();
                        if (sender_pos.Distance(end_pos) > dcspell.Spell.Range)
                            end_pos = sender_pos + direction * dcspell.Spell.Range;

                        Geometry.Polygon my_hitbox = SCommon.Maths.ClipperWrapper.DefineRectangle(my_pos - 60, my_pos + 60, 60);
                        Geometry.Polygon spell_hitbox = null;

                        if (dcspell.Spell.IsSkillshot)
                        {
                            if (dcspell.Spell.Type == SkillshotType.SkillshotLine)
                                spell_hitbox = SCommon.Maths.ClipperWrapper.DefineRectangle(sender_pos, end_pos, dcspell.Spell.Radius);
                            else if (dcspell.Spell.Type == SkillshotType.SkillshotCircle)
                                spell_hitbox = SCommon.Maths.ClipperWrapper.DefineCircle(end_pos, dcspell.Spell.Radius);
                            else if (dcspell.Spell.Type == SkillshotType.SkillshotCone)
                                spell_hitbox = SCommon.Maths.ClipperWrapper.DefineSector(sender_pos, end_pos - sender_pos, dcspell.Spell.Radius * (float)Math.PI / 180, dcspell.Spell.Range);
                        }

                        //spells with arc
                        if (dcspell.Spell.IsArc)
                        {
                            float mul = (end_pos.Distance(sender_pos) / (dcspell.Spell.Range - 20.0f));
                            
                            spell_hitbox = new Geometry.Polygon(
                                SCommon.Maths.ClipperWrapper.DefineArc(sender_pos - dcspell.Spell.ArcData.Pos, end_pos, dcspell.Spell.ArcData.Angle * mul, dcspell.Spell.ArcData.Width, dcspell.Spell.ArcData.Height * mul),
                                SCommon.Maths.ClipperWrapper.DefineArc(sender_pos - dcspell.Spell.ArcData.Pos, end_pos, dcspell.Spell.ArcData.Angle * mul, dcspell.Spell.ArcData.Width, (dcspell.Spell.ArcData.Height + dcspell.Spell.ArcData.Radius) * mul),
                                spell_hitbox);
                        }

                        if (spell_hitbox != null)
                        {
                            if (SCommon.Maths.ClipperWrapper.IsIntersects(SCommon.Maths.ClipperWrapper.MakePaths(my_hitbox), SCommon.Maths.ClipperWrapper.MakePaths(spell_hitbox)))
                                OnSpellHitDetected(direction, ObjectManager.Player);
                            else
                            {
                                if (ObjectManager.Player.CharData.BaseSkinName == "Morgana" && getCheckBoxItem("SHIELDENABLED"))
                                {
                                    var allies = ObjectManager.Player.GetAlliesInRange(EvadeSpell.Range).Where(p => !p.IsMe && getCheckBoxItem("shield" + p.ChampionName));

                                    if (allies != null)
                                    {
                                        foreach (AIHeroClient ally in allies)
                                        {
                                            Vector2 ally_pos = ally.ServerPosition.To2D();
                                            Geometry.Polygon ally_hitbox = SCommon.Maths.ClipperWrapper.DefineRectangle(ally_pos, ally_pos + 60, 60);
                                            if (SCommon.Maths.ClipperWrapper.IsIntersects(SCommon.Maths.ClipperWrapper.MakePaths(ally_hitbox), SCommon.Maths.ClipperWrapper.MakePaths(spell_hitbox)))
                                            {
                                                OnSpellHitDetected(direction, ally);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        m_spell_pool.PutObject(dcspell);
                    }
                }
                catch
                {

                }
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Sets evade pos to near turret
        /// </summary>
        /// <param name="evade_pos">The raw evade pos.</param>
        /// <param name="direction">The skillshot direction</param>
        /// <returns></returns>
        private bool CorrectNearTurret(ref Vector2 evade_pos, Vector2 direction)
        {
            var turret = ObjectManager.Get<Obj_AI_Turret>().Where(p => p.IsAlly).MinOrDefault(q => q.ServerPosition.Distance(ObjectManager.Player.ServerPosition));
            if (turret != null)
            {
                if (turret.ServerPosition.To2D().Distance(evade_pos) > turret.ServerPosition.To2D().Distance(ObjectManager.Player.ServerPosition.To2D() - direction * EvadeSpell.Range))
                {
                    evade_pos = ObjectManager.Player.Position.To2D() - direction * EvadeSpell.Range;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sets evade pos to less enemies
        /// </summary>
        /// <param name="evade_pos">The raw evade pos.</param>
        /// <param name="direction">The skillshot direction</param>
        /// <returns></returns>
        private bool CorrectLessEnemies(ref Vector2 evade_pos, Vector2 direction)
        {
            if (HeroManager.Enemies.Count(p => p.ServerPosition.To2D().Distance(ObjectManager.Player.ServerPosition.To2D() + direction * EvadeSpell.Range) <= ObjectManager.Player.BasicAttack.CastRange) > HeroManager.Enemies.Count(p => p.ServerPosition.To2D().Distance(ObjectManager.Player.ServerPosition.To2D() - direction * EvadeSpell.Range) <= ObjectManager.Player.BasicAttack.CastRange))
            {
                    evade_pos = ObjectManager.Player.Position.To2D() - direction * EvadeSpell.Range;
                    return true;
            }

            return false;
        }
    }
}
