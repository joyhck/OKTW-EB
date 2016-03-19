#region

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
                default:
                    return;
            }
        }
    }
}