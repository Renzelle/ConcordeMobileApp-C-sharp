using System;
using Newtonsoft.Json;

using System;

using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Concorde
{
    public partial class EditProfilePage : ContentPage
    {
        public EditProfilePage()
        {
            InitializeComponent();
            LoadUserData();
        }

        private void LoadUserData()
        {
            try
            {
                if (AccountPage.LoggedInUser != null)
                {
                    FullNameEntry.Text = $"{AccountPage.LoggedInUser.Firstname} {AccountPage.LoggedInUser.Lastname}";
                    EmailEntry.Text = AccountPage.LoggedInUser.Email;
                    PasswordEntry.Text = AccountPage.LoggedInUser.Password;
                    GenderPicker.SelectedItem = AccountPage.LoggedInUser.Gender;

                    if (DateTime.TryParse(AccountPage.LoggedInUser.Birthday, out DateTime birthday))
                        BirthdayPicker.Date = birthday;
                    else
                        BirthdayPicker.Date = DateTime.Now;

                    ContactEntry.Text = AccountPage.LoggedInUser.Contact;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadUserData Error: {ex.Message}");
                // Optional: Show user-friendly alert
                // DisplayAlert("Error", "Something went wrong loading user data.", "OK");
            }
        }

        private async void OnSaveChangesClicked(object sender, EventArgs e)
        {
            if (AccountPage.LoggedInUser == null)
            {
                await DisplayAlert("Error", "No user logged in.", "OK");
                return;
            }

            string fullName = FullNameEntry.Text?.Trim();
            string email = EmailEntry.Text?.Trim();
            string password = PasswordEntry.Text?.Trim();
            string gender = GenderPicker.SelectedItem?.ToString();
            string birthday = BirthdayPicker.Date.ToString("yyyy-MM-dd");
            string contact = ContactEntry.Text?.Trim();

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
            {
                await DisplayAlert("Error", "Full Name and Email are required.", "OK");
                return;
            }

            ShowLoader();

            var updateData = new
            {
                Id = AccountPage.LoggedInUser.Id,
                Firstname = fullName.Split(' ')[0],
                Lastname = fullName.Contains(" ") ? fullName.Substring(fullName.IndexOf(" ") + 1) : "",
                Email = email,
                Password = password,
                Gender = gender,
                Birthday = birthday,
                ContactNumber = contact
            };

            string jsonData = JsonConvert.SerializeObject(updateData);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            using var client = new HttpClient();
            var response = await client.PostAsync("https://concordecac.com/AndroidAppMaui/update_profile.php", content);

            HideLoader();

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Success", "Profile updated successfully!", "OK");

                // Update local user data
                AccountPage.LoggedInUser.Firstname = fullName.Split(' ')[0];
                AccountPage.LoggedInUser.Lastname = fullName.Contains(" ") ? fullName.Substring(fullName.IndexOf(" ") + 1) : "";
                AccountPage.LoggedInUser.Email = email;
                AccountPage.LoggedInUser.Gender = gender;
                AccountPage.LoggedInUser.Password = password;
                AccountPage.LoggedInUser.Birthday = birthday;
                AccountPage.LoggedInUser.Contact = contact;

                // Save updated user info in Preferences (if needed)
                Preferences.Set("LoggedInUser", JsonConvert.SerializeObject(AccountPage.LoggedInUser));

                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Error", "Failed to update profile. Try again later.", "OK");
            }
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
    }
}