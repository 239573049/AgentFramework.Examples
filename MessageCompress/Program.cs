using AgentFrameworkCore.Options;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Text.Json;
using static Microsoft.Agents.AI.ChatClientAgentOptions;

var openAiClient = new OpenAIClient(new ApiKeyCredential(Env.Instance["API_KEY"]!), new OpenAIClientOptions()
{
    Endpoint = new Uri(Env.Instance["ENDPOINT"]!),
});

var chatClient = openAiClient.GetChatClient(Env.Instance["MODEL"]!);

int maxInputToken = 6000;

var usageStore = new UsageStore(maxInputToken);

AIAgent agent = chatClient.CreateAIAgent(new ChatClientAgentOptions()
{
    Instructions = "You are a friendly assistant. Always address the user by their name.",
    ChatMessageStoreFactory = (messageContext) =>
    {
        return new AutoCompress(messageContext, chatClient.AsIChatClient(), usageStore);
    },
});

// 为对话创建一个新线程。
AgentThread thread = agent.GetNewThread();

Console.WriteLine(">> 开始你的对话");

var read = "请使用c#实现一个高性能的冒泡排序";
Console.WriteLine("User:");
Console.WriteLine(read);
// 调用代理并输出文本结果。
var result = await agent.RunAsync(read, thread);
Console.WriteLine("AI:");
Console.WriteLine(result.Text);

usageStore.AddInputTokens(result.Usage?.InputTokenCount ?? 0);


read = "在实现一个高性能的冒泡排序的基础上，请帮我实现一个快速排序算法";

Console.WriteLine("User:");
Console.WriteLine(read);
result = await agent.RunAsync(read, thread);
Console.WriteLine("AI:");
Console.WriteLine(result.Text);

usageStore.AddInputTokens(result.Usage?.InputTokenCount ?? 0);

read = "在提供一个Java的高性能冒泡排序";
Console.WriteLine("User:");
Console.WriteLine(read);
result = await agent.RunAsync(read, thread);
Console.WriteLine("AI:");
Console.WriteLine(result.Text);


usageStore.AddInputTokens(result.Usage?.InputTokenCount ?? 0);


read = "在提供一个py的高性能冒泡排序";
Console.WriteLine("User:");
Console.WriteLine(read);
result = await agent.RunAsync(read, thread);
Console.WriteLine("AI:");
Console.WriteLine(result.Text);


usageStore.AddInputTokens(result.Usage?.InputTokenCount ?? 0);


read = "在提供一个js的高性能冒泡排序";
Console.WriteLine("User:");
Console.WriteLine(read);
result = await agent.RunAsync(read, thread);
Console.WriteLine("AI:");
Console.WriteLine(result.Text);


usageStore.AddInputTokens(result.Usage?.InputTokenCount ?? 0);

read = "在提供一个go的高性能冒泡排序";
Console.WriteLine("User:");
Console.WriteLine(read);
result = await agent.RunAsync(read, thread);
Console.WriteLine("AI:");
Console.WriteLine(result.Text);


usageStore.AddInputTokens(result.Usage?.InputTokenCount ?? 0);


read = "然后实现一个完整的c#项目，包括基准测试";
Console.WriteLine("User:");
Console.WriteLine(read);
result = await agent.RunAsync(read, thread);
Console.WriteLine("AI:");
Console.WriteLine(result.Text);


usageStore.AddInputTokens(result.Usage?.InputTokenCount ?? 0);




class AutoCompress : ChatMessageStore
{
    private List<ChatMessage> messages = new();
    private ChatMessageStoreFactoryContext messageContext;
    private readonly IChatClient chatClient;
    private readonly UsageStore usageStore;
    private readonly string filePath = "messages.json";

    public AutoCompress(ChatMessageStoreFactoryContext messageContext, IChatClient chatClient, UsageStore usageStore)
    {
        this.messageContext = messageContext;
        this.chatClient = chatClient;
        this.usageStore = usageStore;
    }

