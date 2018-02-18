using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RoboRuckus
{
	public partial class MainPage : ContentPage
	{
        private string ssid = "RuckusSetup";
        private string psk = "Ruckus_C0nf";
        private Label message;
        Button connect;
        Button configure;

        public MainPage()
		{
            InitializeComponent();
            message = new Label {
                Text = "Put robot in Setup Mode and push connect",
                HorizontalTextAlignment = TextAlignment.Center,
                FontSize = 25
            };

            connect = new Button
            {
               Text = "Connect",
               VerticalOptions = LayoutOptions.Center,
               HorizontalOptions = LayoutOptions.Center,
            };      
            connect.Clicked += connect_Clicked;

            configure = new Button
            {
                Text = "Configure",
                IsEnabled = false,
                HorizontalOptions = LayoutOptions.Center,
            };
            configure.Clicked += configure_Clicked;

            mainLayout.Children.Add(message);
            mainLayout.Children.Add(connect);
            mainLayout.Children.Add(configure);
        }

        async private void configure_Clicked(object sender, EventArgs e)
        {
            CommandsPage commandsPage = new CommandsPage();
            await Navigation.PushModalAsync(commandsPage);
            await commandsPage.modalClosed; // May be unnecessary
            configure.IsEnabled = false;
            message.Text = "Put robot in Setup Mode and push connect";
            IWifiConnector remover = DependencyService.Get<IWifiConnector>();
            remover.remove(ssid);
        }

        async private void connect_Clicked(object sender, EventArgs e)
        {
            WiFiPage connectPage = new WiFiPage(ssid, psk);
            await Navigation.PushModalAsync(connectPage);
            await connectPage.modalClosed;
            if (connectPage.modalClosed.Result)
            {
                message.Text = "Connected! Push configure to tune the robot.";
                configure.IsEnabled = true;
            }
            else
            {
                message.Text = "Could not connect to robot, please try again.";
            }
        }
    }
}
