using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Diagnostics;
using System.Timers;

namespace Concorde;

public partial class ProductDetailPage : ContentPage
{
    private int quantity = 1;
    private int productStock = 0;
    private int _currentPosition = 0;
    private System.Timers.Timer _slideshowTimer;

    public ProductDetailPage(Product product)
    {
        InitializeComponent();
        BindingContext = product;

        productStock = product.Stock;

        // Disable AddToCartButton if stock is 0
        if (productStock == 0)
        {
            AddToCartButton.IsEnabled = false;
            AddToCartButton.Text = "Out of Stock";
            AddToCartButton.BackgroundColor = Colors.Gray;
        }

        StartSlideshow();
    }

    private void StartSlideshow()
    {
        _slideshowTimer = new System.Timers.Timer(3000);
        _slideshowTimer.Elapsed += OnSlideshowTimerElapsed;
        _slideshowTimer.AutoReset = true;
        _slideshowTimer.Start();
    }

    private void OnSlideshowTimerElapsed(object sender, ElapsedEventArgs e)
    {
        // Access UI elements on the main thread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (ProductCarousel.ItemsSource == null)
                return;

            var imageList = ProductCarousel.ItemsSource as IList<string>;
            if (imageList == null || imageList.Count == 0)
                return;

            _currentPosition++;
            if (_currentPosition >= imageList.Count)
                _currentPosition = 0;

            ProductCarousel.Position = _currentPosition;
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _slideshowTimer?.Stop();
        _slideshowTimer?.Dispose();
    }

    // ✅ Existing code for quantity and cart remains unchanged...
    private void OnIncreaseQuantityClicked(object sender, EventArgs e)
    {
        if (quantity < productStock)
        {
            quantity++;
            QuantityLabel.Text = quantity.ToString();
        }

        UpdateQuantityButtons();
    }

    private async void OnWishlistClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Wishlist", "Added to wishlist", "OK");
    }

    private void OnDecreaseQuantityClicked(object sender, EventArgs e)
    {
        if (quantity > 1)
        {
            quantity--;
            QuantityLabel.Text = quantity.ToString();
        }

        UpdateQuantityButtons();
    }

    private void OnAddToCartClicked(object sender, EventArgs e)
    {
        CartModal.IsVisible = true;

        quantity = 1;
        QuantityLabel.Text = quantity.ToString();

        UpdateQuantityButtons();

        var product = BindingContext as Product;

        if (product != null && product.ImageUrls != null && product.ImageUrls.Any())
        {
            // ✅ Set the first image URL from the ImageUrls list to the CartModalImage
            CartModalImage.Source = product.ImageUrls.First();
        }
        else
        {
            // ✅ Optional fallback if no images found
            CartModalImage.Source = "placeholder_image.png";
        }
    }

    private async void OnConfirmAddToCartClicked(object sender, EventArgs e)
    {
        CartModal.IsVisible = false;

        var product = BindingContext as Product;

        if (product != null)
        {
            LoadingOverlay.IsVisible = true;

            bool isAdded = await AddItemToCartDatabase(product, quantity);

            LoadingOverlay.IsVisible = false;

            if (isAdded)
            {
                var cartItem = new CartItem
                {
                    Id = product.Id,
                    Name = product.Name,
                    ImageUrl = product.ImageUrl,
                    Price = product.Price,
                    Quantity = quantity
                };

                CartService.Instance.AddToCart(cartItem);

                MessagingCenter.Send(this, "CartUpdated", true);

                await DisplayAlert("Success", $"{quantity} item(s) added to cart!", "OK");
            }
            else
            {
                await DisplayAlert("Error", "Failed to add item to cart.", "OK");
            }
        }
    }

    private readonly HttpClient client = new();

    private async Task<bool> AddItemToCartDatabase(Product product, int quantity)
    {
        try
        {
            var url = "https://concordecac.com/AndroidAppMaui/add_to_cart.php";
            var orderedBy = Preferences.Get("UserId", "");

            if (string.IsNullOrEmpty(orderedBy))
            {
                await DisplayAlert("Error", "User not logged in!", "OK");
                return false;
            }

            var values = new Dictionary<string, string>
            {
                { "product_id", product.Id },
                { "product_name", product.Name },
                { "image_url", product.ImageUrl },
                { "quantity", quantity.ToString() },
                { "price", product.Price },
                { "orderedBy", orderedBy }
            };

            var content = new FormUrlEncodedContent(values);

            using var response = await client.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"Server Response: {responseString}");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseString, options);

            if (jsonResponse != null && jsonResponse.ContainsKey("success"))
            {
                var successElement = (JsonElement)jsonResponse["success"];
                return successElement.GetBoolean();
            }

            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error adding item to cart: {ex.Message}");
            return false;
        }
    }

    private void UpdateQuantityButtons()
    {
        IncreaseButton.IsEnabled = quantity < productStock;
        DecreaseButton.IsEnabled = quantity > 1;
    }
}