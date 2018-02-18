using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RoboRuckus
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class WiFiPage : ContentPage
    {
        public Task<bool> modalClosed {
            get
            {
                return tcs.Task;
            }
        }

        private TaskCompletionSource<bool> tcs;
        private Label message;
        private ActivityIndicator working;
        private string _ssid;
        private string _psk;

        public WiFiPage(string ssid, string psk)
        {
            _ssid = ssid;
            _psk = psk;
            tcs = new TaskCompletionSource<bool>();
            InitializeComponent();
            message = new Label { Text = "Scanning for Wi-Fi network..." };
            working = new ActivityIndicator { IsRunning = true, Color = Color.Blue };
            mainLayout.Children.Add(message);
            Task.Run(() => wifiCofig());
        }

        private async Task<bool> wifiCofig()
        {
            bool scanSucceeded = false;
            bool connected = false;
            scanSucceeded = await Task.Run(() => startScan());
            if (scanSucceeded)
            {
                connected = connectToNetwork();
            }
            string result = "Uknown error";
            if (!scanSucceeded)
            {
                result = "Network not found";
                tcs.SetResult(false);
            }
            else if (!connected)
            {
                result = "Could not connect to network";
                tcs.SetResult(false);
            }
            else
            {
                result = "Connected!";
                tcs.SetResult(true);
            }
            Device.BeginInvokeOnMainThread(async () =>
            {
                working.IsRunning = false;
                message.Text = result;               
                await Task.Delay(2500);
                await Navigation.PopModalAsync();
            });
            return scanSucceeded && connected;
        }

        private async Task<bool> startScan()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                message.Text = "Scanning for Wi-Fi network...";
                mainLayout.Children.Add(working);
            });

            IWifiScanner scanner = DependencyService.Get<IWifiScanner>();
            new Thread(() => scanner.getNetworks()).Start();

            int i = 0;
            do
            {
                await Task.Delay(100);
                i++;
            } while (!scanner.done && i < 100);

            string list = "";
            foreach (string network in scanner.WiFiNetworks)
            {
                list += network + "\n";
            }

            if (i >= 100)
            {
                return false;
            }
            if (!scanner.WiFiNetworks.Contains(_ssid))
            {
                return false;
            }
            return true;
        }

        private bool connectToNetwork()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                message.Text = "Connecting to network...";
            });
            IWifiConnector connector = DependencyService.Get<IWifiConnector>();
            return connector.connect(_ssid, _psk);
        }

        protected override bool OnBackButtonPressed()
        {
            return true;
        }
    }
}