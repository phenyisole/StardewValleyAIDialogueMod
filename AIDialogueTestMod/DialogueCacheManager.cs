using System.Collections.Generic;
using System.Linq;

namespace AIDialogueTestMod
{
    public static class DialogueCacheManager
    {
        // 存储每个NPC的预生成对话缓存
        private static readonly Dictionary<string, Queue<DialogueResponse>> Cache = new Dictionary<string, Queue<DialogueResponse>>();
        private const int MaxCacheSize = 5; // 增加缓存上限，确保随时有货

        /// <summary>获取缓存中的对话，如果没有则返回null</summary>
        public static DialogueResponse TryGetDialogue(string npcName)
        {
            if (Cache.ContainsKey(npcName) && Cache[npcName].Count > 0)
            {
                return Cache[npcName].Dequeue();
            }
            return null;
        }

        /// <summary>添加对话到缓存</summary>
        public static void AddDialogue(string npcName, DialogueResponse dialogue)
        {
            if (!Cache.ContainsKey(npcName))
            {
                Cache[npcName] = new Queue<DialogueResponse>();
            }

            // 如果缓存已满，就不加了
            if (Cache[npcName].Count < MaxCacheSize)
            {
                Cache[npcName].Enqueue(dialogue);
            }
        }

        /// <summary>检查NPC是否需要补充缓存</summary>
        public static bool NeedsRefill(string npcName)
        {
            if (!Cache.ContainsKey(npcName)) return true;
            return Cache[npcName].Count < MaxCacheSize;
        }

        /// <summary>获取缓存中的所有对话（用于构建上下文，防止重复）</summary>
        public static List<string> GetCachedDialogues(string npcName)
        {
            if (Cache.ContainsKey(npcName))
            {
                // 只返回文本部分用于上下文
                return Cache[npcName].Select(d => d.Text).ToList();
            }
            return new List<string>();
        }

        /// <summary>清空指定NPC的缓存 (当做出选择时)</summary>
        public static void Clear(string npcName)
        {
            if (Cache.ContainsKey(npcName))
            {
                Cache[npcName].Clear();
            }
        }

        /// <summary>清空所有缓存 (换天时)</summary>
        public static void ClearAll()
        {
            Cache.Clear();
        }
    }
}