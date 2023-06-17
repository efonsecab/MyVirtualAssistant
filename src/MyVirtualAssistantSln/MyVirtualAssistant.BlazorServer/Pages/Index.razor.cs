﻿using Azure.AI.OpenAI;
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
        private bool ShowListenButton { get; set; } = true;
        private string? Error { get; set; }
        private bool IsBusy { get; set; } = false;

        private List<ChatMessage> ChatMessages = new List<ChatMessage>()
        {
            new ChatMessage(ChatRole.System, "You are a helpful assistant.")
        };

        protected override void OnInitialized()
        {
            this.SpeechRecognition!.Continuous = false;
            this.SpeechRecognition!.Result += OnSpeechRecognized;
            this.SpeechSynthesis!.UtteranceEnded += OnUtteranceEnded;
        }

        private void OnUtteranceEnded(object? sender, EventArgs e)
        {
            ShowListenButton = true;
            StateHasChanged();
        }

        private async Task OnListenButtonClickedAsync()
        {
            await this.SpeechRecognition!.StartAsync();
        }

        private async void OnSpeechRecognized(object? sender, SpeechRecognitionEventArgs e)
        {
            ShowListenButton = false;
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
                    MaxTokens = 8000,
                };
                foreach (var singleMessage in this.ChatMessages)
                {
                    chatCompletionsOptions.Messages.Add(singleMessage);
                }
                try
                {
                    IsBusy = true;
                    StateHasChanged();
                    Response<ChatCompletions> response = await this.OpenAIClient!.GetChatCompletionsAsync(
                        deploymentOrModelName: "video_gpt_35", chatCompletionsOptions:
                        chatCompletionsOptions);
                    foreach (var singleChoice in response.Value.Choices)
                    {
                        this.ChatMessages.Add(new ChatMessage(ChatRole.System, singleChoice.Message.Content));
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
                finally
                {
                    IsBusy = false;
                    StateHasChanged();
                }
            }
        }
    }
}
