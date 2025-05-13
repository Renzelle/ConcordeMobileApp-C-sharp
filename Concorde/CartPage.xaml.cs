using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Concorde;

public partial class CartPage : ContentPage, INotifyPropertyChanged
{
    public new event PropertyChangedEventHandler PropertyChanged;

    protected new void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public ObservableCollection<CartItem> CartItems { get; set; } = new();

    private decimal totalPrice; // ✅ Added backing field

    public decimal TotalPrice // ✅ Added property
    {
        get => totalPrice;
        set
        {
            if (totalPrice != value)
            {
                totalPrice = value;
                OnPropertyChanged(); // Notify the UI
            }
        }
    }

    private static readonly HttpClient client = new();

    public CartPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadCartItems(); // Refresh the cart items when the page appears
    }

    private async void LoadCartItems()
    {
        try
        {
            CartItems.Clear();

            string currentUser = Preferences.Get("UserId", ""); // Assuming you store the firstname in Preferences
            if (string.IsNullOrEmpty(currentUser))
            {
                await DisplayAlert("Error", "No logged-in user found.", "OK");
                return;
            }

            var url = "https://concordecac.com/AndroidAppMaui/get_cart_items.php";

            var values = new Dictionary<string, string>
            {
                { "orderedBy", currentUser }
            };

            var content = new FormUrlEncodedContent(values);

            using var response = await client.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            var jsonResponse = JsonSerializer.Deserialize<List<CartItem>>(responseString);

            if (jsonResponse != null)
            {
                foreach (var item in jsonResponse)
                {
                    // Get first image URL if multiple images exist
                    var firstImage = item.ImageUrl.Split(',')[0].Trim();
                    item.ImageUrl = firstImage;

                    item.IsSelected = true;
                    CartItems.Add(item);

                    item.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(CartItem.Quantity) || e.PropertyName == nameof(CartItem.IsSelected))
                        {
                            CalculateTotalPrice();
                        }
                    };
                }

                CalculateTotalPrice(); // Initial calculation
            }
            else
            {
                await DisplayAlert("Error", "Failed to load cart items.", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading cart: {ex.Message}");
            await DisplayAlert("Error", "Failed to connect to the server.", "OK");
        }
    }

    private async void OnRemoveItemSwiped(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is CartItem item)
        {
            bool confirm = await DisplayAlert("Confirm", $"Remove {item.Name} from cart?", "Yes", "No");

            if (confirm)
            {
                bool isDeleted = await RemoveItemFromCartDatabase(item);

                if (isDeleted)
                {
                    // Find the corresponding frame using sender
                    var parentSwipeView = (SwipeView)swipeItem.Parent;
                    var itemFrame = parentSwipeView.Content as Frame;

                    if (itemFrame != null)
                    {
                        // Smooth fade-out animation
                        await itemFrame.FadeTo(0, 250, Easing.CubicOut);
                        await itemFrame.TranslateTo(-100, 0, 250, Easing.CubicOut);
                    }

                    // Remove the item from collection after animation
                    CartItems.Remove(item);
                    CalculateTotalPrice();
                    MessagingCenter.Send(this, "CartUpdated", true);
                }
            }
        }
    }

    private async Task<bool> RemoveItemFromCartDatabase(CartItem item)
    {
        try
        {
            var url = "https://concordecac.com/AndroidAppMaui/delete_from_cart.php";

            var currentUser = Preferences.Get("UserId", "");

            var values = new Dictionary<string, string>
            {
                { "product_id", item.Id },
                { "orderedBy", currentUser }
            };

            var content = new FormUrlEncodedContent(values);

            using var response = await client.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"Server Response: {responseString}");

            var jsonResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseString);

            if (jsonResponse != null && jsonResponse.ContainsKey("message"))
            {
                return true;
            }
            else
            {
                await DisplayAlert("Error", jsonResponse?["error"]?.ToString() ?? "Unknown error", "OK");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error removing item: {ex.Message}");
            await DisplayAlert("Error", "Failed to connect to the server.", "OK");
            return false;
        }
    }

    private void CalculateTotalPrice()
    {
        decimal total = 0;

        foreach (var item in CartItems)
        {
            if (item.IsSelected)
            {
                if (decimal.TryParse(item.Price, out decimal price))
                {
                    total += price * item.Quantity;
                }
            }
        }

        TotalPrice = total;
    }

    private async void OnRemoveItemClicked(object sender, EventArgs e)
    {
        if (sender is ImageButton button && button.BindingContext is CartItem item)
        {
            bool confirm = await DisplayAlert("Confirm", $"Remove {item.Name} from cart?", "Yes", "No");

            if (confirm)
            {
                bool isDeleted = await RemoveItemFromCartDatabase(item);

                if (isDeleted)
                {
                    CartService.Instance.RemoveFromCart(item);
                    CartItems.Remove(item);
                    CalculateTotalPrice(); // ✅ Update total after removal
                    MessagingCenter.Send(this, "CartUpdated", true);
                    await DisplayAlert("Success", $"{item.Name} was removed from your cart.", "OK");
                }
            }
        }
    }

    private async void OnDecreaseQuantityClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is CartItem item)
        {
            if (item.Quantity > 1)
            {
                item.Quantity--;

                bool updated = await UpdateItemQuantityOnServer(item, item.Quantity);

                if (!updated)
                {
                    item.Quantity++; // rollback quantity if server update failed
                    await DisplayAlert("Error", "Failed to update quantity on server.", "OK");
                }
            }
            else
            {
                await DisplayAlert("Notice", "Quantity can't be less than 1.", "OK");
            }
        }
    }

    private async void OnIncreaseQuantityClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is CartItem item)
        {
            int newQuantity = item.Quantity + 1;

            item.Quantity = newQuantity;
            bool updated = await UpdateItemQuantityOnServer(item, newQuantity);

            if (!updated)
            {
                item.Quantity--;
                await DisplayAlert("Error", "Failed to update quantity on server.", "OK");
            }
        }
    }

    private async void OnCheckoutClicked(object sender, EventArgs e)
    {
        var selectedItems = CartItems.Where(i => i.IsSelected).ToList();

        if (!selectedItems.Any())
        {
            await DisplayAlert("Notice", "No items selected for checkout.", "OK");
            return;
        }

        await Navigation.PushAsync(new CheckoutPage(selectedItems, TotalPrice));
    }

    private async Task<bool> UpdateItemQuantityOnServer(CartItem item, int newQuantity)
    {
        try
        {
            var url = "https://concordecac.com/AndroidAppMaui/update_to_cart.php";
            var currentUser = Preferences.Get("UserId", "");

            var values = new Dictionary<string, string>
        {
            { "product_id", item.Id },
            { "quantity", newQuantity.ToString() },
            { "orderedBy", currentUser }
        };

            var content = new FormUrlEncodedContent(values);
            using var response = await client.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"Quantity Update Response: {responseString}");

            using var jsonDoc = JsonDocument.Parse(responseString);
            var jsonResponse = jsonDoc.RootElement;

            if (jsonResponse.TryGetProperty("message", out var messageValue))
            {
                CalculateTotalPrice();
                return true;
            }
            else if (jsonResponse.TryGetProperty("error", out var errorValue))
            {
                string errorMessage = errorValue.GetString();

                if (jsonResponse.TryGetProperty("available_stock", out var stockValue) && stockValue.ValueKind == JsonValueKind.Number)
                {
                    int availableStock = stockValue.GetInt32();
                    await DisplayAlert("Stock Limit Reached", errorMessage, "OK");
                }
                else
                {
                    await DisplayAlert("Error", errorMessage, "OK");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating quantity: {ex.Message}");
            await DisplayAlert("Error", "Failed to update item quantity. Please try again later.", "OK");
        }
        return false;
    }
}

