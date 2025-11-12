
using System.ClientModel;
using System.ComponentModel;
using System.Text.Json;
using AgentFrameworkCore.Options;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

var openAiClient = new OpenAIClient(new ApiKeyCredential(Env.Instance["API_KEY"]!), new OpenAIClientOptions()
{
    Endpoint = new Uri(Env.Instance["ENDPOINT"]!),
});

var chatClient = openAiClient.GetChatClient(Env.Instance["MODEL"]!).AsIChatClient();

// 定义固定的JSON格式示例
var jsonFormatExample = JsonSerializer.Serialize(new
{
    user = new
    {
        name = "张三",
        age = 25,
        email = "zhangsan@example.com",
        address = new
        {
            city = "北京",
            street = "长安街1号",
            zipCode = "100000"
        }
    },
    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
    status = "success"
}, new JsonSerializerOptions { WriteIndented = true });

// 创建消息列表
var messages = new List<ChatMessage>
{
    new(ChatRole.User, new List<AIContent>()
    {
        new TextContent("Generate a user profile with name, age, email, and address details (city, street, zip code), then save it using the SaveUserInfo function."),
        new TextContent($"""
                         <system-instruction>
                         CRITICAL: You MUST generate data that STRICTLY adheres to the following JSON schema.
                         Do NOT deviate from this structure under any circumstances.
                         
                         <schema-reference>
                         {jsonFormatExample}
                         </schema-reference>
                         
                         <validation-rules>
                         1. All required fields must be present and properly typed
                         2. String fields must contain valid, realistic data
                         3. Numeric fields must contain valid integers
                         4. Nested objects must maintain the exact structure shown
                         5. Timestamp must follow the format: yyyy-MM-dd HH:mm:ss
                         6. Status field must indicate operation state
                         </validation-rules>
                         </system-instruction>
                         """)
    })
};

// 创建Agent
ChatClientAgent agent = new(chatClient, new ChatClientAgentOptions()
{
    ChatOptions = new ChatOptions()
    {
        ToolMode = ChatToolMode.Auto,
        Tools = new List<AITool>()
        {
            AIFunctionFactory.Create(SaveUserInfo, new AIFunctionFactoryOptions()
            {
                Name = "SaveUserInfo",
                Description = """
                              Persists user information in a structured JSON format to the system.
                              
                              REQUIRED SCHEMA:
                              {
                                "user": {                    // User entity (REQUIRED)
                                  "name": string,           // Full name of the user
                                  "age": number,            // Age in years (integer)
                                  "email": string,          // Valid email address
                                  "address": {              // Physical address object
                                    "city": string,         // City name
                                    "street": string,       // Street address
                                    "zipCode": string       // Postal/ZIP code
                                  }
                                },
                                "timestamp": string,        // ISO-like timestamp (yyyy-MM-dd HH:mm:ss)
                                "status": string            // Operation status indicator
                              }
                              
                              CONSTRAINTS:
                              - All fields are mandatory and must be present
                              - Types must match exactly as specified
                              - Email must be in valid format
                              - Timestamp must follow the specified format
                              """
            })
        }
    }
});

Console.WriteLine("=== Execution Started ===\n");

// Stream the agent's response
var streaming = agent.RunStreamingAsync(messages);

await foreach (var item in streaming)
{
    Console.Write(item.Text);
}

Console.WriteLine("\n\n=== Execution Completed ===");

// Persists user information with validation
static string SaveUserInfo(
    [Description("A JSON-formatted string containing user information conforming to the predefined schema")]
    string jsonData)
{
    try
    {
        // Parse and deserialize JSON payload
        var userInfo = JsonSerializer.Deserialize<JsonElement>(jsonData);
        
        Console.WriteLine("\n╔════════════════════════════════════╗");
        Console.WriteLine("║   Received JSON Payload           ║");
        Console.WriteLine("╚════════════════════════════════════╝");
        Console.WriteLine(JsonSerializer.Serialize(userInfo, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("════════════════════════════════════\n");
        
        // Schema validation - top-level fields
        if (!userInfo.TryGetProperty("user", out var user))
        {
            return "❌ VALIDATION ERROR: Missing required field 'user'";
        }
        
        if (!userInfo.TryGetProperty("timestamp", out _))
        {
            return "❌ VALIDATION ERROR: Missing required field 'timestamp'";
        }
        
        if (!userInfo.TryGetProperty("status", out _))
        {
            return "❌ VALIDATION ERROR: Missing required field 'status'";
        }
        
        // Schema validation - user object fields
        if (!user.TryGetProperty("name", out _) ||
            !user.TryGetProperty("age", out _) ||
            !user.TryGetProperty("email", out _) ||
            !user.TryGetProperty("address", out var address))
        {
            return "❌ VALIDATION ERROR: User object is missing one or more required fields (name, age, email, address)";
        }
        
        // Schema validation - address object fields
        if (!address.TryGetProperty("city", out _) ||
            !address.TryGetProperty("street", out _) ||
            !address.TryGetProperty("zipCode", out _))
        {
            return "❌ VALIDATION ERROR: Address object is missing one or more required fields (city, street, zipCode)";
        }
        
        return """
               <function-response>
               ✅ SUCCESS: User information has been successfully persisted.
               ✅ VALIDATION: All schema constraints satisfied.
               ✅ STATUS: Data integrity confirmed.
               </function-response>
               """;
    }
    catch (JsonException ex)
    {
        return $"❌ PARSE ERROR: Invalid JSON format - {ex.Message}";
    }
}

