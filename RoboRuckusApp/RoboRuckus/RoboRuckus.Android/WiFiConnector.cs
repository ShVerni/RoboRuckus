using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using RoboRuckus.Droid;
using Xamarin.Forms;
using Android.Net.Wifi;
using Android.Net;
using System.Threading;

[assembly: Dependency(typeof(WifiConnector))]
namespace RoboRuckus.Droid
{
    public class WifiConnector : IWifiConnector
    {
        public bool connect(string ssid, string psk)
        {
            Context context = Android.App.Application.Context;
            if (context == null)
            {
                return false;
            }
            WifiConfiguration wifiConfig = new WifiConfiguration
            {
                Ssid = string.Format("\"{0}\"", ssid),
                PreSharedKey = string.Format("\"{0}\"", psk)
            };

            WifiManager wifi = (WifiManager)context.GetSystemService(Context.WifiService);

            _remove(ssid, wifi);

            int netId = wifi.AddNetwork(wifiConfig);
            wifi.Disconnect();
            if (wifi.EnableNetwork(netId, true))
            {
                if (wifi.Reconnect())
                {
                    ConnectivityManager connectivityManager = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);
                    NetworkInfo networkInfo;
                    int i = 0;
                    do
                    {
                        networkInfo = connectivityManager.ActiveNetworkInfo;
                        Thread.Sleep(100);
                        i++;
                    } while ((networkInfo == null || !(networkInfo.Type == ConnectivityType.Wifi)) && i < 30);
                    if (networkInfo != null && networkInfo.Type == ConnectivityType.Wifi)
                    {
                        return true;
                    }
                }
            }
            _remove(ssid, wifi);
            return false;
        }

        public bool remove(string ssid)
        {
            Context context = Android.App.Application.Context;
            if (context == null)
            {
                return false;
            }

            WifiManager wifi = (WifiManager)context.GetSystemService(Context.WifiService);
            _remove(ssid, wifi);
            return true;
        }

        private void _remove (string ssid, WifiManager wifi)
        {
            List<WifiConfiguration> existing = wifi.ConfiguredNetworks.Where(w => w.Ssid == "\"" + ssid + "\"").ToList<WifiConfiguration>();
            if (existing.Count() > 0)
            {
                foreach (WifiConfiguration config in existing)
                {
                    wifi.RemoveNetwork(config.NetworkId);
                }
            }
        }
    }
}