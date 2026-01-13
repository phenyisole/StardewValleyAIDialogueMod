using System.Collections.Generic;

namespace AIDialogueTestMod
{
    public static class DialogueStateManager
    {
        // 记录每个NPC距离上一次提问过了多少句对话
        // 初始值设为 100，保证第一次对话就有机会触发提问
        private static readonly Dictionary<string, int> DialoguesSinceLastQuestion = new Dictionary<string, int>();

        /// <summary>检查是否满足生成问题的条件 (至少间隔2句)</summary>
        public static bool CanGenerateQuestion(string npcName)
        {
            if (!DialoguesSinceLastQuestion.ContainsKey(npcName))
            {
                DialoguesSinceLastQuestion[npcName] = 100;
            }
            return DialoguesSinceLastQuestion[npcName] >= 2;
        }

        /// <summary>记录一次普通对话 (计数器+1)</summary>
        public static void RecordNormalDialogue(string npcName)
        {
            if (!DialoguesSinceLastQuestion.ContainsKey(npcName))
            {
                DialoguesSinceLastQuestion[npcName] = 0;
            }
            DialoguesSinceLastQuestion[npcName]++;
        }

        /// <summary>记录一次提问对话 (计数器归零)</summary>
        public static void RecordQuestionDialogue(string npcName)
        {
            DialoguesSinceLastQuestion[npcName] = 0;
        }
    }
}