using System.Collections.Generic;

namespace AIDialogueTestMod
{
    public class DialogueResponse
    {
        public string Text { get; set; }
        public string[] Options { get; set; }
        // 存储每个选项对应的回复 key: 选项文本, value: 回复文本
        public Dictionary<string, string> OptionResponses { get; set; } = new Dictionary<string, string>();
    }
}
