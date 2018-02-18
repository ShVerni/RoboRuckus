
using System.Collections.Generic;
using Android.Content;
using Xamarin.Forms;
using Android.Net.Wifi;
using RoboRuckus.Droid;
using System.Threading.Tasks;

[assembly: Dependency(typeof(WifiScanner))]
namespace RoboRuckus.Droid
{
    public class WifiScanner : IWifiScanner
    {

        public bool done
        {
            get
            {
                return _done;
            }
        }

        public List<string> WiFiNetworks
        {
            get
            {
                return _WiFiNetworks;
            }
        }

        private WifiReceiver wifiReceiver;
        private static List<string> _WiFiNetworks = new List<string>();
        private static WifiManager wifi;
        private static TaskCompletionSource<bool> networksScanned = new TaskCompletionSource<bool>();
        private static bool _done = false;

        public void getNetworks()
        {
            _done = false;
            var context = Android.App.Application.Context;
            if (context == null)
            {
                return;
            }

            wifi = (WifiManager)context.GetSystemService(Context.WifiService);
            if (!wifi.IsWifiEnabled)
            {
                wifi.SetWifiEnabled(true);
            }
            wifiReceiver = new WifiReceiver();
            context.RegisterReceiver(wifiReceiver, new IntentFilter(WifiManager.ScanResultsAvailableAction));
            wifi.StartScan();            
        }

        class WifiReceiver : BroadcastReceiver
        {
            public override void OnReceive(Context context, Intent intent)
            {
                IList<ScanResult> scanwifinetworks = wifi.ScanResults;
                foreach (ScanResult wifinetwork in scanwifinetworks)
                {
                    _WiFiNetworks.Add(wifinetwork.Ssid);
                }
                _done = true;
            }
        }

    }

}