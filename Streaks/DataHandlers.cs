using System;
using System.IO;
using System.IO.Streams;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using TShockAPI;
using TShockAPI.Net;

namespace Streaks
{
	internal delegate bool GetDataHandlerDelegate(GetDataHandlerArgs args);

	internal class GetDataHandlerArgs : EventArgs
	{
		public TSPlayer Player { get; private set; }
		public MemoryStream Data { get; private set; }

		public GetDataHandlerArgs(TSPlayer player, MemoryStream data)
		{
			Player = player;
			Data = data;
		}
	}

	internal static class GetDataHandlers
    {
        static Random rnd = new Random();
        private static Dictionary<PacketTypes, GetDataHandlerDelegate> _getDataHandlerDelegates;

		public static void InitGetDataHandler()
		{
			_getDataHandlerDelegates = new Dictionary<PacketTypes, GetDataHandlerDelegate>
			{
				{PacketTypes.PlayerKillMe, HandlePlayerKillMe},
				{PacketTypes.PlayerDamage, HandlePlayerDamage}
			};
		}

		public static bool HandlerGetData(PacketTypes type, TSPlayer player, MemoryStream data)
		{
			GetDataHandlerDelegate handler;
			if (_getDataHandlerDelegates.TryGetValue(type, out handler))
			{
				try
				{
					return handler(new GetDataHandlerArgs(player, data));
				}
				catch (Exception ex)
				{
					TShock.Log.Error(ex.ToString());
				}
			}
			return false;
		}

		private static bool HandlePlayerKillMe(GetDataHandlerArgs args)
		{
			if (args.Player == null) return false;
			var index = args.Player.Index;
			var id = args.Data.ReadByte();
			var direction = (byte)(args.Data.ReadByte() - 1);
			var dmg = args.Data.ReadInt16();
			var pvp = args.Data.ReadByte() == 1;
			var text = args.Data.ReadString();
			var player = Streaks.Players.FirstOrDefault(p => p != null && p.index == index);
			
			if (player == null)
				return false;
            
            if (player.Killer != null && player.Killer.TSPlayer.hostile) {
				if (player.Streak >= 5)
                {
                    int es = rnd.Next(Streaks.Config.EndStreakMessages.Count());
                    TSPlayer.All.SendMessage (String.Format (Streaks.Config.EndStreakMessages[es], player.Killer.Name, player.Name, player.Streak), 170, 0, 255);
					player.Streak = -1;
				} else {
					if (player.Streak <= 0) {
						--player.Streak;
						if (player.Streak <= 10 && player.Streak % 10 == 0)
                        {
                            int ds = rnd.Next(Streaks.Config.DeathStreakMessages.Count());
                            TSPlayer.All.SendMessage (String.Format (Streaks.Config.DeathStreakMessages[ds], player.Name, Math.Abs (player.Streak)), 8, 255, 131);
						}
					} else {
						player.Streak = -1;
					}
				}
				player.Killer.Streak = player.Killer.Streak >= 0 ? player.Killer.Streak + 1 : 1;
				if (player.Killer.Streak >= 5 && player.Killer.Streak % 5 == 0)
                {
                    int ks = rnd.Next(Streaks.Config.KillStreakMessages.Count());
                    TSPlayer.All.SendMessage (String.Format (Streaks.Config.KillStreakMessages[ks], player.Killer.Name, player.Killer.Streak), 255, 0, 251);
				}

				player.Killer = null;
			} else {
				player.Streak = -1;
			}

            return false;
		}

		private static bool HandlePlayerDamage(GetDataHandlerArgs args)
		{
			if (args.Player == null) return false;
			var index = args.Player.Index;
			var playerId = (byte) args.Data.ReadByte();
			args.Data.ReadByte();
			var damage = args.Data.ReadInt16();
			var crit = args.Data.ReadBoolean();
			args.Data.ReadByte();

			//player being attacked
			var player = Streaks.Players.FirstOrDefault(p => p != null && p.index == playerId);
			var attacker = Streaks.Players.FirstOrDefault(p => p != null && p.index == index);

			if (player == null || attacker == null) {
				if (player != null) {
					player.Killer = null;
				}
				return false;
			}

			if (player.index != attacker.index && player.TSPlayer.hostile && attacker.TSPlayer.hostile)
				player.Killer = attacker;
			return false;
		}
	}
}