#region

using System;
using EloBuddy;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;

#endregion

namespace BerbsicAIO
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
                    BerbsicAIO.Champion.Aatrox.Program.Main();
                    break;
                default:
                    return;
            }
        }
    }
}