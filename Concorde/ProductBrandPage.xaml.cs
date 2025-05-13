using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;

namespace Concorde;

[QueryProperty(nameof(BrandName), "brand")]
public partial class ProductBrandPage : ContentPage
{
    private static readonly HttpClient client = new();
    private string _brandName;

    public ObservableCollection<ProductBrand> Products { get; set; } = new();
    private List<ProductBrand> AllBrands { get; set; } = new();
    private int cartItemCount = 0;

    public ProductBrandPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public string BrandName
    {
        get => _brandName;
        set
        {
            _brandName = Uri.UnescapeDataString(value);
            Debug.WriteLine($"? Received Brand: {_brandName}");
            OnPropertyChanged();
            _ = LoadProducts(); // Use async-safe approach
        }
    }

    private async Task LoadProducts()
    {
        if (string.IsNullOrWhiteSpace(BrandName)) return;

        try
        {
            string url = $"https://concordecac.com/AndroidAppMaui/get_productsbrand.php?brand={Uri.EscapeDataString(BrandName)}";
            Debug.WriteLine($"Fetching Products for Brand: {BrandName}");

            using var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var products = JsonSerializer.Deserialize<List<ProductBrand>>(json, options) ?? new List<ProductBrand>();

            Debug.WriteLine($"Loaded {products.Count} products for {BrandName}");

            Products.Clear();
            AllBrands.Clear();

            foreach (var product in products)
            {
                Products.Add(product);
                AllBrands.Add(product);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching brand products: {ex.Message}");
            await DisplayAlert("Error", "Failed to load brand products.", "OK");
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        string searchText = e.NewTextValue?.ToLower() ?? string.Empty;

        var filteredBrands = AllBrands
            .Where(b => b.Name.ToLower().Contains(searchText)
                     || b.Description.ToLower().Contains(searchText))
            .ToList();

        Products.Clear();

        foreach (var brand in filteredBrands)
        {
            Products.Add(brand);
        }
    }

    private async void OnCartButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CartPage());
    }

    private async void OnProductTapped(object sender, EventArgs e)
    {
        var frame = (Frame)sender;
        var selectedBrandProduct = (ProductBrand)frame.BindingContext;

        if (selectedBrandProduct == null)
            return;

        // Convert ProductBrand to Product, ensuring type compatibility
        var selectedProduct = new Product
        {
            Id = selectedBrandProduct.Id.ToString(), // Convert int to string
            Name = selectedBrandProduct.Name,
            Price = selectedBrandProduct.Price.ToString(), // Convert decimal to string
            ImageUrl = string.Join(",", selectedBrandProduct.ImageUrls), // Convert list to comma-separated string
            Description = selectedBrandProduct.Description,
            Stock = selectedBrandProduct.Stock
            // 'Brand' is not in Product, so it's omitted
        };

        await Navigation.PushAsync(new ProductDetailPage(selectedProduct));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await GetCartProductCountFromServer(); // Fetch cart count on page load
    }

    private async Task GetCartProductCountFromServer()
    {
        try
        {
            // Get the stored Firstname
            string firstname = Preferences.Get("Firstname", string.Empty);

            Debug.WriteLine($"Retrieved Firstname from Preferences: {firstname}");

            if (string.IsNullOrEmpty(firstname))
            {
                Debug.WriteLine("Firstname not found in preferences.");
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

public class ProductBrand
{
    [JsonPropertyName("product_id")]
    public int Id { get; set; }

    [JsonPropertyName("model")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("images")]
    public List<string> ImageUrls { get; set; } = new(); // List of images

    [JsonPropertyName("product_desc")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("Stock")]
    public int Stock { get; set; }
}