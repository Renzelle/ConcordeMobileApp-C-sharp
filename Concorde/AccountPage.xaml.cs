using System;
using Newtonsoft.Json;

using System.Diagnostics;
using System.Text.Json;

namespace Concorde
{
    public partial class AccountPage : ContentPage
    {
        public static UserInfo LoggedInUser { get; set; } // ✅ Static property (or use a Singleton/service)
        private readonly HttpClient client = new();

        private int cartItemCount = 0;

        public AccountPage()
        {
            InitializeComponent();
            this.Loaded += AccountPage_Loaded;
            GetCartProductCountFromServer();
        }

        private void AccountPage_Loaded(object sender, EventArgs e)
        {
            if (LoggedInUser != null)
            {
                FullNameLabel.Text = $"{LoggedInUser.Firstname} {LoggedInUser.Lastname}";

                // ✅ Show camera icon if logged in
                CameraIcon.IsVisible = true;

                // Show Logout
                LoginLogoutLabel.Text = "Logout";
                LoginLogoutImage.Source = "logout.png";
                if (!string.IsNullOrEmpty(LoggedInUser.ProfilePicture))
                {
                    ProfileImage.Source = LoggedInUser.ProfilePicture; // ✅ Set profile picture
                }
                else
                {
                    ProfileImage.Source = "profiles.png"; // ✅ Default image if no picture
                }
            }
            else
            {
                FullNameLabel.Text = "Guest User";

                // ✅ Hide camera icon if not logged in
                CameraIcon.IsVisible = false;

                // Show Login
                LoginLogoutLabel.Text = "Login";
                LoginLogoutImage.Source = "login.png";
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (LoggedInUser != null)
            {
                FullNameLabel.Text = $"{LoggedInUser.Firstname} {LoggedInUser.Lastname}";

                // ✅ Show camera icon
                CameraIcon.IsVisible = true;

                LoginLogoutLabel.Text = "Logout";
                LoginLogoutImage.Source = "logout.png";
                ProfileImage.Source = !string.IsNullOrEmpty(LoggedInUser.ProfilePicture)
                    ? LoggedInUser.ProfilePicture
                    : "profiles.png";
            }
            else
            {
                Debug.WriteLine("LoggedInUser is null");

                // ✅ Hide camera icon
                CameraIcon.IsVisible = false;

                FullNameLabel.Text = "Guest User";

                LoginLogoutLabel.Text = "Login";
                LoginLogoutImage.Source = "login.png";
            }
        }

        private async void OnCartTapped(object sender, EventArgs e)
        {
            //await Navigation.PushAsync(new CartPage());
        }

        private async void OnEditLocationClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new EditLocationPage());
        }

        private async void OnHelpCentreClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new HelpCentrePage());
        }

        private async void OnLoginLogoutButtonClicked(object sender, EventArgs e)
        {
            if (LoggedInUser != null)
            {
                // ✅ User is logged in, so this button becomes "Logout"
                bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "Cancel");

                if (confirm)
                {
                    // Clear login info
                    LoggedInUser = null;
                    Preferences.Clear();
                    CartService.Instance.ClearCart();

                    // Reset UI
                    FullNameLabel.Text = "Guest User";

                    LoginLogoutLabel.Text = "Login";
                    LoginLogoutImage.Source = "login.png";

                    await DisplayAlert("Logged Out", "You have been logged out.", "OK");

                    await Shell.Current.GoToAsync($"//{nameof(AccountPage)}");
                }
            }
            else
            {
                // ✅ User not logged in, go to login page
                await Shell.Current.GoToAsync(nameof(LoginPage));
            }
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("User", out var userObj) && userObj is UserInfo user)
            {
                LoggedInUser = user;

                FullNameLabel.Text = $"{user.Firstname} {user.Lastname}";

                LoginLogoutLabel.Text = "Logout";
                LoginLogoutImage.Source = "logout.png";

                ProfileImage.Source = !string.IsNullOrEmpty(user.ProfilePicture)
                    ? user.ProfilePicture
                    : "profiles.png";
            }
        }

        private async void OnCameraIconTapped(object sender, EventArgs e)
        {
            if (LoggedInUser == null)
            {
                await DisplayAlert("Not Logged In", "Please log in to change your profile picture.", "OK");
                return;
            }

            string action = await DisplayActionSheet("Change Profile Picture", "Cancel", null, "Take Photo", "Upload from Gallery");

            FileResult photo = null;

            switch (action)
            {
                case "Take Photo":
                    photo = await MediaPicker.CapturePhotoAsync();
                    break;

                case "Upload from Gallery":
                    photo = await MediaPicker.PickPhotoAsync();
                    break;
            }

            if (photo != null)
            {
                await UploadProfilePicture(photo);
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

                var result = JsonConvert.DeserializeObject<Dictionary<string, int>>(responseString);

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

        private async Task UploadProfilePicture(FileResult photo)
        {
            if (photo == null)
                return;

            ShowLoader();

            try
            {
                using var content = new MultipartFormDataContent();

                using var stream = await photo.OpenReadAsync();
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

                content.Add(streamContent, "profile_picture", photo.FileName);
                content.Add(new StringContent(LoggedInUser.Id.ToString()), "user_id");

                using var client = new HttpClient();
                var response = await client.PostAsync("https://concordecac.com/AndroidAppMaui/upload_profile_picture.php", content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();

                    await DisplayAlert("Success", "Profile picture updated successfully!", "OK");

                    // Refresh UI image
                    ProfileImage.Source = ImageSource.FromFile(photo.FullPath);

                    // Optional: update LoggedInUser.ProfilePicture if needed
                    LoggedInUser.ProfilePicture = photo.FullPath;
                }
                else
                {
                    await DisplayAlert("Failed", "Failed to upload profile picture.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                HideLoader();
            }
        }

        private async void OnEditProfileTapped(object sender, EventArgs e)
        {
            if (LoggedInUser == null)
            {
                await DisplayAlert("Not Logged In", "Please log in to edit your profile.", "OK");
                return;
            }

            await Shell.Current.GoToAsync(nameof(EditProfilePage));
        }

        private async void OnMyPurchasesTapped(object sender, EventArgs e)
        {
            if (LoggedInUser == null)
            {
                await DisplayAlert("Login Required", "Please login to view your purchases.", "OK");
                return;
            }

            await Navigation.PushAsync(new MyPurchasesPage());
        }

        private async void ShowLoader()
        {
            LoadingOverlay.Opacity = 0;
            LoadingOverlay.IsVisible = true;
            await LoadingOverlay.FadeTo(1, 250);
        }

        private async void HideLoader()
        {
            await LoadingOverlay.FadeTo(0, 250);
            LoadingOverlay.IsVisible = false;
        }

        private async void OnSignUpButtonClicked(object sender, EventArgs e)
        {
            // Navigate to signup page or any logic you want
        }
    }
}