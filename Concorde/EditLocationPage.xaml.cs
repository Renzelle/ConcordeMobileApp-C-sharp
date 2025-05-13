using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace Concorde;

public partial class EditLocationPage : ContentPage
{
    // Example data structure
    private Dictionary<string, List<string>> regionProvinces = new();

    private Dictionary<string, List<string>> provinceCities = new();
    private Dictionary<string, List<string>> cityBarangays = new();

    public EditLocationPage()
    {
        InitializeComponent();
        LoadUserData();

        // Sample Data
        regionProvinces = new Dictionary<string, List<string>>
        {
            { "NCR", new List<string> { "Metro Manila" } },
            { "Region IV-A", new List<string> { "Laguna", "Cavite" } },
        };

        provinceCities = new Dictionary<string, List<string>>
        {
            { "Metro Manila", new List<string> { "Manila", "Quezon City", "Makati" } },
            { "Laguna", new List<string> { "Calamba", "San Pablo" } },
            { "Cavite", new List<string> { "Bacoor", "Imus" } },
        };

        cityBarangays = new Dictionary<string, List<string>>
        {
            { "Manila", new List<string> { "Barangay 1", "Barangay 2" } },
            { "Quezon City", new List<string> { "Commonwealth", "Fairview" } },
            { "Makati", new List<string> { "Bel-Air", "Poblacion" } },
            { "Calamba", new List<string> { "Barangay Uno", "Barangay Dos" } },
            { "San Pablo", new List<string> { "Barangay San Rafael", "Barangay San Nicolas" } },
            { "Bacoor", new List<string> { "Mambog", "Molino" } },
            { "Imus", new List<string> { "Alapan", "Anabu" } },
        };

        // Initialize Region Picker
        RegionPicker.ItemsSource = regionProvinces.Keys.ToList();
        RegionPicker.SelectedIndexChanged += RegionPicker_SelectedIndexChanged;
        ProvincePicker.SelectedIndexChanged += ProvincePicker_SelectedIndexChanged;
        CityPicker.SelectedIndexChanged += CityPicker_SelectedIndexChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadUserData();
    }

    private void LoadUserData()
    {
        try
        {
            if (AccountPage.LoggedInUser != null)
            {
                var user = AccountPage.LoggedInUser;

                Debug.WriteLine($"Region: {user.Region}, Province: {user.Province}, City: {user.City}, Barangay: {user.Barangay}");

                // REGION
                if (!string.IsNullOrEmpty(user.Region))
                {
                    RegionPicker.SelectedItem = user.Region;

                    if (regionProvinces.ContainsKey(user.Region))
                    {
                        ProvincePicker.ItemsSource = regionProvinces[user.Region];
                        ProvincePicker.IsEnabled = true;
                    }
                }

                // PROVINCE
                if (!string.IsNullOrEmpty(user.Province))
                {
                    ProvincePicker.SelectedItem = user.Province;

                    if (provinceCities.ContainsKey(user.Province))
                    {
                        CityPicker.ItemsSource = provinceCities[user.Province];
                        CityPicker.IsEnabled = true;
                    }
                }

                // CITY
                if (!string.IsNullOrEmpty(user.City))
                {
                    CityPicker.SelectedItem = user.City;

                    if (cityBarangays.ContainsKey(user.City))
                    {
                        BarangayPicker.ItemsSource = cityBarangays[user.City];
                        BarangayPicker.IsEnabled = true;
                    }
                }

                // BARANGAY
                if (!string.IsNullOrEmpty(user.Barangay))
                {
                    BarangayPicker.SelectedItem = user.Barangay;
                }

                // LOCATIONS & ZIPCODE
                HomeAddressEntry.Text = user.Locations ?? "";
                ZipCodeEntry.Text = user.Zip_code ?? "";

                Debug.WriteLine("User data loaded successfully.");
            }
            else
            {
                Debug.WriteLine("LoggedInUser is null.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LoadUserData Error: {ex.Message}");
        }
    }

    private void RegionPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        var selectedRegion = RegionPicker.SelectedItem?.ToString();

        if (!string.IsNullOrEmpty(selectedRegion) && regionProvinces.ContainsKey(selectedRegion))
        {
            ProvincePicker.ItemsSource = regionProvinces[selectedRegion];
            ProvincePicker.IsEnabled = true;

            // Reset lower pickers
            ProvincePicker.SelectedIndex = -1;
            CityPicker.ItemsSource = null; CityPicker.IsEnabled = false;
            BarangayPicker.ItemsSource = null; BarangayPicker.IsEnabled = false;
        }
    }

