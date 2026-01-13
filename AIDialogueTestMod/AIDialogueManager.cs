using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AIDialogueTestMod
{
    public class AIDialogueManager
    {
        private readonly ModConfig config;
        private readonly IMonitor monitor;
        private readonly HttpClient httpClient;

        public AIDialogueManager(ModConfig config, IMonitor monitor)
        {
            this.config = config;
            this.monitor = monitor;
            this.httpClient = new HttpClient();
            this.httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>生成AI对话 (异步)</summary>
        public async Task<DialogueResponse> GenerateDialogueAsync(NPC npc, bool requestOptions)
        {
            // 检查 API Key 是否已配置
            if (string.IsNullOrWhiteSpace(config.APIKey) || config.APIKey.Contains("your-"))
            {
                return new DialogueResponse { Text = "我需要配置 API Key 才能说话。(请检查控制台日志)" };
            }

            try
            {
                // 收集游戏上下文信息
                var context = GatherGameContext(npc);

                // 调用AI API
                string rawResponse = await CallAIAPI(context, requestOptions);

                // 解析响应（分离文本和选项）
                return ParseAIResponseStructured(rawResponse);
            }
            catch (Exception ex)
            {
                this.monitor.Log($"AI对话生成失败: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        // 解析结构化响应
        private DialogueResponse ParseAIResponseStructured(string rawResponse)
        {
            var response = new DialogueResponse();
            
            // 检查是否包含选项标记
            if (rawResponse.Contains("[OPTIONS]"))
            {
                var parts = rawResponse.Split(new[] { "[OPTIONS]" }, StringSplitOptions.None);
                response.Text = parts[0].Trim();
                
                if (parts.Length > 1)
                {
                    var optionsBlock = parts[1].Trim();
                    var lines = optionsBlock.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    var optionsList = new List<string>();
                    
                    foreach (var line in lines)
                    {
                        string optLine = line.Trim();
                        if (string.IsNullOrWhiteSpace(optLine)) continue;

                        // 检查是否包含回复映射 (格式: "选项文本 => 回复文本")
                        if (optLine.Contains("=>"))
                        {
                            var mapping = optLine.Split(new[] { "=>" }, StringSplitOptions.None);
                            string optText = CleanOptionPrefix(mapping[0].Trim());
                            string replyText = mapping[1].Trim();
                            
                            optionsList.Add(optText);
                            response.OptionResponses[optText] = replyText;
                        }
                        else
                        {
                            string optText = CleanOptionPrefix(optLine);
                            optionsList.Add(optText);
                        }

                        if (optionsList.Count >= 4) break;
                    }
                    response.Options = optionsList.ToArray();
                }
            }
            else
            {
                response.Text = rawResponse;
            }

            return response;
        }

        private string CleanOptionPrefix(string opt)
        {
            // 去掉数字前缀 (如 "1. 选项" 或 "- 选项")
            if (opt.StartsWith("- ")) return opt.Substring(2).Trim();
            
            int dotIndex = opt.IndexOf('.');
            if (dotIndex > 0 && dotIndex < 4)
            {
                return opt.Substring(dotIndex + 1).Trim();
            }
            return opt;
        }

        /// <summary>生成AI对话 (同步阻塞，已弃用，仅兼容)</summary>
        public string GenerateDialogue(NPC npc)
        {
            return GenerateDialogueAsync(npc, false).GetAwaiter().GetResult()?.Text;
        }

        /// <summary>收集游戏上下文信息</summary>
        private GameContext GatherGameContext(NPC npc)
        {
            var player = Game1.player;
            var friendship = player.friendshipData.ContainsKey(npc.Name)
                ? player.friendshipData[npc.Name]
                : null;

            return new GameContext
            {
                NPCName = npc.Name,
                FriendshipPoints = friendship?.Points ?? 0,
                FriendshipLevel = friendship != null ? friendship.Points / 250 : 0,
                IsDating = friendship?.IsDating() ?? false,
                IsMarried = friendship?.IsMarried() ?? false,

                Season = Game1.currentSeason,
                Weather = Game1.isRaining ? "下雨" : (Game1.isSnowing ? "下雪" : "晴天"),
                DayOfWeek = Game1.dayOfMonth,
                WeekDay = Game1.Date.DayOfWeek.ToString(),
                Year = Game1.year,

                PlayerName = player.Name,
                PlayerGender = player.IsMale ? "男" : "女",
                PlayerMoney = player.Money,

                Location = Game1.currentLocation.Name,
                TimeOfDay = Game1.timeOfDay
            };
        }

        /// <summary>调用AI API</summary>
        private async Task<string> CallAIAPI(GameContext context, bool requestOptions)
        {
            string apiUrl = GetAPIUrl();

            // 构建提示词
            string systemPrompt = BuildSystemPrompt(context, requestOptions);

            // 根据不同提供商构建请求 - 使用传统if-else替代switch表达式
            object requestBody;
            if (config.AIProvider == "OpenAI")
            {
                requestBody = BuildOpenAIRequest(systemPrompt, context, requestOptions);
            }
            else if (config.AIProvider == "Claude")
            {
                requestBody = BuildClaudeRequest(systemPrompt, context);
            }
            else if (config.AIProvider == "Gemini")
            {
                requestBody = BuildGeminiRequest(systemPrompt, context);
            }
            else
            {
                requestBody = BuildOpenAIRequest(systemPrompt, context, requestOptions);
            }

            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
            };

            // 添加认证头
            if (config.AIProvider == "OpenAI" || config.AIProvider == "Custom" || config.AIProvider == "DashScope")
            {
                request.Headers.Add("Authorization", $"Bearer {config.APIKey}");
            }
            else if (config.AIProvider == "Claude")
            {
                request.Headers.Add("x-api-key", config.APIKey);
                request.Headers.Add("anthropic-version", "2023-06-01");
            }

            var response = await httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (config.DebugMode)
            {
                monitor.Log($"API响应: {responseContent}", LogLevel.Debug);
            }

            return ParseAPIResponse(responseContent);
        }

        /// <summary>构建系统提示词</summary>
        private string BuildSystemPrompt(GameContext ctx, bool requestOptions)
        {
            // 获取角色画像
            var profile = CharacterProfileRepository.GetProfile(ctx.NPCName);
            string exampleDialogues = string.Join("\n", profile.ExampleDialogues.Select(d => $"- {d}"));

            string prompt = $@"你是星露谷物语游戏中的NPC角色 {ctx.NPCName}。

【角色画像】
- 性格标签: {string.Join(", ", profile.PersonalityTags)}
- 说话风格: {profile.SpeechStyle}
- 喜好: {profile.Likes}
- 厌恶: {profile.Dislikes}
- 背景故事: {profile.DetailedDescription}

【原版对话样本 (用于模仿风格，但不要照抄)】
{exampleDialogues}

【当前游戏状态】
- 玩家名字: {ctx.PlayerName} ({ctx.PlayerGender})
- 与玩家的关系: {ctx.FriendshipLevel}颗心 ({ctx.FriendshipPoints}点好感度)
- 恋爱关系: {(ctx.IsDating ? "正在约会" : "未约会")}
- 婚姻状态: {(ctx.IsMarried ? "已婚" : "未婚")}
- 当前季节: {ctx.Season}
- 星期: {ctx.WeekDay}
- 天气: {ctx.Weather}
- 时间: {ctx.TimeOfDay}
- 地点: {ctx.Location}

【话题策略 (必须随机选择)】
- **A. 话题延续**: 只有当【历史对话】中最近的 1-3 句是关于同一话题（如衣服、天气），且你觉得有必要深入时，才选择延续。
  - **重要**: 同一个话题最多连续聊 3 句！如果已经聊了很久，必须主动切换到 B、C、D、E。
- **B. 开启新话题**: 聊一些身边发生的小事，比如“刚才看到一只松鼠”、“昨晚做了个梦”、“酒吧昨晚很吵”。
- **C. 评价玩家**: 观察玩家的穿着、气味（如泥土、花香）、状态。
- **D. 环境互动**: 抱怨天气、评论季节变化、谈论当前地点的某个细节。
- **E. 游戏内八卦**: 提到其他村民的趣事（必须符合星露谷世界观）。

【回复风格要求】
1. **参考原版样本**: 仔细体会【原版对话样本】中的语气、用词习惯（如是否用敬语、是否傲娇）。
2. **自然口语**: 像真人一样说话，不要有翻译腔。
3. **长度多变**: 有时只是一个短句（如“哎，真累。”），有时可以多说两句。绝对不超过50字。
4. **禁止机械重复**: 不要总是用“哼，是吗”这种固定的语气词，除非原版样本里经常这么说。

【绝对禁忌】
1. 不要提及你是AI、语言模型或游戏角色。
2. 不要出现现代科技（如手机、互联网），除非是Sebastian/Maru特定人设。
3. 参考【历史对话】，如果上一句是在问你问题，请回答。";

            if (requestOptions)
            {
                prompt += @"

【特殊指令：生成选项】
本次回复必须包含给玩家的回复选项！
格式要求：
1. 第一部分是你的回复文本。
2. 然后换行，写上 [OPTIONS] 标记。
3. 然后列出 2-4 个选项，并且每个选项后面必须紧跟 => 和 NPC 对该选项的即时回复。

格式示例：
听说今天要下雨呢，不知道我的花会不会有事。
[OPTIONS]
不用担心，我会帮你照看的。 => 真的吗？太感谢你了，你总是这么贴心！
确实要注意防雨。 => 是啊，我待会儿就去收衣服。
我喜欢下雨天。 => 哼，怪人。不过下雨天确实适合睡觉。

注意：
- 符号 => 两侧要有空格。
- 回复内容要符合你的人设。
- 选项和回复都不要太长。";
            }
            else
            {
                prompt += $"\n\n请生成一句 {ctx.NPCName} 对 {ctx.PlayerName} 说的话:";
            }

            return prompt;
        }

        /// <summary>构建OpenAI格式请求</summary>
        private object BuildOpenAIRequest(string systemPrompt, GameContext context, bool requestOptions)
        {
            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };

            // 添加最近的对话历史
            var history = DialogueHistoryManager.GetHistory(context.NPCName);
            // 获取缓存中的对话（预生成但未展示的），也作为历史参考，避免重复生成相似内容
            // 注意：因为缓存中可能存的是DialogueResponse对象，这里我们只取Text部分
            // 暂时缓存里存的是string，后续如果缓存也存Response对象，需要适配
            var cached = DialogueCacheManager.GetCachedDialogues(context.NPCName);
            
            // 调试日志：检查历史记录是否为空
            this.monitor.Log($"构建请求: 历史记录条数 = {history.Count}, 缓存预生成条数 = {cached.Count}", LogLevel.Debug);
            
            if (history.Count > 0 || cached.Count > 0)
            {
                // 将历史记录打包成一条 User 消息，明确告诉 AI 这是它之前说过的话
                string historyText = "【你之前的回复记录（作为上下文参考）】:\n";
                
                // 1. 真实历史
                foreach (var oldDialogue in history)
                {
                    historyText += $"- {oldDialogue}\n";
                }
                
                // 2. 缓存中待展示的对话 (也视为已存在，防止重复)
                foreach (var pendingDialogue in cached)
                {
                    historyText += $"- [待展示] {pendingDialogue}\n";
                }

                messages.Add(new { role = "user", content = historyText });
            }

            // 添加随机因子到用户输入中，防止缓存
            string randomFactor = DateTime.Now.Ticks.ToString();
            string userPrompt = $"[System: Random Seed {randomFactor}]\n现在{context.PlayerName}走过来和你打招呼。请根据【话题策略】生成回复。";
            if (requestOptions)
            {
                userPrompt += "\n[System: Please provide dialogue options!]";
            }
            messages.Add(new { role = "user", content = userPrompt });

            return new
            {
                model = config.ModelName,
                messages = messages.ToArray(),
                temperature = config.Temperature,
                top_p = config.TopP,
                max_tokens = config.MaxResponseLength + (requestOptions ? 100 : 0) // 如果请求选项，增加Token限额
            };
        }

        /// <summary>构建Claude格式请求</summary>
        private object BuildClaudeRequest(string systemPrompt, GameContext context)
        {
            // 构建包含历史记录的消息
            string fullContent = systemPrompt;
            var history = DialogueHistoryManager.GetHistory(context.NPCName);
            
            if (history.Count > 0)
            {
                fullContent += "\n\n【之前的对话记录】\n";
                foreach (var h in history)
                {
                    fullContent += $"- {context.NPCName}: {h}\n";
                }
            }

            fullContent += $"\n\n现在{context.PlayerName}走过来和你打招呼";

            return new
            {
                model = config.ModelName,
                max_tokens = config.MaxResponseLength,
                messages = new[]
                {
                    new { role = "user", content = fullContent }
                }
            };
        }

        /// <summary>构建Gemini格式请求</summary>
        private object BuildGeminiRequest(string systemPrompt, GameContext context)
        {
            // 构建包含历史记录的文本
            string text = systemPrompt;
            var history = DialogueHistoryManager.GetHistory(context.NPCName);

            if (history.Count > 0)
            {
                text += "\n\n【之前的对话记录 (请避免重复)】\n";
                foreach (var h in history)
                {
                    text += $"- {h}\n";
                }
            }

            text += $"\n\n现在{context.PlayerName}走过来和你打招呼";

            return new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = text } } }
                },
                generationConfig = new
                {
                    temperature = config.Temperature,
                    maxOutputTokens = config.MaxResponseLength
                }
            };
        }

        /// <summary>解析API响应</summary>
        private string ParseAPIResponse(string responseJson)
        {
            try
            {
                dynamic response = JsonConvert.DeserializeObject(responseJson);

                // OpenAI格式
                if (response.choices != null)
                {
                    return response.choices[0].message.content.ToString().Trim();
                }
                // Claude格式
                else if (response.content != null)
                {
                    return response.content[0].text.ToString().Trim();
                }
                // Gemini格式
                else if (response.candidates != null)
                {
                    return response.candidates[0].content.parts[0].text.ToString().Trim();
                }

                return "对话解析失败";
            }
            catch
            {
                return "AI响应格式错误";
            }
        }

        /// <summary>获取API URL</summary>
        private string GetAPIUrl()
        {
            // 使用传统if-else替代switch表达式
            if (config.AIProvider == "OpenAI")
            {
                return "https://api.openai.com/v1/chat/completions";
            }
            else if (config.AIProvider == "DashScope")
            {
                return "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";
            }
            else if (config.AIProvider == "Claude")
            {
                return "https://api.anthropic.com/v1/messages";
            }
            else if (config.AIProvider == "Gemini")
            {
                return $"https://generativelanguage.googleapis.com/v1beta/models/{config.ModelName}:generateContent?key={config.APIKey}";
            }
            else if (config.AIProvider == "Custom")
            {
                return config.CustomAPIEndpoint;
            }
            else
            {
                return config.CustomAPIEndpoint;
            }
        }
    }
}