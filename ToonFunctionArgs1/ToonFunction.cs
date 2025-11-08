using System.ComponentModel;
using AIDotNet.Toon;

namespace ToonFunctionArgs1;

public class ToonFunction
{
    public string Write(
        [Description("The data to be written, TOON format string")]
        string value)
    {
        var toon = ToonSerializer.Deserialize<object>(value, new ToonSerializerOptions()
        {
            Strict = false
        });

        return """
               <system-reminder>
                这是系统提醒：你已经写入成功
               </system-reminder>
               """;
    }
}