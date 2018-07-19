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
	[ApiVersion(2, 1)]
	public class Streaks : TerrariaPlugin
	{
		public static readonly List<Player> Players = new List<Player>();
		public DateTime LastUpdate = DateTime.UtcNow;
		public Timer OnSecondUpdate;
        public static Config Config = new Config();
        public static DateTime[] Times = new DateTime[255];
        private Random Rand = new Random();

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
            Order = 14;
        }
		
		public override void Initialize()
		{
			ServerApi.Hooks.NetGreetPlayer.Register(this, GreetPlayer);
			ServerApi.Hooks.ServerLeave.Register(this, PlayerLeave);
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);

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

        private void OnInitialize(EventArgs args)
        {
            PvPController.PvPController.OnPlayerKill += new PvPController.PvPController.PlayerKillHandler(HandlePlayerKill);
        }

        private void HandlePlayerKill(object sender, PvPController.PlayerKillEventArgs args)
        {
            if (args.Victim == null || args.Killer == null)
            {
                return;
            }

            var killer = Players.FirstOrDefault(p => p != null && p.index == args.Killer.Index);
            var victim = args.Killer.Index != args.Victim.Index ? Players.FirstOrDefault(p => p != null && p.index == args.Victim.Index) : null;

            if (killer != null && victim != null)
            {
                if (victim.Streak >= 5)
                {
                    int es = Rand.Next(Config.EndStreakMessages.Count());
                    TSPlayer.All.SendMessage(String.Format( Config.EndStreakMessages[es], killer.Name, victim.Name, victim.Streak), 170, 0, 255);
                    victim.Streak = -1;
                }
                else
                {
                    if (victim.Streak <= 0)
                    {
                        --victim.Streak;
                        if (victim.Streak <= 10 && victim.Streak % 10 == 0)
                        {
                            int ds = Rand.Next(Config.DeathStreakMessages.Count());
                            TSPlayer.All.SendMessage(String.Format(Config.DeathStreakMessages[ds], victim.Name, Math.Abs(victim.Streak)), 8, 255, 131);
                        }
                    }
                    else
                    {
                        victim.Streak = -1;
                    }
                }

                killer.Streak = killer.Streak >= 0 ? killer.Streak + 1 : 1;
                if (killer.Streak >= 5 && killer.Streak % 5 == 0)
                {
                    int ks = Rand.Next(Config.KillStreakMessages.Count());
                    TSPlayer.All.SendMessage(String.Format(Config.KillStreakMessages[ks], killer.Name, killer.Streak), 255, 0, 251);
                }
            }
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
            if (TShock.Players[args.Who] == null) return;
            var exists = Players.Where(p => p != null && p.index == args.Who).Count() > 0;
			if (exists) {
				Players.RemoveAll (p => p != null && p.index == args.Who);
			}
			Players.Add (new Player (args.Who));
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
	}
}

