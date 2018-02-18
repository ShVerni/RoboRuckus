using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Net;
using System.Threading;

namespace RoboRuckus
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CommandsPage : ContentPage
    {

        public Task<bool> modalClosed
        {
            get
            {
                return tcs.Task;
            }
        }
        private TaskCompletionSource<bool> tcs;

        IPEndPoint robot = new IPEndPoint(IPAddress.Parse("192.168.3.1"), 8080);

        private string name = "";

        private Slider leftForwardSpeed;
        private Slider rightForwardSpeed;
        private Slider rightBackwardSpeed;
        private Slider leftBackwardSpeed;
        private Slider Z_threshold;
        private Slider turnBoost;
        private Slider drift_threshold;
        private Slider turnFactor;
        private Slider turn_drift_threshold;
        private Entry robotName;
        private Button quit;
        private Button speedTest;
        private Button navTest;
        private Button updateName;

        private Label message;
        private Label leftForwardSpeedLabel;
        private Label rightForwardSpeedLabel;
        private Label rightBackwardSpeedLabel;
        private Label leftBackwardSpeedLabel;
        private Label Z_thresholdLabel;
        private Label turnBoostLabel;
        private Label drift_thresholdLabel;
        private Label turnFactorLabel;
        private Label turn_drift_thresholdLabel;

        public CommandsPage()
        {
            tcs = new TaskCompletionSource<bool>();
            InitializeComponent();

            message = new Label { Text = "ready" };

            leftForwardSpeedLabel = new Label { Text = "Left Forward Speed: 94" };
            leftForwardSpeed = new Slider(88, 110, 94);

            leftBackwardSpeedLabel = new Label { Text = "Left Backward Speed: 94" };
            leftBackwardSpeed = new Slider(88, 110, 94);

            rightForwardSpeedLabel = new Label { Text = "Right Forward Speed: 94" };
            rightForwardSpeed = new Slider(88, 110, 94);

            rightBackwardSpeedLabel = new Label { Text = "Right Backward Speed: 94" };
            rightBackwardSpeed = new Slider(88, 110, 94);

            Z_thresholdLabel = new Label { Text = "Z Threshold: -100" };
            Z_threshold = new Slider(-200, 0, -100);

            turnBoostLabel = new Label { Text = "Turn Boot: 4" };
            turnBoost = new Slider(0, 10, 4);

            drift_thresholdLabel = new Label { Text = "Drift Threshold: 1" };
            drift_threshold = new Slider(0, 10, 1);

            turnFactorLabel = new Label { Text = "Turn Factor: 1.44" };
            turnFactor = new Slider(0.5, 2, 1.44);

            turn_drift_thresholdLabel = new Label { Text = "Turn Drift Threshold: 0.4" };
            turn_drift_threshold = new Slider(0, 1.5, 0.4);


            robotName = new Entry { Text = "Beta Bot", VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.FillAndExpand };
            updateName = new Button { Text = "Update Name", VerticalOptions = LayoutOptions.Center };
            updateName.Clicked += UpdateName_Clicked;

            speedTest = new Button { Text = "Speed Test", HorizontalOptions = LayoutOptions.Start };
            speedTest.Clicked += SpeedTest_Clicked;

            navTest = new Button { Text = "Nav Test", HorizontalOptions = LayoutOptions.Center };
            navTest.Clicked += NavTest_Clicked;

            quit = new Button { Text = "Quit", HorizontalOptions = LayoutOptions.End };
            quit.Clicked += Quit_Clicked;

            this.Content = new ScrollView
            {
                Content = new StackLayout
                {
                    Padding = new Thickness(10),
                    Children =
                    {
                        new Label{
                            Text = "Robot Parameters",
                            FontSize = 30,
                            FontAttributes = FontAttributes.Bold,
                            HorizontalOptions = LayoutOptions.Center
                        },
                        message,
                        leftForwardSpeedLabel,
                        leftForwardSpeed,
                        leftBackwardSpeedLabel,
                        leftBackwardSpeed,
                        rightForwardSpeedLabel,
                        rightForwardSpeed,
                        rightBackwardSpeedLabel,
                        rightBackwardSpeed,
                        Z_thresholdLabel,
                        Z_threshold,
                        turnBoostLabel,
                        turnBoost,
                        drift_thresholdLabel,
                        drift_threshold,
                        turnFactorLabel,
                        turnFactor,
                        turn_drift_thresholdLabel,
                        turn_drift_threshold,
                        new StackLayout
                        {
                            Children =
                            {
                                updateName,
                                robotName
                            },
                            Orientation = StackOrientation.Horizontal,
                            HorizontalOptions = LayoutOptions.FillAndExpand
                        },
                        new StackLayout
                        {
                            Children =
                            {
                                speedTest,
                                navTest,
                                quit
                            },
                            Orientation = StackOrientation.Horizontal,
                            HorizontalOptions = LayoutOptions.CenterAndExpand
                        }
                    }
                }
            };
            turn_drift_threshold.ValueChanged += Turn_drift_threshold_ValueChanged;
            

            turnFactor.ValueChanged += TurnFactor_ValueChanged;
            drift_threshold.ValueChanged += Drift_threshold_ValueChanged;
            turnBoost.ValueChanged += TurnBoost_ValueChanged;
            Z_threshold.ValueChanged += Z_threshold_ValueChanged;
            rightBackwardSpeed.ValueChanged += RightBackwardSpeed_ValueChanged;
            rightForwardSpeed.ValueChanged += RightForwardSpeed_ValueChanged;
            leftBackwardSpeed.ValueChanged += LeftBackwardSpeed_ValueChanged;
            leftForwardSpeed.ValueChanged += LeftForwardSpeed_ValueChanged;

            if (Device.RuntimePlatform != Device.Android)
            {
                turn_drift_threshold.Unfocused += Turn_drift_threshold_Unfocused;
                turnFactor.Unfocused += TurnFactor_Unfocused;
                turnBoost.Unfocused += TurnBoost_Unfocused;
                Z_threshold.Unfocused += Z_threshold_Unfocused;
                rightBackwardSpeed.Unfocused += RightBackwardSpeed_Unfocused;
                rightForwardSpeed.Unfocused += RightForwardSpeed_Unfocused;
                leftBackwardSpeed.Unfocused += LeftBackwardSpeed_Unfocused;
                leftForwardSpeed.Unfocused += LeftForwardSpeed_Unfocused;
            }

            refresh();

        }

        private bool refresh()
        {
            string result = sendData("10:0").Trim().Replace("\0", "");
            if (result != "ER" && result != "")
            {
                string[] values = result.Split(',');
                leftForwardSpeed.Value =  double.Parse(values[0]);
                rightForwardSpeed.Value = 180 - double.Parse(values[1]);
                rightBackwardSpeed.Value = double.Parse(values[2]);
                leftBackwardSpeed.Value = 180 - double.Parse(values[3]);
                Z_threshold.Value = double.Parse(values[4]);
                turnBoost.Value = double.Parse(values[5]);
                drift_threshold.Value = double.Parse(values[6]);
                turn_drift_threshold.Value = double.Parse(values[7]);
                turnFactor.Value = double.Parse(values[8]);
                robotName.Text = Uri.UnescapeDataString(values[9]);                
                return true;
            }
            return false;
        }

        private string sendData(string data)
        {
            using (TcpClient client = new TcpClient())
            {
                client.ReceiveTimeout = 5000;
                client.SendTimeout = 2000;
                IAsyncResult result = client.BeginConnect(robot.Address, robot.Port, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));
                if (success)
                {
                    NetworkStream stream = client.GetStream();
                    if (stream.CanWrite)
                    {
                        try
                        {
                            Byte[] sendBytes = Encoding.ASCII.GetBytes(data);
                            stream.Write(sendBytes, 0, sendBytes.Length);
                            if (stream.CanRead)
                            {
                                byte[] bytes = new byte[client.ReceiveBufferSize];
                                string message = "";
                                do
                                {
                                    stream.Read(bytes, 0, (int)client.ReceiveBufferSize);
                                    message += Encoding.ASCII.GetString(bytes);
                                } while (stream.DataAvailable);
                                stream.Close();
                                return message;
                            }
                        }
                        catch (Exception e)
                        {
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                message.Text = "Error";
                            });
                            return "ER";
                        }
                    }
                    stream.Close();
                }

            }
            Device.BeginInvokeOnMainThread(() =>
            {
                message.Text = "Error";
            });
            return "ER";
        }

        private void Quit_Clicked(object sender, EventArgs e)
        {
            if (Device.RuntimePlatform == Device.Android)
            {
                sendData("8:" + ((float)turnFactor.Value).ToString());
                Thread.Sleep(10);
                sendData("7:" + ((float)turn_drift_threshold.Value).ToString());
                Thread.Sleep(10);
                sendData("6:" + ((int)drift_threshold.Value).ToString());
                Thread.Sleep(10);
                sendData("5:" + ((int)turnBoost.Value).ToString());
                Thread.Sleep(10);
                sendData("4:" + ((float)Z_threshold.Value).ToString());
                Thread.Sleep(10);
                sendData("3:" + ((int)rightBackwardSpeed.Value).ToString());
                Thread.Sleep(10);
                sendData("2:" + (180 - (int)rightForwardSpeed.Value).ToString());
                Thread.Sleep(10);
                sendData("1:" + (180 - (int)leftBackwardSpeed.Value).ToString());
                Thread.Sleep(10);
                sendData("0:" + ((int)leftForwardSpeed.Value).ToString());
                Thread.Sleep(10);
            }
            else
            {
                Thread.Sleep(250);
            }
            string result = sendData("13:0");
            tcs.SetResult(true);
            Device.BeginInvokeOnMainThread(async () =>
            {
                await Navigation.PopModalAsync();
            });
        }

        private void SpeedTest_Clicked(object sender, EventArgs e)
        {
            if (Device.RuntimePlatform == Device.Android)

            {
                sendData("3:" + ((int)rightBackwardSpeed.Value).ToString());
                Thread.Sleep(10);
                sendData("2:" + (180 - (int)rightForwardSpeed.Value).ToString());
                Thread.Sleep(10);
                sendData("1:" + (180 - (int)leftBackwardSpeed.Value).ToString());
                Thread.Sleep(10);
                sendData("0:" + ((int)leftForwardSpeed.Value).ToString());
                Thread.Sleep(10);
            }
            sendData("11:0");
        }

        private void NavTest_Clicked(object sender, EventArgs e)
        {
            if (Device.RuntimePlatform == Device.Android)
            {
                sendData("8:" + ((float)turnFactor.Value).ToString());
                Thread.Sleep(10);
                sendData("7:" + ((float)turn_drift_threshold.Value).ToString());
                Thread.Sleep(10);
                sendData("6:" + ((int)drift_threshold.Value).ToString());
                Thread.Sleep(10);
                sendData("5:" + ((int)turnBoost.Value).ToString());
                Thread.Sleep(10);
                sendData("4:" + ((float)Z_threshold.Value).ToString());
                Thread.Sleep(10);
            }
            sendData("12:0");
        }

        private void UpdateName_Clicked(object sender, EventArgs e)
        {
            sendData("9:" + Uri.EscapeDataString(robotName.Text));
        }

        private void Turn_drift_threshold_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            double value = Math.Round(e.NewValue / 0.1) * 0.1;
            ((Slider)sender).Value = value;
            turn_drift_thresholdLabel.Text = "Turn Drift Threshold: " + value.ToString("N1");
        }

        private void TurnFactor_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            double value = Math.Round(e.NewValue / 0.01) * 0.01;
            ((Slider)sender).Value = value;
            turnFactorLabel.Text = "Turn Boost: " + value.ToString("N2");
        }

        private void Drift_threshold_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            int value = (int)Math.Round(e.NewValue);
            ((Slider)sender).Value = value;
            drift_thresholdLabel.Text = "Drift Threshold: " + value.ToString();
         }

        private void TurnBoost_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            int value = (int)Math.Round(e.NewValue);
            ((Slider)sender).Value = value;
            turnBoostLabel.Text = "Turn Boost: " + value.ToString();
        }


        private void Z_threshold_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            int value = (int)Math.Round(e.NewValue);
            ((Slider)sender).Value = value;
            Z_thresholdLabel.Text = "Z Threshold: " + value.ToString();
        }

        private void RightBackwardSpeed_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            int value = (int)Math.Round(e.NewValue);
            ((Slider)sender).Value = value;
            rightBackwardSpeedLabel.Text = "Right Backward Speed: " + value.ToString();
        }

        private void RightForwardSpeed_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            int value = (int)Math.Round(e.NewValue);
            ((Slider)sender).Value = value;
            rightForwardSpeedLabel.Text = "Right Forward Speed: " + value.ToString();
        }

        private void LeftForwardSpeed_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            int value = (int)Math.Round(e.NewValue);
            ((Slider)sender).Value = value;
             leftForwardSpeedLabel.Text = "Left Forward Speed: " + value.ToString();
        }

        private void LeftBackwardSpeed_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            int value = (int)Math.Round(e.NewValue);
            ((Slider)sender).Value = value;
            leftBackwardSpeedLabel.Text = "Left Backward Speed: " + value.ToString();
        }

        private void TurnFactor_Unfocused(object sender, FocusEventArgs e)
        {
            sendData("8:" + ((float)((Slider)sender).Value).ToString());
        }

        private void Turn_drift_threshold_Unfocused(object sender, FocusEventArgs e)
        {
            sendData("7:" + ((float)((Slider)sender).Value).ToString());
        }

        private void Drift_threshold_Unfocused(object sender, FocusEventArgs e)
        {
            sendData("6:" + ((int)((Slider)sender).Value).ToString());
        }

        private void TurnBoost_Unfocused(object sender, FocusEventArgs e)
        {
            sendData("5:" + ((int)((Slider)sender).Value).ToString());
        }

        private void Z_threshold_Unfocused(object sender, FocusEventArgs e)
        {
            sendData("4:" + ((float)((Slider)sender).Value).ToString());
        }

        private void RightBackwardSpeed_Unfocused(object sender, FocusEventArgs e)
        {
            sendData("3:" + ((int)((Slider)sender).Value).ToString());
        }

        private void RightForwardSpeed_Unfocused(object sender, FocusEventArgs e)
        {
            sendData("2:" + (180 - (int)((Slider)sender).Value).ToString());
        }

        private void LeftBackwardSpeed_Unfocused(object sender, FocusEventArgs e)
        {
            sendData("1:" + (180 - (int)((Slider)sender).Value).ToString());
        }

        private void LeftForwardSpeed_Unfocused(object sender, FocusEventArgs e)
        {
            sendData("0:" + ((int)((Slider)sender).Value).ToString());
        }

    }
}