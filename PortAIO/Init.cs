﻿#region

using System;
using EloBuddy;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;

#endregion

namespace PortAIO
{
    internal static class Init
    {
        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Initialize;
        }

        public static SCommon.PluginBase.Champion Champion;

        private static void Initialize(EventArgs args)
        {
            switch (ObjectManager.Player.ChampionName.ToLower())
            {
                case "aatrox": // BrianSharp's Aatrox
                    PortAIO.Champion.Aatrox.Program.Main();
                    break;
                case "ahri": // DZAhri
                    PortAIO.Champion.Ahri.Program.OnLoad();
                    break;
                case "akali": // Akali by xQx
                    PortAIO.Champion.Akali.Program.Main();
                    break;
                case "alistar": // El Alistar
                    PortAIO.Champion.Alistar.Program.OnGameLoad();
                    break;
                case "amumu": // Shine#
                    PortAIO.Champion.Amumu.Program.OnLoad();
                    break;
                case "anivia": // OKTW - Sebby - All Seeby champs go down here
                case "annie":
                case "ashe":
                    SebbyLib.Program.GameOnOnGameLoad();
                    break;
                case "azir": // Synx Auto Carry
                    Champion = new SAutoCarry.Champions.Azir();
                    break;
                case "bard": // Dreamless Wanderer
                    PortAIO.Champion.Bard.Program.OnLoad();
                    break;
                default:
                    return;
            }
        }
    }
}