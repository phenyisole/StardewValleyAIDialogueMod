using System.Collections.Generic;
using System.Linq;

namespace AIDialogueTestMod
{
    public static class DialogueHistoryManager
    {
        // 存储每个NPC的对话历史，Key是NPC名字
        private static readonly Dictionary<string, LinkedList<string>> Histories = new Dictionary<string, LinkedList<string>>();
        private const int MaxHistoryCount = 10;

        /// <summary>添加一条新的对话记录</summary>
        public static void AddHistory(string npcName, string dialogue)
        {
            if (!Histories.ContainsKey(npcName))
            {
                Histories[npcName] = new LinkedList<string>();
            }

            var history = Histories[npcName];
            history.AddLast(dialogue);

            // 保持历史记录不超过限制
            while (history.Count > MaxHistoryCount)
            {
                history.RemoveFirst();
            }
        }

        /// <summary>获取指定NPC的最近对话历史</summary>
        public static List<string> GetHistory(string npcName)
        {
            if (Histories.TryGetValue(npcName, out var history))
            {
                return history.ToList();
            }
            return new List<string>();
        }

        /// <summary>清空指定NPC的历史（可选，比如每天重置）</summary>
        public static void ClearHistory(string npcName)
        {
            if (Histories.ContainsKey(npcName))
            {
                Histories[npcName].Clear();
            }
        }
    }
}