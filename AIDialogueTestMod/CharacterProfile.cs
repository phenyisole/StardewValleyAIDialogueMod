using System.Collections.Generic;

namespace AIDialogueTestMod
{
    public class CharacterProfile
    {
        /// <summary>角色名</summary>
        public string Name { get; set; }

        /// <summary>性格关键词 (e.g. 傲娇, 运动狂)</summary>
        public string[] PersonalityTags { get; set; }

        /// <summary>说话风格 (e.g. 使用大量网络用语, 语气正式)</summary>
        public string SpeechStyle { get; set; }

        /// <summary>详细的角色背景和行为准则</summary>
        public string DetailedDescription { get; set; }

        /// <summary>喜好 (用于对话参考)</summary>
        public string Likes { get; set; }

        /// <summary>厌恶 (用于对话参考)</summary>
        public string Dislikes { get; set; }

        /// <summary>经典原版对话样本 (用于风格参考)</summary>
        public string[] ExampleDialogues { get; set; }
    }
}