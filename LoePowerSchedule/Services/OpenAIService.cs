using OpenAI.Chat;

namespace LoePowerSchedule.Services;

public class OpenAiService(ChatClient client)
{
    private const string Instruction = "Parse information from this table, return response only the JSON. Dates should be in ISO format. The structure of json should be following: ```{\"date\":\"parsed_iso_date_here\",\"groups\":[{\"id\":\"3.2\",\"schedule\":[{\"type\":\"power_on\",\"start_time\":13,\"end_time\":15}]}]}```. This table contains information about electricity power on and off schedule, in columns there are hours of day, in rows groups to which this schedule relates. Do not skip any offs or ons. Each group should contains relevant power_on and power_off records during day";
    private readonly ChatClient _client = client ?? throw new ArgumentNullException(nameof(client));

    public async Task<string> ParseImageWithGpt(string imagePath)
    {    
        var response = await _client.CompleteChatAsync([
            new SystemChatMessage("You are helpful assistant that converts image data into JSON"),
            new UserChatMessage(
                // ChatMessageContentPart.CreateImageMessageContentPart(new Uri(imagePath)),
                Instruction
            )
                
        ], new ChatCompletionOptions
        {
            MaxTokens   = 2000,
            Temperature = 0.2f,
            TopP = 1,
        });

        var isError = response.GetRawResponse().IsError;
        return isError ? "error" : response.Value.Content[0].Text;
    }
}