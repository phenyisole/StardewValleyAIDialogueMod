using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIDialogueTestMod
{
    public class ModConfig
    {
        /// <summary>AI提供商: OpenAI, Claude, Gemini, DashScope, Custom</summary>
        public string AIProvider { get; set; } = "DashScope";

        /// <summary>API密钥</summary>
        public string APIKey { get; set; } = "sk-f10919448ae44b43a1c7ed1d789fb859";

        /// <summary>模型名称</summary>
        public string ModelName { get; set; } = "qwen-plus";

        /// <summary>自定义API端点 (如果使用Custom提供商)</summary>
        public string CustomAPIEndpoint { get; set; } = "https://api.openai.com/v1/chat/completions";

        /// <summary>是否启用调试日志</summary>
        public bool DebugMode { get; set; } = true;

        /// <summary>AI回复最大长度</summary>
        public int MaxResponseLength { get; set; } = 200;

        /// <summary>温度参数 (0-1)，越高越随机</summary>
        public float Temperature { get; set; } = 0.9f;

        /// <summary>核采样参数 (0-1)</summary>
        public float TopP { get; set; } = 0.95f;
    }
}
