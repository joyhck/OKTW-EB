﻿using System;
using System.Collections.Generic;
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
namespace SCommon.Packet
{
    public static class PacketHandler
    {
        /// <summary>
        /// The FunctionList class
        /// </summary>
        private class FunctionList : List<Action<byte[]>>
        {
            public FunctionList()
                : base()
            {

            }
        }

        private static FunctionList[] s_opcodeMap;

        /// <summary>
        /// Initializes PacketHandler class
        /// </summary>
        static PacketHandler()
        {
            s_opcodeMap = new FunctionList[256 * 256];
            for (int i = 0; i < 256 * 256; i++)
                s_opcodeMap[i] = new FunctionList();

            Game.OnProcessPacket += Game_OnProcessPacket;
        }

        /// <summary>
        /// Registers function to call when the opcode is received
        /// </summary>
        /// <param name="opcode">The opcode.</param>
        /// <param name="fn">The function.</param>
        public static void Register(ushort opcode, Action<byte[]> fn)
        {
            s_opcodeMap[opcode].Add(fn);
        }

        /// <summary>
        /// Unregisters the function
        /// </summary>
        /// <param name="opcode">The opcode.</param>
        /// <param name="fn">The unction.</param>
        public static void Unregister(ushort opcode, Action<byte[]> fn)
        {
            s_opcodeMap[opcode].Remove(fn);
        }

        /// <summary>
        /// Unregisters all functions of the opcode.
        /// </summary>
        /// <param name="opcode">The opcode.</param>
        public static void Clear(ushort opcode)
        {
            s_opcodeMap[opcode].Clear();
        }

        /// <summary>
        /// The event when called a packet received from the server.
        /// </summary>
        /// <param name="args">The args./param>
        private static void Game_OnProcessPacket(GamePacketEventArgs args)
        {
            foreach (var fn in s_opcodeMap[BitConverter.ToUInt16(args.PacketData, 0)])
            {
                if (fn != null)
                    fn(args.PacketData);
            }
        }
    }
}
