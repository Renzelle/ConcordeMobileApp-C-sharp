using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;

namespace Concorde;

public partial class ChatPage : ContentPage
{
    public ObservableCollection<ChatMessage> Messages { get; set; } = new();
    private GeminiChatService chatService = new();

    public ChatPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UserInput.Text))
            return;

        string userMessage = UserInput.Text;
        Messages.Add(new ChatMessage { Content = userMessage, IsUser = true });

        UserInput.Text = "";

        string botResponse = await chatService.GetResponse(userMessage);
        Messages.Add(new ChatMessage { Content = botResponse, IsUser = false });
    }
}

public class ChatMessage
{
    public string Content { get; set; }
    public bool IsUser { get; set; }
}

public class GeminiChatService
{
    private readonly HttpClient _client = new();
    private readonly string apiKey = "AIzaSyARIqyZ2A9QqMzh2dZbqDQ-qSFLi5QTAHA"; // Replace with your actual API key
    private const string apiUrl = "https://generativelanguage.googleapis.com/v1/models/gemini-1.5-pro-latest:generateContent";

    public async Task<string> GetResponse(string userMessage)
    {
        if (string.IsNullOrEmpty(apiKey))
            return "Error: API key is missing!";

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = userMessage } } }
            }
        };

        var requestJson = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        _client.DefaultRequestHeaders.Clear();

        var response = await _client.PostAsync($"{apiUrl}?key={apiKey}", content);
        var responseJson = await response.Content.ReadAsStringAsync();

        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("content", out var contentProp))
            {
                return contentProp.GetString();
            }
            else
            {
                return $"Error: Unexpected API response format.\nResponse: {responseJson}";
            }
        }
        catch (Exception ex)
        {
            return $"Error parsing response: {ex.Message}\nResponse: {responseJson}";
        }
    }
}