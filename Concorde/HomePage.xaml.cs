using System;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Timers;
using Microsoft.Maui.Dispatching;
using System.Text.Json;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Concorde
{
    public partial class HomePage : ContentPage
    {
        private List<string> images = new()
        {
            "banner.png",
            "banner2.jpg",
            "banner3.jpg"
        };

        public ObservableCollection<Product> Products { get; set; } = new();
        public ObservableCollection<Brand> Brands { get; set; } = new();
        private int currentIndex = 0;
        private System.Timers.Timer _bannerTimer;
        private static readonly HttpClient client = new();
        private int cartItemCount = 0;

        public HomePage()
        {
            LoadBrands();
            InitializeComponent();
            StartSlideshow();
            BindingContext = this;
            GetCartProductCountFromServer();

            // ✅ Optional: Listen for cart updates if you add MessagingCenter logic later
            MessagingCenter.Subscribe<ProductDetailPage, bool>(this, "CartUpdated", (sender, updated) =>
            {
                if (updated)
                {
                    GetCartProductCountFromServer();
                }
            });
        }

        private void StartSlideshow()
        {
            SlideshowImage.Source = images[currentIndex];

            _bannerTimer = new System.Timers.Timer(3000);
            _bannerTimer.Elapsed += (sender, e) =>
            {
                currentIndex = (currentIndex + 1) % images.Count;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SlideshowImage.Source = images[currentIndex];
                });
            };
            _bannerTimer.AutoReset = true;
            _bannerTimer.Start();
        }

        private async void OnCartButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CartPage());
        }

        private async Task LoadBrands()
        {
            try
            {
                string url = "https://concordecac.com/AndroidAppMaui/get_brand.php";
                Debug.WriteLine($"[API Request] Fetching brands from: {url}");

                using var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[API Response] {json}");

                var brands = JsonSerializer.Deserialize<List<Brand>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                Brands.Clear();
                foreach (var brand in brands)
                    Brands.Add(brand);

                Debug.WriteLine($"[Success] Loaded {Brands.Count} brands");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Error] Fetching brands failed: {ex.Message}");
            }
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

        private async void OnWishlistClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Wishlist", "Added to wishlist", "OK");
        }

        private void UpdateCartBadge()
        {
            CartBadge.Text = cartItemCount.ToString();
            CartBadge.IsVisible = cartItemCount > 0;
        }

        private async void OnSearchEntryCompleted(object sender, EventArgs e)
        {
            var entry = (Entry)sender;
            string searchText = entry.Text?.Trim().ToLower() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                Products.Clear();
                return;
            }

            // ✅ Call your database/API search
            await SearchProducts(searchText);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            GetCartProductCountFromServer();

            SearchEntry.Text = string.Empty;
            await LoadBrands();
            await LoadAllProducts();
        }

        private async Task LoadAllProducts()
        {
            try
            {
                string url = "https://concordecac.com/AndroidAppMaui/get_products.php";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

                using var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"All Products Response JSON: {responseBody}");

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var products = JsonSerializer.Deserialize<List<Product>>(responseBody, options) ?? new List<Product>();

                Debug.WriteLine($"Total Products Loaded: {products.Count}");

                Products.Clear(); // Clear previous list
                foreach (var product in products)
                {
                    Products.Add(product);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception while loading all products: {ex.Message}");
                await DisplayAlert("Error", $"Failed to load products: {ex.Message}", "OK");
            }
        }

        private async Task SearchProducts(string keyword)
        {
            try
            {
                string url = "https://concordecac.com/AndroidAppMaui/get_products.php";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

                using var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Response JSON: {responseBody}");

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var products = JsonSerializer.Deserialize<List<Product>>(responseBody, options) ?? new List<Product>();

                Debug.WriteLine($"Products Count from API: {products.Count}");

                var filteredProducts = products
                    .Where(p => p.Name.ToLower().Contains(keyword)
                             || p.Description.ToLower().Contains(keyword))
                    .ToList();

                Debug.WriteLine($"Filtered Products Count: {filteredProducts.Count}");

                Products.Clear();
                foreach (var product in filteredProducts)
                {
                    Products.Add(product);
                }

                if (Products.Count == 0)
                {
                    await DisplayAlert("No Results", "No products found.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Search failed: {ex.Message}", "OK");
            }
        }

        private async void OnProductTapped(object sender, EventArgs e)
        {
            if (sender is Frame frame && frame.BindingContext is Product selectedProduct)
            {
                await Navigation.PushAsync(new ProductDetailPage(selectedProduct));
            }
        }

        private async void OnBrandSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Brand selectedBrand)
            {
                string brandParam = Uri.EscapeDataString(selectedBrand.B_Desc.Trim());

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
    }

    public class Brand
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("b_img")]
        public string B_Img { get; set; }

        [JsonPropertyName("b_name")]
        public string B_Name { get; set; }

        [JsonPropertyName("b_desc")]
        public string B_Desc { get; set; }
    }
}