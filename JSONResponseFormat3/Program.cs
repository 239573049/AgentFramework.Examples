using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentFrameworkCore.Options;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

// 创建 OpenAI 客户端
var openAiClient = new OpenAIClient(new ApiKeyCredential(Env.Instance["API_KEY"]!), new OpenAIClientOptions()
{
    Endpoint = new Uri(Env.Instance["ENDPOINT"]!),
});

var chatClient = openAiClient.GetChatClient(Env.Instance["MODEL"]!).AsIChatClient();

Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║   Response Format JSON Generation Demo                 ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

// 示例 1: 使用 response_format 生成用户信息
await GenerateUserProfile(chatClient);

Console.WriteLine();

// 示例 2: 使用 response_format 生成产品列表
await GenerateProductList(chatClient);

Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║   All Examples Completed Successfully                   ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

/// <summary>
/// 示例 1: 使用 response_format 生成用户信息
/// </summary>
static async Task GenerateUserProfile(IChatClient chatClient)
{
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine("Example 1: Generate User Profile with response_format");
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

    // 定义用户信息的 Schema
    var userProfileSchema = new
    {
        type = "object",
        properties = new
        {
            user = new
            {
                type = "object",
                properties = new
                {
                    name = new { type = "string", description = "Full name of the user" },
                    age = new { type = "integer", description = "Age in years" },
                    email = new { type = "string", description = "Email address" },
                    phone = new { type = "string", description = "Phone number" },
                    address = new
                    {
                        type = "object",
                        properties = new
                        {
                            city = new { type = "string", description = "City name" },
                            street = new { type = "string", description = "Street address" },
                            zipCode = new { type = "string", description = "Postal code" }
                        },
                        required = new[] { "city", "street", "zipCode" }
                    }
                },
                required = new[] { "name", "age", "email", "phone", "address" }
            },
            metadata = new
            {
                type = "object",
                properties = new
                {
                    timestamp = new { type = "string", description = "ISO 8601 timestamp" },
                    source = new { type = "string", description = "Data source identifier" },
                    version = new { type = "string", description = "Schema version" }
                },
                required = new[] { "timestamp", "source", "version" }
            }
        },
        required = new[] { "user", "metadata" },
        additionalProperties = false
    };

    // 将 Schema 转换为 JsonElement
    var schemaJsonElement = JsonDocument.Parse(JsonSerializer.Serialize(userProfileSchema)).RootElement;

    // 创建消息列表
    var messages = new List<ChatMessage>
    {
        new(ChatRole.System, """
            You are a data generation assistant. Generate realistic user profile data 
            that strictly conforms to the provided JSON schema. Ensure all required 
            fields are present and data types are correct.
            """),
        new(ChatRole.User, "Generate a user profile for a software engineer living in Shanghai, China.")
    };

    // 使用 ChatClientAgent 并配置 response_format
    ChatClientAgent agent = new(chatClient, new ChatClientAgentOptions()
    {
        ChatOptions = new ChatOptions()
        {
            ResponseFormat = ChatResponseFormat.ForJsonSchema(
                schemaJsonElement,
                "user_profile_schema",
                "User profile information with personal details and metadata"
            )
        }
    });

    Console.WriteLine("📤 Executing agent with response_format schema...\n");

    // 使用 Agent 执行并流式输出
    var result = await agent.RunAsync(messages);

    Console.WriteLine("📥 Received JSON Response:\n");
    
    // 获取最后一条消息的文本内容
    var jsonResponse = result.Messages.LastOrDefault()?.Text;
    
    if (string.IsNullOrEmpty(jsonResponse))
    {
        Console.WriteLine("❌ Error: Empty response received");
        return;
    }
    
    // 解析并美化输出
    var jsonDocument = JsonDocument.Parse(jsonResponse);
    var formattedJson = JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions { WriteIndented = true });
    
    Console.WriteLine(formattedJson);
    
    // 反序列化为强类型对象
    var userProfile = JsonSerializer.Deserialize<UserProfileResponse>(jsonResponse);
    
    Console.WriteLine("\n✅ Validation Results:");
    Console.WriteLine($"   • User Name: {userProfile?.User?.Name}");
    Console.WriteLine($"   • User Age: {userProfile?.User?.Age}");
    Console.WriteLine($"   • User Email: {userProfile?.User?.Email}");
    Console.WriteLine($"   • City: {userProfile?.User?.Address?.City}");
    Console.WriteLine($"   • Timestamp: {userProfile?.Metadata?.Timestamp}");
    Console.WriteLine($"   • Schema Version: {userProfile?.Metadata?.Version}\n");
}

