using System.Text.Json;
using System.Text.Json.Serialization;

namespace TradingSystem.Infrastructure.Serialization;

public static class JsonDefaults
{
    public static JsonSerializerOptions Messaging { get; }  =  CreateMessaging();

    private static JsonSerializerOptions CreateMessaging()
    {
        var options  =  new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive  =  true, 
            PropertyNamingPolicy  =  JsonNamingPolicy.SnakeCaseLower, 
            DefaultIgnoreCondition  =  JsonIgnoreCondition.WhenWritingNull
        };
        
        options.Converters.Add(new JsonStringEnumConverter());
        
        return options;
    }
}
