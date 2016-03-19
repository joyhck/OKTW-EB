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
using PortAIO.Champion.Ahri;

namespace PortAIO.Utility.DZAhri
{
    static class Helpers
    {

        public static bool IsCharmed(this AIHeroClient target)
        {
            return target.HasBuff("AhriSeduce", true);
        }

        public static bool IsSafe(this Vector3 myVector)
        {
            var killableEnemy = myVector.GetEnemiesInRange(600f).Find(h => GetComboDamage(h) >= h.Health);
            var killableEnemyNumber = killableEnemy != null ? 1 : 0;
            var killableEnemyPlayer = ObjectManager.Player.GetEnemiesInRange(600f).Find(h => GetComboDamage(h) >= h.Health);
            var killableEnemyPlayerNumber = killableEnemyPlayer != null ? 1 : 0;

            if ((ObjectManager.Player.UnderTurret(true) && killableEnemyPlayerNumber == 0) || (myVector.UnderTurret(true) && killableEnemyNumber == 0))
            {
                return false;
            }
            if (myVector.CountEnemiesInRange(600f) == 1 || ObjectManager.Player.CountEnemiesInRange(600f) >= 1)
            {
                return true;
            }
            return myVector.CountEnemiesInRange(600f) - killableEnemyNumber - myVector.CountAlliesInRange(600f) + 1 >= 0;
        }

        public static float GetComboDamage(AIHeroClient enemy)
        {
            float totalDamage = 0;
            totalDamage += Program._spells[SpellSlot.Q].IsReady() ? Program._spells[SpellSlot.Q].GetDamage(enemy) : 0;
            totalDamage += Program._spells[SpellSlot.W].IsReady() ? Program._spells[SpellSlot.W].GetDamage(enemy) : 0;
            totalDamage += Program._spells[SpellSlot.E].IsReady() ? Program._spells[SpellSlot.E].GetDamage(enemy) : 0;
            totalDamage += (Program._spells[SpellSlot.R].IsReady() || (RStacks() != 0)) ? Program._spells[SpellSlot.R].GetDamage(enemy) : 0;
            return totalDamage;
        }
        public static bool IsRCasted()
        {
            return EloBuddy.Player.Instance.HasBuff("AhriTumble", true);
        }
        public static int RStacks()
        {
            var rBuff = ObjectManager.Player.Buffs.Find(buff => buff.Name == "AhriTumble");
            return rBuff != null ? rBuff.Count : 0;
        }
    }
}
