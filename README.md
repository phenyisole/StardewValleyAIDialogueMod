
---

# 🌾 AI 对话模组 - 星露谷物语版（v1.2）

> 本模组集成大语言模型（LLM），让 NPC 能根据**游戏环境、好感度、角色性格**等上下文，生成**无限、生动且个性化的对话内容**。

🔗 [Nexus Mods 页面](https://www.nexusmods.com/stardewvalley/mods/41069)

---

## ✅ 安装说明

1. 将 `AIDialogueTestMod` 文件夹解压到你的《星露谷物语》的 `Mods` 目录中。  
2. 启动一次游戏，模组将自动生成 `config.json` 配置文件。

---

## ⚙️ 配置说明

编辑模组目录下的 `config.json` 文件（使用任意文本编辑器），填入你的 AI 服务提供商信息和 API 密钥。

### 示例配置：
```json
{
  "AIProvider": "DashScope",
  "APIKey": "sk-xxxxxxx",
  "ModelName": "qwen-plus",
  "CustomAPIEndpoint": "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions",
  "DebugMode": true,
  "MaxResponseLength": 200,
  "Temperature": 0.9,
  "TopP": 0.95
}
```

> 💡 **推荐模型**：`deepseek-v3`、`qwen-plus`  

---

## 🎮 功能特性

- 🧠 **上下文感知对话**：结合天气、季节、节日、玩家关系等动态生成内容  
- 💬 **互动选项**：约 40% 概率触发多选对话，由你决定回应  
- 🎭 **角色性格还原**：忠实还原原作中每位 NPC 的说话风格与个性  
- ⚡ **后台预生成**：减少对话时卡顿，提升流畅体验  
- 🔄 **无限对话**：告别重复台词，每次交谈都独一无二！

---

## ❓ 常见问题

- **Q：对话卡在“正在思考中...”或“加载中”？**  
  A：直接关闭对话框，重新与 NPC 对话即可。

- **Q：为什么没有出现互动选项？**  
  A：互动选项触发概率约为 40%，若未出现，请耐心听完 NPC 的完整对话。

---

## 📋 系统需求

- 《星露谷物语》**1.6 或更高版本**
- **SMAPI 4.0+**
- **稳定的网络连接**（用于调用 AI 服务）

---

## ⚠️ 注意事项

- 首次生成对话可能有 **1–2 秒延迟**，后续响应会显著加快。
- 使用 AI 服务**可能产生费用**，具体取决于你所选的 API 提供商及用量。
- 请妥善保管你的 `APIKey`，避免泄露。

---

> 欢迎反馈建议与 Bug！

---
