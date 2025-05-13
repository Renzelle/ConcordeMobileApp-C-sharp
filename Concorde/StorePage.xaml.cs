using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace Concorde;

public partial class StorePage : ContentPage
{
    public ObservableCollection<Branch> Branches { get; set; } = new();
    private static HttpClient client = new();
    private List<Branch> AllBranch { get; set; } = new();
    private int cartItemCount = 0;

    public StorePage()
    {
        InitializeComponent();
        BindingContext = this;
        Loadbranch();
        GetCartProductCountFromServer();

        // ✅ Optional: Listen for cart updates (if needed for real-time badge updates)
        MessagingCenter.Subscribe<ProductDetailPage, bool>(this, "CartUpdated", (sender, updated) =>
        {
            if (updated)
            {
                GetCartProductCountFromServer();
            }
        });
    }

    private async void Loadbranch()
    {
        try
        {
            string url = "https://concordecac.com/AndroidAppMaui/get_branches.php";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var branches = JsonSerializer.Deserialize<List<Branch>>(responseBody, options) ?? new List<Branch>();

            // Populate AllBranch list here ✅
            AllBranch = branches;

            Branches.Clear();
            foreach (var branch in branches)
            {
                Branches.Add(branch);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load branch: {ex.Message}", "OK");
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        string searchText = e.NewTextValue?.ToLower() ?? string.Empty;

        var filteredBranch = AllBranch
            .Where(b => b.Name.ToLower().Contains(searchText))
            .ToList();

        Branches.Clear();

        foreach (var branch in filteredBranch)
        {
            Branches.Add(branch);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SearchEntry.Text = string.Empty;
    }

    private async void OnCartButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CartPage());
    }

    private async Task GetCartProductCountFromServer()
    {
        try
        {
            // Get the stored Firstname
            string firstname = Preferences.Get("UserId", string.Empty);

            Debug.WriteLine($"Retrieved UserId from Preferences: {firstname}");

            if (string.IsNullOrEmpty(firstname))
            {
                Debug.WriteLine("UserId not found in preferences.");
                cartItemCount = 0;
                UpdateCartBadge();
                return;
            }

            var url = $"https://concordecac.com/AndroidAppMaui/get_cart_count.php?firstname={Uri.EscapeDataString(firstname)}";

            Debug.WriteLine($"Requesting cart count from URL: {url}");

            using var response = await client.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"Cart Count API Raw Response: {responseString}");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<Dictionary<string, int>>(responseString, options);

            if (result != null && result.ContainsKey("count"))
            {
                cartItemCount = result["count"];
                Debug.WriteLine($"Cart Item Count for '{firstname}': {cartItemCount}");
                UpdateCartBadge();
            }
            else
            {
                Debug.WriteLine($"Failed to get valid count from response: {responseString}");
                await DisplayAlert("Error", "Failed to get cart count", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception while getting cart count: {ex.Message}");
            await DisplayAlert("Error", "Failed to connect to the server.", "OK");
        }
    }

    private void UpdateCartBadge()
    {
        CartBadge.Text = cartItemCount.ToString();
        CartBadge.IsVisible = cartItemCount > 0;
    }
}

public class Branch
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("bch_name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("bch_location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("bch_image")]
    public string ImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("bch_contact")]
    public string Contact { get; set; } = string.Empty;

    public Command<string> OpenMapCommand => new(async (location) =>
    {
        if (!string.IsNullOrWhiteSpace(location))
        {
            string mapsUrl = $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(location)}";
            await Launcher.OpenAsync(mapsUrl);
        }
    });

    public Command<string> OpenContactCommand => new(async (contact) =>
    {
        if (!string.IsNullOrWhiteSpace(contact))
        {
            string action = await App.Current.MainPage.DisplayActionSheet(
                "Choose Action", "Cancel", null, "Call", "SMS");

            if (action == "Call")
            {
                await Launcher.OpenAsync($"tel:{contact}");
            }
            else if (action == "SMS")
            {
                await Launcher.OpenAsync($"sms:{contact}");
            }
        }
    });
}