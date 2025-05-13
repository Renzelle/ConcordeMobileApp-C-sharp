using System.Collections.ObjectModel;

namespace Concorde
{
    public partial class HelpCentrePage : ContentPage
    {
        public ObservableCollection<HelpTopic> HelpTopics { get; set; }
        public ObservableCollection<FAQ> FAQs { get; set; }

        public HelpCentrePage()
        {
            InitializeComponent();

            HelpTopics = new ObservableCollection<HelpTopic>
            {
                new HelpTopic { Title = "Order Issues", Icon = "order.png" },
                new HelpTopic { Title = "Payment Methods", Icon = "payment.png" },
                new HelpTopic { Title = "Shipping & Delivery", Icon = "shipping.png" },
                new HelpTopic { Title = "Returns & Refunds", Icon = "returns.png" },
                new HelpTopic { Title = "Account & Security", Icon = "account.png" }
            };

            FAQs = new ObservableCollection<FAQ>
            {
                new FAQ { Question = "How can I track my order?", Answer = "You can track your order by going to the 'Orders' page in your account." },
                new FAQ { Question = "What payment methods do you accept?", Answer = "We accept credit/debit cards, PayPal, and cash on delivery." },
                new FAQ { Question = "How do I return a product?", Answer = "Go to 'My Orders', select the item, and request a return." }
            };

            BindingContext = this;
        }

        private async void OnContactSupportClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Support", "Contact us at support@concordecac.com", "OK");
        }

        private async void OnChatbotClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Support", "Not Yet Working - Developer", "OK");
            //await Navigation.PushAsync(new ChatPage());
        }
    }

    public class HelpTopic
    {
        public string Title { get; set; }
        public string Icon { get; set; }
    }

    public class FAQ
    {
        public string Question { get; set; }
        public string Answer { get; set; }
    }
}