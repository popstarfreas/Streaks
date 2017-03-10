using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace Streaks
{
    public class Config
    {
        public string[] DeathMessages;
        public string[] KillStreakMessages;
        public string[] DeathStreakMessages;
        public string[] EndStreakMessages;

        public void Write(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static Config Read(string path)
        {
            if (!File.Exists(path))
            {
                Config.WriteTemplates(path);
            }
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
        }

        public static void WriteTemplates(string file)
        {
            var Conf = new Config();
            Conf.DeathMessages = new string[] { "{0} has been slain by {1}'s ego.", "{0} failed to dodge {1}.", "{1} cut {0} in half.", "{1} wiped {0} off the floor." };
            Conf.KillStreakMessages = new string[] { "{0} is now on a {1} Killstreak." };
            Conf.DeathStreakMessages = new string[] { "{0} is having a bad day and has died {1} times!" };
            Conf.EndStreakMessages = new string[] { "{0} has ended {1}'s {2} Killstreak." };
            Conf.Write(file);
        }
    }
}
