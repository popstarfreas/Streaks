using System;
using TShockAPI;

namespace Streaks
{
	public class Player
	{
		public readonly int index;
		public TSPlayer TsPlayer { get { return TShock.Players[index]; } }
		public Terraria.Player TSPlayer { get { return TShock.Players[index].TPlayer; } }
		public string Name { get { return TShock.Players[index].Name; } }
		public Player Killer;
		public int Streak = 0;
		public bool PendingStreakClear = false;

		public Player (int index)
		{
			this.index = index;
		}
	}
}

