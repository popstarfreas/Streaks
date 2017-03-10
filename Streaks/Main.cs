using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace Streaks
{
	[ApiVersion(2, 0)]
	public class Streaks : TerrariaPlugin
	{
		public static readonly List<Player> Players = new List<Player>();
		public DateTime LastUpdate = DateTime.UtcNow;
		public Timer OnSecondUpdate;
        public static Config Config = new Config();
        public static DateTime[] Times = new DateTime[255];

        public override string Author
		{
			get
			{
				return "popstarfreas";
			}
		}

		public override string Description
		{
			get
			{
				return "Adds Streaks";
			}
		}

		public override string Name
		{
			get
			{
				return "Streaks";
			}
		}

		public override Version Version
		{
			get
			{
				return new Version(1, 2, 0);
			}
		}

		public Streaks(Main game) : base(game)
        {
            Order = 1;
        }
		
		public override void Initialize()
		{
			ServerApi.Hooks.NetGreetPlayer.Register(this, GreetPlayer);
			ServerApi.Hooks.ServerLeave.Register(this, PlayerLeave);
			ServerApi.Hooks.NetGetData.Register(this, GetData);

			GetDataHandlers.InitGetDataHandler();

			TShockAPI.Commands.ChatCommands.Add (new Command ("streak.check", CheckStreakCommand, "streak"));
            TShockAPI.Commands.ChatCommands.Add(new Command("streak.reload", Reload, "streaksreload"));
            OnSecondUpdate = new Timer (1000);
			OnSecondUpdate.Enabled = true;
			OnSecondUpdate.Elapsed += SecondUpdate;

            string path = Path.Combine(TShock.SavePath, "Streaks.json");
            if (!File.Exists(path))
                Config.WriteTemplates(path);
            Config = Config.Read(path);
        }

		private void SecondUpdate(object sender, ElapsedEventArgs args)
		{
			lock (Players) {
				foreach (var player in Players.Where(p => p != null)) {
					if (player != null) {
						if (player.PendingStreakClear) {
							player.PendingStreakClear = false;
							player.Streak = 0;
							player.TsPlayer.SendMessage ("You were out of PvP too long, so your Streak was reset!", Color.Yellow);
						}

						if (!player.TSPlayer.hostile && player.Streak >= 1) {
							player.PendingStreakClear = true;
						}
					}
				}
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				ServerApi.Hooks.NetGreetPlayer.Deregister (this, GreetPlayer);
				ServerApi.Hooks.ServerLeave.Deregister (this, PlayerLeave);
                ServerApi.Hooks.NetGetData.Deregister(this, GetData);
                base.Dispose (disposing);
			}
        }
        void Reload(CommandArgs e)
        {
            string path = Path.Combine(TShock.SavePath, "Streaks.json");
            if (!File.Exists(path))
                Config.WriteTemplates(path);
            Config = Config.Read(path);
            e.Player.SendSuccessMessage("Reloaded Streaks config.");
        }

        private void GreetPlayer(GreetPlayerEventArgs args)
		{
			if (!args.Handled) {
				var exists = Players.Where(p => p != null && p.index == args.Who).Count() > 0;
				if (exists) {
					Players.RemoveAll (p => p != null && p.index == args.Who);
				}
				Players.Add (new Player (args.Who));
			}
		}

		private void PlayerLeave(LeaveEventArgs args)
		{
			Players.RemoveAll (p => p != null && p.index == args.Who);
		}

		private void CheckStreakCommand(CommandArgs args)
		{
			var player = Players.FirstOrDefault (p => p != null && p.index == args.Player.Index);

			if (player == null)
				return;

			player.TsPlayer.SendMessage (String.Format("You are on a {0} {1}streak.", Math.Abs(player.Streak), player.Streak > -1 ? "Kill" : "Death"), Color.Yellow);
		}

		private void GetData(GetDataEventArgs args)
		{
			var type = args.MsgID;
			var player = TShock.Players[args.Msg.whoAmI];

			if (player == null)
			{
				args.Handled = true;
				return;
			}

			if (!player.ConnectionAlive)
			{
				args.Handled = true;
				return;
			}

			using (var data = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length))
			{
				try
				{
					if (GetDataHandlers.HandlerGetData(type, player, data))
						args.Handled = true;
				}
				catch (Exception ex)
				{
					TShock.Log.ConsoleError(ex.ToString());
				}
			}
		}
	}
}

