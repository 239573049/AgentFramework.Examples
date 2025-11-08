namespace AgentFrameworkCore.Options;

public class Env
{
    private static Dictionary<string, string>? _cachedEnvDict;
    private static DateTime _lastModified = DateTime.MinValue;

    public static Env Instance { get; } = new Env();

    // 支持通过 env["key"] 获取或设置值
    public string? this[string key]
    {
        get
        {
            // 优先从 .env 文件读取
            string envFilePath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (File.Exists(envFilePath))
            {
                DateTime currentModified = File.GetLastWriteTime(envFilePath);
                if (_cachedEnvDict == null || currentModified > _lastModified)
                {
                    var lines = File.ReadAllLines(envFilePath);
                    _cachedEnvDict = lines
                        .Where(line => !string.IsNullOrWhiteSpace(line) && line.Contains('='))
                        .Select(line => line.Split('=', 2))
                        .Where(parts => parts.Length == 2)
                        .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());
                    _lastModified = currentModified;
                }

                if (_cachedEnvDict.TryGetValue(key, out var value))
                    return value;
            }

            // 文件不存在，回退到环境变量
            return Environment.GetEnvironmentVariable(key);
        }
    }
}