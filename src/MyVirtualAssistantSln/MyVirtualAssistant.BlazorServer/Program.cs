using Azure.AI.OpenAI;
using Azure;
using Toolbelt.Blazor.Extensions.DependencyInjection;
using Blazored.Toast;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddBlazoredToast();

builder.Services.AddSpeechRecognition();
builder.Services.AddSpeechSynthesis();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var azureOpenAIEndpoint = builder.Configuration["Azure:OpenAI:CompletionsDeployment:Endpoint"]!;
var azureOpenAIKey = builder.Configuration["Azure:OpenAI:CompletionsDeployment:Key"]!;
builder.Services.AddSingleton<OpenAIClient>((sp) =>
{
    return new OpenAIClient(endpoint: new Uri(azureOpenAIEndpoint),
        keyCredential: new AzureKeyCredential(azureOpenAIKey));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