/// <summary>
/// 示例 2: 使用 response_format 生成产品列表
/// </summary>
static async Task GenerateProductList(IChatClient chatClient)
{
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine("Example 2: Generate Product List with response_format");
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

    // 定义产品列表的 Schema
    var productListSchema = new
    {
        type = "object",
        properties = new
        {
            products = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "Unique product identifier" },
                        name = new { type = "string", description = "Product name" },
                        category = new { type = "string", description = "Product category" },
                        price = new { type = "number", description = "Price in USD" },
                        inStock = new { type = "boolean", description = "Availability status" },
                        tags = new
                        {
                            type = "array",
                            items = new { type = "string" },
                            description = "Product tags"
                        }
                    },
                    required = new[] { "id", "name", "category", "price", "inStock", "tags" }
                },
                minItems = 3,
                maxItems = 5
            },
            totalCount = new { type = "integer", description = "Total number of products" },
            generatedAt = new { type = "string", description = "Generation timestamp" }
        },
        required = new[] { "products", "totalCount", "generatedAt" },
        additionalProperties = false
    };

    // 将 Schema 转换为 JsonElement
    var productSchemaJsonElement = JsonDocument.Parse(JsonSerializer.Serialize(productListSchema)).RootElement;

    // 创建消息列表
    var messages = new List<ChatMessage>
    {
        new(ChatRole.System, """
            You are a product catalog generator. Create realistic product listings 
            with accurate pricing and categorization. Ensure all data conforms to 
            the provided schema structure.
            """),
        new(ChatRole.User, "Generate a list of 4 electronic products with detailed information.")
    };

    // 使用 ChatClientAgent 并配置 response_format
    ChatClientAgent agent = new(chatClient, new ChatClientAgentOptions()
    {
        ChatOptions = new ChatOptions()
        {
            ResponseFormat = ChatResponseFormat.ForJsonSchema(
                productSchemaJsonElement,
                "product_list_schema",
                "Product catalog listing with detailed information"
            )
        }
    });

    Console.WriteLine("📤 Executing agent with response_format schema...\n");

    // 使用 Agent 执行
    var result = await agent.RunAsync(messages);

    Console.WriteLine("📥 Received JSON Response:\n");
    
    // 获取最后一条消息的文本内容
    var jsonResponse = result.Messages.LastOrDefault()?.Text;
    
    if (string.IsNullOrEmpty(jsonResponse))
    {
        Console.WriteLine("❌ Error: Empty response received");
        return;
    }
    
    // 解析并美化输出
    var jsonDocument = JsonDocument.Parse(jsonResponse);
    var formattedJson = JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions { WriteIndented = true });
    
    Console.WriteLine(formattedJson);
    
    // 反序列化为强类型对象
    var productList = JsonSerializer.Deserialize<ProductListResponse>(jsonResponse);
    
    Console.WriteLine("\n✅ Validation Results:");
    Console.WriteLine($"   • Total Products: {productList?.TotalCount}");
    Console.WriteLine($"   • Generated At: {productList?.GeneratedAt}");
    
    if (productList?.Products != null)
    {
        foreach (var product in productList.Products)
        {
            Console.WriteLine($"   • Product: {product.Name} - ${product.Price} ({product.Category})");
        }
    }
    Console.WriteLine();
}

#region Data Models

/// <summary>
/// 用户信息响应模型
/// </summary>
public class UserProfileResponse
{
    [JsonPropertyName("user")]
    public UserInfo? User { get; set; }
    
    [JsonPropertyName("metadata")]
    public MetadataInfo? Metadata { get; set; }
}

public class UserInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("age")]
    public int Age { get; set; }
    
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
    
    [JsonPropertyName("address")]
    public AddressInfo? Address { get; set; }
}

public class AddressInfo
{
    [JsonPropertyName("city")]
    public string? City { get; set; }
    
    [JsonPropertyName("street")]
    public string? Street { get; set; }
    
    [JsonPropertyName("zipCode")]
    public string? ZipCode { get; set; }
}

public class MetadataInfo
{
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }
    
    [JsonPropertyName("source")]
    public string? Source { get; set; }
    
    [JsonPropertyName("version")]
    public string? Version { get; set; }
}

/// <summary>
/// 产品列表响应模型
/// </summary>
public class ProductListResponse
{
    [JsonPropertyName("products")]
    public List<ProductInfo>? Products { get; set; }
    
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
    
    [JsonPropertyName("generatedAt")]
    public string? GeneratedAt { get; set; }
}

public class ProductInfo
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("category")]
    public string? Category { get; set; }
    
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
    
    [JsonPropertyName("inStock")]
    public bool InStock { get; set; }
    
    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}

#endregion
