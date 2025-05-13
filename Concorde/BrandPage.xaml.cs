using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace Concorde;

public partial class BrandPage : ContentPage
{
    public ObservableCollection<Brands> Brands { get; set; } = new();
    private List<Brands> AllBrands { get; set; } = new();
    private static readonly HttpClient client = new();

    private int cartItemCount = 0;

    public BrandPage()
    {
        InitializeComponent();
        BindingContext = this;

        LoadBrands();
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

    private async void LoadBrands()
    {
        try
        {
            string url = "https://concordecac.com/AndroidAppMaui/get_brand.php";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var brands = JsonSerializer.Deserialize<List<Brands>>(responseBody, options) ?? new List<Brands>();

            Brands.Clear();
            AllBrands.Clear();

            foreach (var brand in brands)
            {
                Brands.Add(brand);
                AllBrands.Add(brand);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load brands: {ex.Message}", "OK");
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        string searchText = e.NewTextValue?.ToLower() ?? string.Empty;

        var filteredBrands = AllBrands
            .Where(b => b.Name.ToLower().Contains(searchText)
                     || b.Description.ToLower().Contains(searchText))
            .ToList();

        Brands.Clear();

        foreach (var brand in filteredBrands)
        {
            Brands.Add(brand);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SearchEntry.Text = string.Empty;
    }

    private async void OnBrandSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Brands selectedBrand)
        {
            string brandParam = Uri.EscapeDataString(selectedBrand.Description.Trim());

            Debug.WriteLine($"✅ Checking products for Brand: {brandParam}");

            try
            {
                string url = $"https://concordecac.com/AndroidAppMaui/get_productsbrand.php?brand={brandParam}";
                using var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var products = JsonSerializer.Deserialize<List<ProductBrand>>(json, options) ?? new List<ProductBrand>();

                if (products.Count == 0)
                {
                    await DisplayAlert("No Products", "There are no products available for this category.", "OK");
                    return;
                }

                Debug.WriteLine($"✅ Navigating to ProductBrandPage with Brand: {brandParam}");
                await Shell.Current.GoToAsync($"ProductBrandPage?brand={brandParam}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error Fetching Products: {ex.Message}");
                await DisplayAlert("Error", "Failed to fetch products. Please try again later.", "OK");
            }
        }
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

public class Brands
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("b_name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("b_img")]
    public string ImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("b_desc")]
    public string Description { get; set; } = string.Empty;
}