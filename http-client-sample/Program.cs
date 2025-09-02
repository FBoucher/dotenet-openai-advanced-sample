
using System.Text;
using System.Text.Json;
using DotNetEnv;

Env.TraversePath().Load();
var REKA_API_KEY = Environment.GetEnvironmentVariable("REKA_API_KEY")
    ?? throw new InvalidOperationException("REKA_API_KEY environment variable is not set.");


using var httpClient = new HttpClient();
httpClient.Timeout = Timeout.InfiniteTimeSpan;

var requestUrl = "https://api.reka.ai/v1/chat/completions";

var responseFormat = new
{
    type = "json_schema",
    json_schema = new
    {
        name = "ListRestaurants",
        schema = new
        {
            type = "object",
            properties = new
            {
                restaurants = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new { type = "string" },
                            address = new { type = "string" },
                            phoneNumber = new { type = "string" },
                            website = new { type = "string" },
                            score = new { type = "integer" },
                            priceLevel = new 
                            { 
                                type = "string", 
                                @enum = new[] { "$", "$$", "$$$" } 
                            },
                        }
                    }
                }
            }
        }
    }
};



var requestPayload = new
{
    model = "reka-flash-research",
    messages = new[]
            {
                new
                {
                    role = "user",
                    content = "Give me 3 nice, not crazy expensive, restaurants for a romantic dinner in New York city"
                }
            },
    response_format = responseFormat,
    extra_body = new
    {
        research = new
        {
            web_search = new
            {
                max_uses = 2,
                allowed_domains = new string[] { "tripadvisor.com" }
            }
        }
    }
};

// Serialize the payload to JSON
var jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
});

using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
request.Headers.Add("Authorization", $"Bearer {REKA_API_KEY}");
request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

try
{
    var response = await httpClient.SendAsync(request);

    var responseContent = await response.Content.ReadAsStringAsync();

    if (response.IsSuccessStatusCode)
    {
        //Console.WriteLine(responseContent);
        var jsonDocument = JsonDocument.Parse(responseContent);

        var answerStr = jsonDocument.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content").GetString();

        var answer = JsonDocument.Parse(answerStr!);

        var prettyJson = JsonSerializer.Serialize(answer, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        Console.WriteLine(prettyJson);
    }
    else
    {
        Console.WriteLine($"Request failed with status code: {response.StatusCode}");
        Console.WriteLine($"Response: {responseContent}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
