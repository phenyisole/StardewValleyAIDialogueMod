using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIDialogueTestMod
{
    public class GameContext
    {
        // NPC信息
        public string NPCName { get; set; }
        public int FriendshipPoints { get; set; }
        public int FriendshipLevel { get; set; }
        public bool IsDating { get; set; }
        public bool IsMarried { get; set; }

        // 游戏时间
        public string Season { get; set; }
        public string Weather { get; set; }
        public int DayOfWeek { get; set; }
        public string WeekDay { get; set; }
        public int Year { get; set; }
        public int TimeOfDay { get; set; }

        // 玩家信息
        public string PlayerName { get; set; }
        public string PlayerGender { get; set; }
        public int PlayerMoney { get; set; }

        // 位置
        public string Location { get; set; }
    }
}