    private void ProvincePicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        var selectedProvince = ProvincePicker.SelectedItem?.ToString();

        if (!string.IsNullOrEmpty(selectedProvince) && provinceCities.ContainsKey(selectedProvince))
        {
            CityPicker.ItemsSource = provinceCities[selectedProvince];
            CityPicker.IsEnabled = true;

            // Reset lower pickers
            CityPicker.SelectedIndex = -1;
            BarangayPicker.ItemsSource = null; BarangayPicker.IsEnabled = false;
        }
    }

    private void CityPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        var selectedCity = CityPicker.SelectedItem?.ToString();

        if (!string.IsNullOrEmpty(selectedCity) && cityBarangays.ContainsKey(selectedCity))
        {
            BarangayPicker.ItemsSource = cityBarangays[selectedCity];
            BarangayPicker.IsEnabled = true;

            // Reset barangay picker
            BarangayPicker.SelectedIndex = -1;
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var region = RegionPicker.SelectedItem?.ToString() ?? "";
        var province = ProvincePicker.SelectedItem?.ToString() ?? "";
        var city = CityPicker.SelectedItem?.ToString() ?? "";
        var barangay = BarangayPicker.SelectedItem?.ToString() ?? "";
        var locations = HomeAddressEntry.Text ?? "";
        var zipCode = ZipCodeEntry.Text ?? "";
        var userIdStr = AccountPage.LoggedInUser.Id;

        if (!int.TryParse(userIdStr, out int userId) || userId == 0)
        {
            await DisplayAlert("Error", $"User ID is invalid! Value: {userIdStr}", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(region) || string.IsNullOrWhiteSpace(province) ||
            string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(barangay) ||
            string.IsNullOrWhiteSpace(locations) || string.IsNullOrWhiteSpace(zipCode))
        {
            await DisplayAlert("Error", "Please complete all fields.", "OK");
            return;
        }

        var jsonData = new
        {
            id = userId,
            region = region,
            province = province,
            city = city,
            barangay = barangay,
            locations = locations,
            zip_code = zipCode
        };

        var json = JsonConvert.SerializeObject(jsonData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using (var client = new HttpClient())
        {
            try
            {
                ShowLoading(true);
                await Task.Delay(2000);
                // URL of your PHP script (adjust based on your domain or localhost)
                var url = "https://concordecac.com/AndroidAppMaui/update_user_address.php";

                var response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine("Response: " + jsonResponse);

                    var result = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

                    if (result.success == true)
                    {
                        await DisplayAlert("Success", "Address updated successfully!", "OK");
                        await Navigation.PopAsync(); // Go back to previous page
                    }
                    else
                    {
                        await DisplayAlert("Error", (string)result.message, "OK");
                    }
                }
                else
                {
                    await DisplayAlert("Error", "Server error: " + response.StatusCode, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Exception: " + ex.Message, "OK");
            }
            finally
            {
                ShowLoading(false);
            }
        }
    }

    private async void ShowLoading(bool show)
    {
        if (show)
        {
            LoadingOverlay.IsVisible = true;
            await LoadingOverlay.FadeTo(1, 250); // Optional fade in
        }
        else
        {
            await LoadingOverlay.FadeTo(0, 250);
            LoadingOverlay.IsVisible = false;
        }
    }
}