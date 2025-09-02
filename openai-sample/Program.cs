
using DotNetEnv;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.ClientModel.Primitives;

Env.TraversePath().Load();

var REKA_API_KEY = Environment.GetEnvironmentVariable("REKA_API_KEY")
    ?? throw new InvalidOperationException("REKA_API_KEY environment variable is not set.");
var baseUrl = "http://api.reka.ai/v1";

var openAiClient = new OpenAIClient(new ApiKeyCredential(REKA_API_KEY), new OpenAIClientOptions
{
    Endpoint = new Uri(baseUrl)
});

var client = openAiClient.GetChatClient("reka-flash-research");

try
{
    string prompt = "Suggest 3 hikes in the Greater Montreal area, for a familly trip with kids between 5 and 10 years old.";

    List<ChatMessage> messages = new List<ChatMessage>
                        {
                            new UserChatMessage(prompt)
                        };

    ChatResponseFormat myResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                            jsonSchemaFormatName: "trail-response",
                            jsonSchema: BinaryData.FromBytes("""
                                {
                                    "type": "object",
                                    "properties": {
                                        "trails": {
                                            "type": "array",
                                            "items": {
                                                "type": "object",
                                                "properties": {
                                                    "name": {
                                                        "type": "string"
                                                    },
                                                    "address": {
                                                        "type": "string"
                                                    },
                                                    "website": {
                                                        "type": "string"
                                                    },
                                                    "rating": {
                                                        "type": "number"
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                """u8.ToArray()),
                            jsonSchemaIsStrict: true);

    ChatCompletionOptions ccOptions = new ();

// Workaround for unsupported extra_body property by OpenAi SDK
#pragma warning disable CS8600

    ccOptions = ((IJsonModel<ChatCompletionOptions>)ccOptions).Create(new BinaryData("""
    {
        "research": {
            "web_search": {
                "max_uses": 1,
                "blocked_domains": ["reddit.com"]
            }
        }
    }
    """), ModelReaderWriterOptions.Json);

    ccOptions!.ResponseFormat = myResponseFormat;

#pragma warning restore CS8600
// End of workaround

    var completion = await client.CompleteChatAsync(messages, ccOptions);
    var result = completion.Value;

    var generatedText = result.Content[0].Text;
    // Pretty print JSON output
    try
    {
        using var doc = System.Text.Json.JsonDocument.Parse(generatedText);
        var prettyJson = System.Text.Json.JsonSerializer.Serialize(doc.RootElement, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine($"Result:\n{prettyJson}");
    }
    catch
    {
        // Fallback to raw output if not valid JSON
        Console.WriteLine($"Result:\n {generatedText}");
    }

    // Show token usage
    Console.WriteLine($"\n-------------\nTotal tokens: {result.Usage?.TotalTokenCount ?? 0}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
