﻿﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AIDialogueTestMod
{
    public class ModEntry : Mod
    {
        private ModConfig Config;
        private AIDialogueManager DialogueManager;

        public override void Entry(IModHelper helper)
        {
            // 加载配置
            this.Config = this.Helper.ReadConfig<ModConfig>();

            // 初始化AI对话管理器
            this.DialogueManager = new AIDialogueManager(this.Config, this.Monitor);

            // 订阅事件
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.Player.Warped += this.OnWarped;

            this.Monitor.Log("AI对话系统已加载!", LogLevel.Info);
            this.Monitor.Log($"API提供商: {this.Config.AIProvider}", LogLevel.Info);
        }

        private int tickCounter = 0;
        private bool isGenerating = false;

        // 切换地图时立即检查
        private void OnWarped(object sender, WarpedEventArgs e)
        {
            // 切换地图后，立即重置计数器，加速生成
            tickCounter = 100; 
        }

        private long thinkingStartTime = 0;
        private Task<DialogueResponse> activeGenerationTask = null;
        private NPC activeGenerationNpc = null;

        // 后台生成循环
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            // 1. 处理“思考中”提示框的自动关闭
            if (thinkingStartTime > 0)
            {
                // TotalMilliseconds 是 double 类型，需要强制转换
                long elapsed = (long)Game1.currentGameTime.TotalGameTime.TotalMilliseconds - thinkingStartTime;
                if (elapsed > 1000) // 超过1秒
                {
                    // 如果当前还在显示思考中，就关闭它
                    if (Game1.activeClickableMenu is StardewValley.Menus.DialogueBox db && 
                        db.getCurrentString() == "正在思考中...")
                    {
                        Game1.exitActiveMenu();
                    }
                    thinkingStartTime = 0; // 只尝试关闭一次
                }
            }

            // 2. 检查主动生成的任务是否完成
            if (activeGenerationTask != null)
            {
                if (activeGenerationTask.IsCompleted)
                {
                    HandleActiveGenerationResult();
                }
            }

            // 3. 后台生成逻辑 (仅当没有主动任务时)
            else 
            {
                // 每30帧(约0.5秒)检查一次，加快频率
                tickCounter++;
                if (tickCounter < 30) return;
                tickCounter = 0;

                // 如果正在生成中，先跳过
                if (isGenerating) return;

                // 获取当前地点的所有NPC
                if (Game1.currentLocation == null) return;
                
                var npcs = Game1.currentLocation.characters.Where(n => !n.IsMonster).ToList();

                // 简单的调度策略：每次tick只为一个NPC生成
                foreach (var npc in npcs)
                {
                    // 检查是否需要生成 (必须满足缓存不足 + 冷却时间到了)
                    // 注意：这里我们还需要检查是否应该生成带选项的对话？
                    // 暂时后台生成不带选项，选项只在主动交互时触发
                    if (DialogueCacheManager.NeedsRefill(npc.Name))
                    {
                        GenerateBackgroundDialogue(npc);
                        break; 
                    }
                }
            }
        }

        private async void HandleActiveGenerationResult()
        {
            try
            {
                var response = await activeGenerationTask;
                activeGenerationTask = null; // 清除任务状态
                
                if (response != null && !string.IsNullOrWhiteSpace(response.Text))
                {
                    // 加入历史
                    DialogueHistoryManager.AddHistory(activeGenerationNpc.Name, response.Text);
                    
                    // 显示对话
                    // 如果有选项，使用特殊逻辑
                    if (response.Options != null && response.Options.Length > 0)
                    {
                        CreateQuestionDialogue(activeGenerationNpc, response);
                        // 记录这次是提问
                        DialogueStateManager.RecordQuestionDialogue(activeGenerationNpc.Name);
                    }
                    else
                    {
                        // 普通对话
                        activeGenerationNpc.CurrentDialogue.Clear();
                        activeGenerationNpc.CurrentDialogue.Push(new StardewValley.Dialogue(activeGenerationNpc, null, response.Text));
                        Game1.drawDialogue(activeGenerationNpc);
                        
                        // 记录这次是普通对话
                        DialogueStateManager.RecordNormalDialogue(activeGenerationNpc.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"主动生成失败: {ex.Message}", LogLevel.Error);
                activeGenerationTask = null;
            }
        }

        private void CreateQuestionDialogue(NPC npc, DialogueResponse response)
        {
            var choices = new List<Response>();
            for (int i = 0; i < response.Options.Length; i++)
            {
                // Key格式: Answer_{Index}_{OptionText}
                choices.Add(new Response($"Answer_{i}", response.Options[i]));
            }

            Game1.currentLocation.createQuestionDialogue(
                response.Text, 
                choices.ToArray(), 
                (who, answerKey) => {
                    // 记录玩家选择
                    string chosenText = "Unknown";
                    string npcReply = null;

                    if (answerKey.StartsWith("Answer_"))
                    {
                        int index = int.Parse(answerKey.Split('_')[1]);
                        if (index < response.Options.Length)
                        {
                            chosenText = response.Options[index];
                            
                            // 尝试获取对应的NPC回复
                            if (response.OptionResponses != null && response.OptionResponses.ContainsKey(chosenText))
                            {
                                npcReply = response.OptionResponses[chosenText];
                            }
                        }
                    }
                    
                    this.Monitor.Log($"玩家选择了: {chosenText}", LogLevel.Debug);
                    DialogueHistoryManager.AddHistory(npc.Name, $"[玩家选择]: {chosenText}");
                    
                    // 清除缓存，确保下次对话反映选择
                    DialogueCacheManager.Clear(npc.Name);

                    // 如果有预生成的回复，立即显示
                    if (!string.IsNullOrEmpty(npcReply))
                    {
                        this.Monitor.Log($"[即时回复] {npcReply}", LogLevel.Debug);
                        DialogueHistoryManager.AddHistory(npc.Name, npcReply);
                        
                        npc.CurrentDialogue.Clear();
                        npc.CurrentDialogue.Push(new StardewValley.Dialogue(npc, null, npcReply));
                        Game1.drawDialogue(npc);
                    }
                }
            );
        }

        private async void GenerateBackgroundDialogue(NPC npc)
        {
            isGenerating = true;
            try
            {
                // 随机决定是否生成带选项的对话 (50%概率 + 冷却满足)
                // 这样缓存里就会混合普通对话和选项对话
                bool requestOptions = false;
                if (DialogueStateManager.CanGenerateQuestion(npc.Name))
                {
                    requestOptions = Game1.random.NextDouble() < 0.5;
                }

                var response = await this.DialogueManager.GenerateDialogueAsync(npc, requestOptions);
                
                if (response != null && !string.IsNullOrWhiteSpace(response.Text) && !response.Text.Contains("API Key"))
                {
                    DialogueCacheManager.AddDialogue(npc.Name, response);
                }
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"[后台] 生成失败: {ex.Message}", LogLevel.Warn);
            }
            finally
            {
                isGenerating = false;
            }
        }

        // 每天开始时重置对话次数限制
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            DialogueHistoryManager.ClearHistory("ALL"); // 清理历史的占位符，实际上可以保留
            DialogueCacheManager.ClearAll(); // 每天清空缓存，因为话题要更新
            this.Monitor.Log("已重置所有NPC对话缓存", LogLevel.Debug);
        }

        // 拦截对话交互
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            if (!e.Button.IsActionButton()) return;

            // 获取点击位置的瓦片坐标
            var grabTile = e.Cursor.GrabTile;
            
            // 查找点击位置的NPC (更精确的检测)
            var clickedNpc = Game1.currentLocation.characters
                .FirstOrDefault(npc => npc.Tile == grabTile);

            // 如果点击的是瓦片中心附近，也可以尝试通过包围盒检测（双重保障）
            if (clickedNpc == null)
            {
                clickedNpc = Game1.currentLocation.characters
                    .FirstOrDefault(npc =>
                    {
                        var box = npc.GetBoundingBox();
                        // 扩大一点检测范围
                        return box.Contains((int)grabTile.X * 64 + 32, (int)grabTile.Y * 64 + 32);
                    });
            }

            if (clickedNpc != null && !clickedNpc.IsMonster && clickedNpc.isVillager())
            {
                // 1. 优先尝试从缓存获取
                var cachedResponse = DialogueCacheManager.TryGetDialogue(clickedNpc.Name);
                if (cachedResponse != null)
                {
                    this.Monitor.Log($"[命中缓存] 为 {clickedNpc.Name} 使用预生成对话", LogLevel.Debug);
                    
                    // 加入历史
                    DialogueHistoryManager.AddHistory(clickedNpc.Name, cachedResponse.Text);
                    
                    // 显示 (区分普通对话和选项)
                    if (cachedResponse.Options != null && cachedResponse.Options.Length > 0)
                    {
                        this.Monitor.Log($"[缓存] 触发选项对话", LogLevel.Debug);
                        CreateQuestionDialogue(clickedNpc, cachedResponse);
                        DialogueStateManager.RecordQuestionDialogue(clickedNpc.Name);
                    }
                    else
                    {
                        clickedNpc.CurrentDialogue.Clear();
                        clickedNpc.CurrentDialogue.Push(new StardewValley.Dialogue(clickedNpc, null, cachedResponse.Text));
                        Game1.drawDialogue(clickedNpc);
                        DialogueStateManager.RecordNormalDialogue(clickedNpc.Name);
                    }
                }
                else
                {
                    // 2. 缓存未命中，启动异步生成任务
                    // 检查是否应该请求选项 (50%概率 + 冷却满足)
                    bool requestOptions = false;
                    if (DialogueStateManager.CanGenerateQuestion(clickedNpc.Name))
                    {
                        requestOptions = Game1.random.NextDouble() < 0.5;
                    }

                    this.Monitor.Log($"[实时生成] 为 {clickedNpc.Name} 请求对话 (选项: {requestOptions})", LogLevel.Debug);

                    // 显示思考中
                    Game1.drawObjectDialogue("正在思考中...");
                    thinkingStartTime = (long)Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
                    
                    // 启动任务
                    activeGenerationNpc = clickedNpc;
                    activeGenerationTask = this.DialogueManager.GenerateDialogueAsync(clickedNpc, requestOptions);
                }
            }
        }
    }
}
