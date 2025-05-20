using System.Text.Json.Serialization;

namespace Worker.Dtos;

public class GroqApiResponseDto
{
    [JsonPropertyName("choices")]
    public List<GroqApiChoicesResponseDto> Choices { get; set; } = [];
}

public class GroqApiChoicesResponseDto
{
    [JsonPropertyName("message")]
    public GroqApiChoicesMessageResponseDto? Message { get; set; }
}

public class GroqApiChoicesMessageResponseDto
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}