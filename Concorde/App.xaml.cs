namespace Concorde;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(ProductBrandPage), typeof(ProductBrandPage));
        Routing.RegisterRoute(nameof(AccountPage), typeof(AccountPage));
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(EditProfilePage), typeof(EditProfilePage));
        Application.Current.UserAppTheme = AppTheme.Light;

        MainPage = new AppShell(); // Ensure Shell is set first

        Dispatcher.Dispatch(async () =>
        {
            if (Preferences.ContainsKey("UserId"))
            {
                AccountPage.LoggedInUser = new UserInfo
                {
                    Id = Preferences.Get("UserId", ""),
                    Firstname = Preferences.Get("Firstname", ""),
                    Lastname = Preferences.Get("Lastname", ""),
                    Email = Preferences.Get("Email", ""),
                    Password = Preferences.Get("Password", ""),
                    Contact = Preferences.Get("Contact", ""),
                    Birthday = Preferences.Get("Birthday", ""),
                    Gender = Preferences.Get("Gender", ""),
                    Locations = Preferences.Get("Locations", ""),
                    Region = Preferences.Get("Region", ""),
                    Province = Preferences.Get("Province", ""),
                    City = Preferences.Get("City", ""),
                    Barangay = Preferences.Get("Barangay", ""),
                    Zip_code = Preferences.Get("Zip_code", ""),
                    ProfilePicture = Preferences.Get("ProfilePicture", ""),
                    Usertype = Preferences.Get("Usertype", "")
                };
                await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
            }
            else
            {
                await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
            }
        });
    }
}