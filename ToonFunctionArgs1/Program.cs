using System.ClientModel;
using AgentFrameworkCore.Options;
using AIDotNet.Toon;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using ToonFunctionArgs1;

var openAiClient = new OpenAIClient(new ApiKeyCredential(Env.Instance["API_KEY"]), new OpenAIClientOptions()
{
    Endpoint = new Uri(Env.Instance["ENDPOINT"]),
});


var toon = new ToonFunction();

var chatClient = openAiClient.GetChatClient(Env.Instance["MODEL"]).AsIChatClient();

var toonFormat = ToonSerializer.Serialize(new[]
{
    new
    {
        title = "勇者冒险",
        plot = "一个勇敢的骑士踏上了寻找魔法剑的冒险之旅",
        characters = new[]
        {
            new
            {
                name = "亚瑟",
                role = "主角",
                description = "勇敢的骑士"
            },
            new
            {
                name = "亚瑟",
                role = "主角",
                description = "勇敢的骑士"
            }
        }
    }
});

var messages = new List<ChatMessage>
{
    new(ChatRole.User, new List<AIContent>()
    {
        new TextContent("帮我创建一个简单的故事，包含标题、作者、情节和至少2个角色的信息，并使用`Write`存储结构。"),
        new TextContent($"""
                         <system-reminder>
                         重要：你生成的格式必须参考example示例
                         <example>
                         {toonFormat}
                         </example>
                         </system-reminder>
                         """)
    })
};

ChatClientAgent agent = new(chatClient, new ChatClientAgentOptions()
{
    ChatOptions = new ChatOptions()
    {
        ToolMode = ChatToolMode.Auto,
        Tools = new List<AITool>()
        {
            AIFunctionFactory.Create(toon.Write, new AIFunctionFactoryOptions()
            {
                Name = "Write",
                Description = $"""
                                Save the story in TOON format with this structure:
                                - title: story title (required)
                                - plot: story plot (required)
                                - characters: array of character objects (at least 2)
                                  • name: character name
                                  • role: character role
                                  • description: character description
                                """
            })
        }
    }
});


var streaming = agent.RunStreamingAsync(messages);


await foreach (var item in streaming)
{
    Console.Write(item.Text);
}