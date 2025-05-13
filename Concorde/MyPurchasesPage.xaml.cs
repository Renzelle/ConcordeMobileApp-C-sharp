using System;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;

namespace Concorde
{
    public partial class MyPurchasesPage : ContentPage
    {
        private ObservableCollection<Order> Orders = new ObservableCollection<Order>();
        private const string ApiBaseUrl = "https://concordecac.com/AndroidAppMaui/";
        private static readonly HttpClient client = new HttpClient();

        public MyPurchasesPage()
        {
            InitializeComponent();
            LoadOrders("Pending");
        }

        private void OnTabClicked(object sender, EventArgs e)
        {
            if (sender is Button clickedButton)
            {
                string selectedTab = clickedButton.ClassId;

                foreach (var child in TabsLayout.Children)
                {
                    if (child is Button button)
                    {
                        if (button.ClassId == selectedTab)
                        {
                            button.BackgroundColor = Colors.Black;
                            button.TextColor = Colors.White;
                            button.FontAttributes = FontAttributes.Bold;
                        }
                        else
                        {
                            button.BackgroundColor = Colors.Transparent;
                            button.TextColor = Colors.Black;
                            button.FontAttributes = FontAttributes.None;
                        }
                    }
                }

                string status = selectedTab switch
                {
                    "ToPay" => "Pending",
                    "ToShip" => "ToShip",
                    "ToReceive" => "ToReceive",
                    "Completed" => "Completed",
                    "ReturnRefund" => "ReturnRefund",
                    "Cancelled" => "Cancelled",
                    _ => ""
                };
                LoadOrders(status);
            }
        }

        private async void LoadOrders(string status)
        {
            try
            {
                Orders.Clear();
                EmptyOrdersLabel.IsVisible = false;
                var userId = Preferences.Get("UserId", "");

                if (string.IsNullOrEmpty(userId))
                {
                    await DisplayAlert("Error", "User not logged in.", "OK");
                    return;
                }

                var requestData = new { orderedBy = userId, status = status.Replace(" ", "") };
                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(ApiBaseUrl + "get_orders.php", content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<OrderResponse>(result);

                    if (apiResponse?.status == "success" && apiResponse.orders != null && apiResponse.orders.Count > 0)
                    {
                        foreach (var order in apiResponse.orders)
                        {
                            Orders.Add(new Order
                            {
                                OrderId = order.OrderId,
                                Status = order.Status,
                                TotalAmount = order.TotalAmount,
                                Items = order.Items ?? new List<OrderItem>()
                            });
                        }
                        EmptyOrdersImage.IsVisible = false; // Hide empty image
                        EmptyOrdersLabel.IsVisible = false;
                    }
                    else
                    {
                        EmptyOrdersImage.IsVisible = true; // Show empty image
                        EmptyOrdersLabel.IsVisible = true;
                    }
                }
                else
                {
                    EmptyOrdersImage.IsVisible = true; // Show empty image
                    EmptyOrdersLabel.IsVisible = true;
                }

                OrdersCollectionView.ItemsSource = Orders;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
                EmptyOrdersImage.IsVisible = true; // Show empty image
                EmptyOrdersLabel.IsVisible = true;
            }
        }

        private async void OnBuyAgainClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var order = button?.BindingContext as Order;

            if (order != null)
            {
                bool allAdded = true;
                foreach (var item in order.Items)
                {
                    bool isAdded = await AddItemToCartDatabase(item);
                    if (!isAdded) allAdded = false;
                }

                if (allAdded)
                {
                    MessagingCenter.Send(this, "CartUpdated", true);
                    await DisplayAlert("Success", "All items added to cart!", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "Some items couldn't be added.", "OK");
                }
            }
        }

        private async void OnCompleteOrderClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var order = button?.BindingContext as Order;

            if (order == null) return;

            bool confirm = await DisplayAlert("Confirm", "Mark this order as completed?", "Yes", "No");
            if (!confirm) return;

            try
            {
                var requestData = new { order_id = order.OrderId, status = "Completed" };
                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(ApiBaseUrl + "update_order_status.php", content);
                var result = await response.Content.ReadAsStringAsync();

                var apiResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(result);

                if (apiResponse != null && apiResponse.ContainsKey("status") && apiResponse["status"].ToString() == "success")
                {
                    await DisplayAlert("Success", "Order marked as completed!", "OK");
                    LoadOrders("ToReceive"); // Refresh "To Receive" orders
                }
                else
                {
                    await DisplayAlert("Error", "Failed to update order status.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private async Task<bool> AddItemToCartDatabase(OrderItem item)
        {
            try
            {
                var url = ApiBaseUrl + "add_to_cart.php";
                var orderedBy = Preferences.Get("UserId", "");

                if (string.IsNullOrEmpty(orderedBy))
                {
                    await DisplayAlert("Error", "User not logged in!", "OK");
                    return false;
                }

                var values = new Dictionary<string, string>
                {
                    { "product_id", item.product_id.ToString() },
                    { "product_name", item.product_name },
                    { "image_url", item.FirstImage },
                    { "quantity", item.quantity.ToString() },
                    { "price", item.price.ToString() },
                    { "orderedBy", orderedBy }
                };

                var content = new FormUrlEncodedContent(values);
                using var response = await client.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseString);

                return jsonResponse != null && jsonResponse.ContainsKey("success") && (bool)jsonResponse["success"];
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async void OnCancelOrderClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var order = button?.BindingContext as Order;

            if (order == null) return;

            bool confirm = await DisplayAlert("Cancel Order", "Are you sure?", "Yes", "No");
            if (!confirm) return;

            try
            {
                var requestData = new { order_id = order.OrderId, status = "Cancelled" };
                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(ApiBaseUrl + "update_order_status.php", content);
                var result = await response.Content.ReadAsStringAsync();

                var apiResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(result);

                if (apiResponse != null && apiResponse.ContainsKey("status") && apiResponse["status"].ToString() == "success")
                {
                    await DisplayAlert("Success", "Order cancelled!", "OK");
                    LoadOrders("Pending");
                }
                else
                {
                    await DisplayAlert("Error", "Cancellation failed.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        public class OrderResponse
        {
            public string status { get; set; }
            public List<Order> orders { get; set; }
        }

        public class Order
        {
            [JsonProperty("id")]
            public int OrderId { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("totalAmount")]
            public double TotalAmount { get; set; }

            [JsonProperty("items")]
            public List<OrderItem> Items { get; set; }

            public string StatusIcon => Status switch
            {
                "Pending" => "💰",
                "ToShip" => "🚀",
                "ToReceive" => "📦",
                "Completed" => "✅",
                "ReturnRefund" => "🔄",
                "Cancelled" => "❌",
                _ => "❓"
            };

            public bool CanBuyAgain => Items != null && Items.Any(item => item.Stock > 0);
        }

        public class OrderItem
        {
            public int product_id { get; set; }

            [JsonProperty("model")]
            public string product_name { get; set; }

            [JsonProperty("image")]
            public string image { get; set; }

            public int Stock { get; set; }
            public int quantity { get; set; }
            public double price { get; set; }

            public string FirstImage => string.IsNullOrEmpty(image) ? null : image.Split(',')[0];
        }
    }
}