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

namespace SCommon.Orbwalking
{
    public class Drawings
    {
        /// <summary>
        /// The orbwalker instance.
        /// </summary>
        private Orbwalker m_Instance;

        /// <summary>
        /// Drawings constructor
        /// </summary>
        /// <param name="instance">The orbwalker instance.</param>
        public Drawings(Orbwalker instance)
        {
            m_Instance = instance;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        /// <summary>
        /// Drawing.OnDraw event.
        /// </summary>
        /// <param name="args">The args.</param>
        private void Drawing_OnDraw(EventArgs args)
        {
            if (m_Instance.Configuration.SelfAACircle)
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Utility.GetAARange(), Color.Red, m_Instance.Configuration.LineWidth);

            if(m_Instance.Configuration.EnemyAACircle)
            {
                foreach (var target in HeroManager.Enemies.FindAll(target => target.IsValidTarget(1200)))
                    Render.Circle.DrawCircle(target.Position, Utility.GetAARange(target), Color.Blue, m_Instance.Configuration.LineWidth);
            }

            if (m_Instance.Configuration.HoldZone)
                Render.Circle.DrawCircle(ObjectManager.Player.Position, m_Instance.Configuration.HoldAreaRadius, Color.Black, m_Instance.Configuration.LineWidth);

            if(m_Instance.Configuration.LastHitMinion)
            {
                foreach(var minion in MinionManager.GetMinions(1200))
                {
                    if (Damage.Prediction.IsLastHitable(minion))
                        Render.Circle.DrawCircle(minion.Position, minion.BoundingRadius * 2, Color.Silver, m_Instance.Configuration.LineWidth);
                }
            }
        }
    }
}
