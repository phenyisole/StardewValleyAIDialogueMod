AI对话模组 - 星露谷物语版 - 1.2
https://www.nexusmods.com/stardewvalley/mods/41069
本模组集成了大语言模型(LLM)，让NPC能够根据游戏环境、好感度以及性格设定，生成无限且生动的对话内容。

:: 安装说明 ::
• 将 "AIDialogueTestMod" 文件夹解压放入你的星露谷物语 "Mods" 文件夹下。
• 运行一次游戏以生成 config.json 配置文件。

:: 配置说明 ::
你可以在模组目录下的 config.json 文件中配置 AI 提供商和 API 密钥。
使用任意文本编辑器打开该文件并填入你的 Key。

配置示例：
 { 
   "AIProvider": "DashScope", 
   "APIKey": "sk-xxxxxxx", // 你的阿里百炼或 OpenAI Key
   "ModelName": "qwen-plus", // 推荐模型:deepseekv3
   "CustomAPIEndpoint": "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions",
   "DebugMode": true, 
   "MaxResponseLength": 200, 
   "Temperature": 0.9, 
   "TopP": 0.95 
 }

:: 常见问题 ::
• 若出现对话卡在“正在思考中...”或“加载中”的情况，请直接关闭对话框，然后重新与NPC对话即可。
• 互动选项大概有40%的几率触发，如果没出现，请耐心听NPC说完话。

:: 功能特性 ::
• 基于上下文（天气、季节、关系）生成无限且独特的对话
• 互动对话选项（约40%几率），让你能决定说什么
• 后台预生成机制，最大程度减少卡顿
• 忠实于游戏设定的角色性格还原

:: 需求 ::
• 星露谷物语 1.6+
• SMAPI 4.0+
• 稳定的网络连接

:: 注意事项 ::
• 首次生成可能会有 1-2 秒的延迟，后续会变快。
• API 调用可能会产生费用，具体取决于你使用的服务商。
