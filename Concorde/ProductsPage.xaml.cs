using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Diagnostics;
using System.ComponentModel;
using Microsoft.Maui.Controls;

namespace Concorde;

public partial class ProductsPage : ContentPage
{
    public ObservableCollection<Product> Products { get; set; } = new();
    private List<Product> AllProducts { get; set; } = new();

    private readonly HttpClient client = new();

    private int cartItemCount = 0;

    public ProductsPage()
    {
        InitializeComponent();
        BindingContext = this;

        LoadProducts();

        // Fetch the distinct product count for the cart badge
        GetCartProductCountFromServer();

        MessagingCenter.Subscribe<ProductDetailPage, bool>(this, "CartUpdated", (sender, updated) =>
        {
            if (updated)
            {
                GetCartProductCountFromServer();
            }
        });
    }

    private async void LoadProducts()
    {
        try
        {
            string url = "https://concordecac.com/AndroidAppMaui/get_products.php";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var products = JsonSerializer.Deserialize<List<Product>>(responseBody, options) ?? new List<Product>();

            Products.Clear();
            AllProducts.Clear();
            foreach (var product in products)
            {
                Products.Add(product);
                AllProducts.Add(product);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load products: {ex.Message}", "OK");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SearchEntry.Text = string.Empty;
    }

    private async void OnWishlistClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Wishlist", "Added to wishlist", "OK");
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        string searchText = e.NewTextValue?.ToLower() ?? string.Empty;

        var filteredProducts = AllProducts
            .Where(p => p.Name.ToLower().Contains(searchText)
                     || p.Description.ToLower().Contains(searchText))
            .ToList();

        Products.Clear();
        foreach (var product in filteredProducts)
        {
            Products.Add(product);
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

    private async void OnProductTapped(object sender, EventArgs e)
    {
        var frame = (Frame)sender;
        var selectedProduct = (Product)frame.BindingContext;

        if (selectedProduct == null)
            return;

        await Navigation.PushAsync(new ProductDetailPage(selectedProduct));
    }
}

public class Product
{
    [JsonPropertyName("product_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public string Price { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string ImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("product_desc")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("Stock")]
    public int Stock { get; set; }

    // ADD THIS PROPERTY
    public List<string> ImageUrls
    {
        get
        {
            return ImageUrl.Split(',')
                .Select(img => img.Trim())
                .ToList();
        }
    }
}