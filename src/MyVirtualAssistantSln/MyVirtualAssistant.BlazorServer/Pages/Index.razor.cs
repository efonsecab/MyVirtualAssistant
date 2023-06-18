using Azure.AI.OpenAI;
using Azure;
using Microsoft.AspNetCore.Components;
using Toolbelt.Blazor.SpeechRecognition;
using Toolbelt.Blazor.SpeechSynthesis;
using Blazored.Toast.Services;

namespace MyVirtualAssistant.BlazorServer.Pages
{
    public partial class Index
    {
        [Inject]
        private SpeechRecognition? SpeechRecognition { get; set; }
        [Inject]
        private SpeechSynthesis? SpeechSynthesis { get; set; }
        [Inject]
        private OpenAIClient? OpenAIClient { get; set; }
        [Inject]
        private IToastService? ToastService { get; set; }
        private string? Error { get; set; }

        private List<ChatMessage> ChatMessages = new List<ChatMessage>()
        {
            new ChatMessage(ChatRole.System, "You are a helpful assistant.")
        };
        private bool IsBusy { get; set; }

        protected override async void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                this.SpeechRecognition!.Continuous = true;
                this.SpeechRecognition!.Result += OnSpeechRecognized;
                this.SpeechSynthesis!.UtteranceEnded += OnUtteranceEnded;
                await this.SpeechRecognition!.StartAsync();
            }
        }


        private async void OnUtteranceEnded(object? sender, EventArgs e)
        {
            this.IsBusy = false;
            StateHasChanged();
            await this.SpeechRecognition!.StartAsync();
        }

        private async void OnSpeechRecognized(object? sender, SpeechRecognitionEventArgs e)
        {
            await this.SpeechRecognition!.StopAsync();
            this.IsBusy = true;
            this.Error = string.Empty;
            StateHasChanged();
            foreach (var singleResult in e.Results)
            {
                var mostAccurateTranscript = singleResult.Items
                    .OrderByDescending(p => p.Confidence).First().Transcript;
                this.ChatMessages.Add(new ChatMessage(ChatRole.User, mostAccurateTranscript));
                StateHasChanged();
                var chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    ChoicesPerPrompt = 1,
                    FrequencyPenalty = 0,
                    MaxTokens = 2000,
                    NucleusSamplingFactor = 0.95f,
                    PresencePenalty = 0,
                    Temperature = 0.7f
                };
                foreach (var singleMessage in this.ChatMessages)
                {
                    chatCompletionsOptions.Messages.Add(singleMessage);
                }
                try
                {
                    StateHasChanged();
                    Response<ChatCompletions> response = await this.OpenAIClient!.GetChatCompletionsAsync(
                        deploymentOrModelName: "video_gpt_35", chatCompletionsOptions:
                        chatCompletionsOptions);
                    foreach (var singleChoice in response.Value.Choices)
                    {
                        this.ChatMessages.Add(new ChatMessage(ChatRole.Assistant, singleChoice.Message.Content));
                        StateHasChanged();
                        await this.SpeechSynthesis!.SpeakAsync(singleChoice.Message.Content);
                    }
                }
                catch (Exception ex)
                {
                    this.ToastService!.ShowError(ex.Message);
                    this.Error = ex.Message;
                    StateHasChanged();
                    await this.SpeechSynthesis!.SpeakAsync(this.Error);
                }
            }
        }
    }
}