// =======================
// CartItem Class
// =======================

public class CartItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private string id;
    private string name;
    private string imageUrl;
    private string price;
    private int quantity = 1;
    private decimal totalPrice;
    private bool isSelected;

    [JsonPropertyName("product_id")]
    public string Id
    {
        get => id;
        set => SetProperty(ref id, value);
    }

    [JsonPropertyName("product_name")]
    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    [JsonPropertyName("image_url")]
    public string ImageUrl
    {
        get => imageUrl;
        set => SetProperty(ref imageUrl, value);
    }

    [JsonPropertyName("price")]
    public string Price
    {
        get => price;
        set => SetProperty(ref price, value);
    }

    [JsonPropertyName("quantity")]
    public int Quantity
    {
        get => quantity;
        set => SetProperty(ref quantity, value);
    }

    public decimal TotalPrice
    {
        get => totalPrice;
        set => SetProperty(ref totalPrice, value);
    }

    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }

    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;

        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

// =======================
// CartService Singleton
// =======================

public class CartService
{
    private static readonly Lazy<CartService> _instance = new(() => new CartService());
    public static CartService Instance => _instance.Value;

    private readonly List<CartItem> cartItems = new();

    private CartService()
    { }

    public List<CartItem> GetCartItems() => cartItems;

    public void AddToCart(CartItem item)
    {
        var existing = cartItems.FirstOrDefault(i => i.Id == item.Id);
        if (existing != null)
        {
            existing.Quantity += item.Quantity;
        }
        else
        {
            cartItems.Add(item);
        }
    }

    public void RemoveFromCart(CartItem item)
    {
        cartItems.RemoveAll(i => i.Id == item.Id);
    }

    public void ClearCart() => cartItems.Clear();
}