    public override async Task AddMessagesAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        this.messages.AddRange(messages);
        await SaveToFileAsync(cancellationToken);
    }

    public override async Task<IEnumerable<ChatMessage>> GetMessagesAsync(CancellationToken cancellationToken = default)
    {
        await LoadFromFileAsync(cancellationToken);

        if (usageStore.IsInputTokenLimitExceeded() && messages.Count > 3)
        {
            Console.WriteLine("\n⚠️  Token limit exceeded, compressing conversation history...\n");

            // 保留系统消息
            var systemMessages = messages.Where(m => m.Role == ChatRole.System).ToList();

            // 保留最近的2轮对话（用户消息+助手消息）
            var recentMessages = messages.Where(m => m.Role != ChatRole.System)
                                        .TakeLast(2) // 最近1轮（1个用户消息 + 1个助手消息）
                                        .ToList();

            // 需要压缩的消息（排除系统消息和最近的消息）
            var messagesToCompress = messages.Where(m => m.Role != ChatRole.System)
                                            .Except(recentMessages)
                                            .ToList();

            if (messagesToCompress.Count > 0)
            {
                // 创建压缩提示 - 将历史消息作为上下文，用户消息包含压缩指令
                var compressionMessages = new List<ChatMessage>(messagesToCompress)
                {
                    new ChatMessage(ChatRole.User, """
                    Please compress the above conversation history into a concise summary.
                    
                    The summary should:
                    1. Retain key information and context
                    2. Record important decisions and outcomes
                    3. Preserve the final task requirements and goals
                    4. Use concise language, not exceeding 1/3 of the original length
                    
                    Provide ONLY the summary without any additional explanations.
                    """)
                };

                // 调用AI进行压缩
                var compressionAgent = chatClient.CreateAIAgent(instructions: """
                    You are a conversation summarization assistant. 
                    Your task is to analyze conversation history and create concise, accurate summaries.
                    Focus on extracting the most important information while maintaining context coherence.
                    """);
                var compressionResult = await compressionAgent.RunAsync(compressionMessages);

                var compressedSummary = compressionResult.Text ?? "Unable to generate summary";

                Console.WriteLine($"✅ Compressed {messagesToCompress.Count} messages into summary\n");

                // 重建消息列表：系统消息 + 压缩摘要 + 最近的消息
                var newMessages = new List<ChatMessage>();

                // 添加系统消息
                newMessages.AddRange(systemMessages);

                // 添加压缩后的摘要作为用户消息（提供历史对话上下文）
                newMessages.Add(new ChatMessage(ChatRole.User,
                    $"""
                    <conversation-history-summary>
                    [This is a summary of our previous conversation for context]
                    
                    {compressedSummary}
                    </conversation-history-summary>
                    """));

                // 添加最近的消息
                newMessages.AddRange(recentMessages);

                // 更新消息列表
                messages = newMessages;

                // 重置token计数器（因为已经压缩）
                usageStore.Reset();

                // 保存压缩后的消息
                await SaveToFileAsync(cancellationToken);

                Console.WriteLine($"📊 Messages after compression: {messages.Count}\n");
            }
        }

        return messages;
    }

    public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        return JsonSerializer.SerializeToElement(messages, jsonSerializerOptions ?? messageContext.JsonSerializerOptions);
    }

    private async Task SaveToFileAsync(CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(messages, messageContext.JsonSerializerOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    private async Task LoadFromFileAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(filePath))
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            messages = JsonSerializer.Deserialize<List<ChatMessage>>(json, messageContext.JsonSerializerOptions) ?? new();
        }
    }
}

public class UsageStore
{
    private long inputMaxTokens;

    private long currentInputTokens;

    private float threshold;

    public UsageStore(long inputMaxTokens, float threshold = 0.8f)
    {
        this.inputMaxTokens = inputMaxTokens;
        this.currentInputTokens = 0;
        this.threshold = threshold;
    }

    /// <summary>
    /// 添加输入的token数量
    /// </summary>
    public void AddInputTokens(long tokens)
    {
        Interlocked.Add(ref this.currentInputTokens, tokens);
    }

    /// <summary>
    /// 校验当前输入token数量是否超出限制
    /// </summary>
    public bool IsInputTokenLimitExceeded()
    {
        return this.currentInputTokens >= this.inputMaxTokens * this.threshold;
    }

    /// <summary>
    /// 重置token计数器
    /// </summary>
    public void Reset()
    {
        Interlocked.Exchange(ref this.currentInputTokens, 0);
    }
}