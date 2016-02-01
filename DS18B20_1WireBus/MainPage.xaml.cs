using DS18B20_1WireBus.Model;
using DS18B201WireLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DS18B20_1WireBus
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private OneWire onewire;
        private string deviceId = string.Empty;
        private DispatcherTimer timer;
        private bool inprog = false;
        TemperatureModel tempData;

        public MainPage()
        {
            this.InitializeComponent();
            tempData = new TemperatureModel();
            deviceId = string.Empty;
            this.DataContext = tempData;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1200);
            timer.Tick += Timer_Tick;
            onewire = new OneWire();
        }

        private async void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            await GetFirstSerialPort();
            if(deviceId != string.Empty)
            {
                tempData.StatusText = "Reading from device: " + deviceId;
                tempData.Started = true;
                timer.Start();
            }
        }

        private async void Timer_Tick(object sender, object e)
        {
            if (!inprog)
            {
                inprog = true;
                tempData.Temperature = await onewire.getTemperature(deviceId);
                inprog = false;
            }
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            tempData.Started = false;
            onewire.shutdown();
        }

        private async Task GetFirstSerialPort()
        {
            try
            {
                string aqs = SerialDevice.GetDeviceSelector();
                var dis = await DeviceInformation.FindAllAsync(aqs);
                if(dis.Count > 0)
                {
                    var deviceInfo = dis.First();
                    deviceId = deviceInfo.Id;
                }
            }
            catch (Exception ex)
            {
                tempData.StatusText = "Unable to get serial device: " + ex.Message;
            }
        }
    }
}
