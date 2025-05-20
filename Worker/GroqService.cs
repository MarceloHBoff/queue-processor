using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using Worker.Dtos;

namespace Worker;

public class GroqService
{
    private readonly HttpClient _httpClient;

    public GroqService(IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {configuration.GetValue("GroqKey", "")}");
    }

    public async Task<string> GetResponse(string message)
    {
        var requestBody = new
        {
            model = "meta-llama/llama-4-scout-17b-16e-instruct",
            messages = new[]
            {
                new { role = "user", content = $@"
                    #Role
                    You are an AI assistant that helps write professional, polite, and context-aware email replies.

                    #Task
                    Given the full content of an email, write a thoughtful response that addresses the sender's message, answers any questions, and maintains a friendly and professional tone.

                    #Input
                    {message}

                    #Response
                    Not add footer and sugestion to response another questions, only response the question.
                " }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);

        var responseJSON = JsonSerializer.Deserialize<GroqApiResponseDto>(await response.Content.ReadAsStringAsync());

        return responseJSON?.Choices[0]?.Message?.Content ?? "";
    }
}
