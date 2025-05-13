using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System;

namespace Concorde;

public partial class CheckoutPage : ContentPage, INotifyPropertyChanged
{
    public List<string> ShippingMethods { get; set; } = new() { "Standard Delivery - ₱50", "Express Delivery - ₱150" };

    private string selectedShippingMethod;

    public string SelectedShippingMethod
    {
        get => selectedShippingMethod;
        set
        {
            if (selectedShippingMethod != value)
            {
                selectedShippingMethod = value;
                UpdateShippingFee();
                CalculateTotalPrice();
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<CartItem> CheckoutItems { get; set; } = new();

    private decimal subtotal;

    public decimal Subtotal
    {
        get => subtotal;
        set
        {
            if (subtotal != value)
            {
                subtotal = value;
                OnPropertyChanged();
            }
        }
    }

    private decimal shippingFee;

    public decimal ShippingFee
    {
        get => shippingFee;
        set
        {
            if (shippingFee != value)
            {
                shippingFee = value;
                OnPropertyChanged();
            }
        }
    }

    private decimal discount;

    public decimal Discount
    {
        get => discount;
        set
        {
            if (discount != value)
            {
                discount = value;
                CalculateTotalPrice();
                OnPropertyChanged();
            }
        }
    }

    private decimal totalPrice;

    public decimal TotalPrice
    {
        get => totalPrice;
        set
        {
            if (totalPrice != value)
            {
                totalPrice = value;
                OnPropertyChanged();
            }
        }
    }

    private string deliveryAddress;

    public string DeliveryAddress
    {
        get => deliveryAddress;
        set
        {
            if (deliveryAddress != value)
            {
                deliveryAddress = value;
                OnPropertyChanged();
            }
        }
    }

    private string orderNotes;

    public string OrderNotes
    {
        get => orderNotes;
        set
        {
            if (orderNotes != value)
            {
                orderNotes = value;
                OnPropertyChanged();
            }
        }
    }

    private string voucherCode;

    public string VoucherCode
    {
        get => voucherCode;
        set
        {
            if (voucherCode != value)
            {
                voucherCode = value;
                OnPropertyChanged();
            }
        }
    }

    private static readonly HttpClient client = new();

    public string UserName { get; set; }

    public CheckoutPage(List<CartItem> selectedItems, decimal totalPrice)
    {
        InitializeComponent();
        BindingContext = this;

        SelectedShippingMethod = ShippingMethods[0];

        foreach (var item in selectedItems)
        {
            CheckoutItems.Add(item);
        }

        DeliveryAddress = AccountPage.LoggedInUser?.Locations ?? "No address found!";
        UserName = $"{AccountPage.LoggedInUser?.Firstname} {AccountPage.LoggedInUser?.Lastname}";

        CalculateSubtotal();
        UpdateShippingFee();
        CalculateTotalPrice();
    }

    private void CalculateSubtotal()
    {
        Subtotal = CheckoutItems.Sum(i => decimal.Parse(i.Price) * i.Quantity);
    }

    private void UpdateShippingFee()
    {
        ShippingFee = SelectedShippingMethod.Contains("Express") ? 150 : 50;
    }

    private void CalculateTotalPrice()
    {
        TotalPrice = Subtotal + ShippingFee - Discount;
    }

    private void ApplyVoucher(string voucherCode)
    {
        // Very basic example
        if (voucherCode == "DISCOUNT50")
        {
            Discount = 50;
        }
        else
        {
            Discount = 0;
        }

        CalculateTotalPrice();
    }

    private async void OnApplyVoucherClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(VoucherCode))
        {
            ApplyVoucher(VoucherCode);
            await DisplayAlert("Voucher", Discount > 0 ? $"₱{Discount} discount applied!" : "Invalid voucher.", "OK");
        }
        else
        {
            await DisplayAlert("Voucher", "Please enter a voucher code.", "OK");
        }
    }

    private async void OnEditAddressClicked(object sender, EventArgs e)
    {
        // Example: Prompt user for a new address
        string newAddress = await DisplayPromptAsync("Edit Address", "Enter new delivery address:", initialValue: DeliveryAddress);

        if (!string.IsNullOrEmpty(newAddress))
        {
            DeliveryAddress = newAddress;
        }
        if (string.IsNullOrWhiteSpace(DeliveryAddress))
        {
            await DisplayAlert("Error", "Please enter a delivery address.", "OK");
            return;
        }
    }

    private void OnSelectShippingMethodClicked(object sender, EventArgs e)
    {
        // Example implementation: You can replace this with your own logic
        DisplayActionSheet("Select Shipping Method", "Cancel", null, ShippingMethods.ToArray())
            .ContinueWith(async t =>
            {
                var result = await t;
                if (!string.IsNullOrEmpty(result))
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        SelectedShippingMethod = result;
                    });
                }
            });
    }

    private string _paymentMethod = "Cash on Delivery";

    public string PaymentMethod
    {
        get => _paymentMethod;
        set
        {
            if (_paymentMethod != value)
            {
                _paymentMethod = value;
                OnPropertyChanged(nameof(PaymentMethod));
            }
        }
    }

    private async void OnSelectPaymentMethodClicked(object sender, EventArgs e)
    {
        // Example payment methods list (you can customize this)
        var paymentMethods = new List<string> { "Cash on Delivery", "Credit Card", "GCash" };

        var result = await DisplayActionSheet("Select Payment Method", "Cancel", null, paymentMethods.ToArray());

        if (!string.IsNullOrEmpty(result) && result != "Cancel")
        {
            PaymentMethod = result;
            OnPropertyChanged(nameof(PaymentMethod));
        }
    }

    private async void OnPlaceOrderClicked(object sender, EventArgs e)
    {
        string currentUser = Preferences.Get("UserId", "");

        if (string.IsNullOrEmpty(currentUser))
        {
            await DisplayAlert("Error", "User not logged in!", "OK");
            return;
        }

        var orderData = new
        {
            orderedBy = currentUser,
            product_id = Id,
            userName = UserName,
            deliveryAddress = DeliveryAddress,
            shippingMethod = SelectedShippingMethod,
            paymentMethod = PaymentMethod,
            subtotal = Subtotal,
            shippingFee = ShippingFee,
            discount = Discount,
            totalAmount = TotalPrice,
            notes = OrderNotes,
            items = CheckoutItems.Select(i => new
            {
                product_id = i.Id,
                quantity = i.Quantity,
                price = decimal.Parse(i.Price)
            }).ToList()
        };

        string json = JsonSerializer.Serialize(orderData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            LoadingOverlay.IsVisible = true;
            await Task.Delay(2000);
            var response = await client.PostAsync("https://concordecac.com/AndroidAppMaui/checkout.php", content);

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Error", "Failed to place order. Please try again.", "OK");
                return;
            }

            var result = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(result);

            if (jsonResponse != null && jsonResponse.TryGetValue("message", out string message))
            {
                // Remove only the ordered items from the cart
                foreach (var item in CheckoutItems)
                {
                    CartService.Instance.RemoveFromCart(item);
                }

                await DisplayAlert("Success", "Order placed successfully!", "OK");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await DisplayAlert("Error", jsonResponse?["error"] ?? "Unknown error occurred.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Checkout failed: {ex.Message}", "OK");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

    // Implement INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}