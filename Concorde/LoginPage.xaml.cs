using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Concorde
{
    public partial class LoginPage : ContentPage
    {
        private readonly HttpClient client = new();

        public LoginPage()
        {
            InitializeComponent();
        }

        private async void OnLoginButtonClicked(object sender, EventArgs e)
        {
            string username = UsernameEntry.Text?.Trim();
            string password = PasswordEntry.Text?.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Error", "Please fill in both fields.", "OK");
                return;
            }

            await Login(username, password);
        }

        private bool _isPasswordVisible = false;

        private void OnShowPasswordClicked(object sender, EventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            PasswordEntry.IsPassword = !_isPasswordVisible;

            // Change icon based on visibility
            ShowPasswordButton.Source = _isPasswordVisible ? "view.png" : "hide.png";
        }

        private async Task Login(string username, string password)
        {
            try
            {
                // ✅ Show loading indicator
                ShowLoading(true);

                string url = "https://concordecac.com/AndroidAppMaui/login.php";

                var payload = new
                {
                    username = username,
                    password = password
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                using var response = await client.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"Login API Response: {responseString}");

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<LoginResponse>(responseString, options);

                if (result != null && result.Status == "success")
                {
                    await DisplayAlert("Success", result.Message, "OK");

                    try
                    {
                        Debug.WriteLine($"Navigating with user: {result.User?.Firstname} {result.User?.Lastname}");

                        AccountPage.LoggedInUser = result.User;
                        Preferences.Set("UserId", result.User.Id);
                        Preferences.Set("Firstname", result.User.Firstname);
                        Preferences.Set("Lastname", result.User.Lastname);
                        Preferences.Set("Email", result.User.Email);
                        Preferences.Set("Password", result.User.Password);
                        Preferences.Set("Gender", result.User.Gender);
                        Preferences.Set("Birthday", result.User.Birthday);
                        Preferences.Set("Contact", result.User.Contact);
                        Preferences.Set("Locations", result.User.Locations);
                        Preferences.Set("Region", result.User.Region);
                        Preferences.Set("Province", result.User.Province);
                        Preferences.Set("City", result.User.City);
                        Preferences.Set("Barangay", result.User.Barangay);
                        Preferences.Set("Zip_code", result.User.Zip_code);
                        Preferences.Set("ProfilePicture", result.User.ProfilePicture);
                        Preferences.Set("Usertype", result.User.Usertype);

                        // ✅ Navigate after login
                        await Shell.Current.GoToAsync($"///{nameof(AccountPage)}");
                    }
                    catch (Exception navEx)
                    {
                        Debug.WriteLine($"Navigation error: {navEx.Message}");
                        await DisplayAlert("Navigation Error", navEx.Message, "OK");
                    }

                    return;
                }
                else
                {
                    await DisplayAlert("Login Failed", result?.Message ?? "Unknown error", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Login error: {ex.Message}");
                await DisplayAlert("Error", "Failed to connect to server.", "OK");
            }
            finally
            {
                // ✅ Hide loading indicator
                ShowLoading(false);
            }
        }

        private void ShowLoading(bool show)
        {
            LoadingOverlay.IsVisible = show;
            LoadingIndicator.IsRunning = show;
        }

        private async void OnSignUpTapped(object sender, EventArgs e)
        {
            // ✅ Navigate to signup page
            // await Navigation.PushAsync(new SignupPage());
            await DisplayAlert("Navigate", "Navigate to signup page.", "OK");
        }

        private void OnEntryCompleted(object sender, EventArgs e)
        {
            OnLoginButtonClicked(sender, e);
        }
    }

    public class LoginResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public UserInfo User { get; set; }
    }

    public class UserInfo
    {
        public string Id { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Contact { get; set; }
        public string Gender { get; set; }
        public string Birthday { get; set; }
        public string Locations { get; set; }
        public string Region { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string Barangay { get; set; }
        public string Zip_code { get; set; }
        public string ProfilePicture { get; set; }
        public string Usertype { get; set; }
    }
}