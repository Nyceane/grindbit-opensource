using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Maker.Serial;
using Microsoft.Maker.RemoteWiring;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Devices.Enumeration;
using System.Threading;
using Microsoft.Azure.Devices.Client;
using System.Text;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Grindbit
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        IStream connection;
        RemoteDevice arduino;

        bool left = false;
        bool right = false;

        private const string DeviceConnectionString = "HostName=<iothub_host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>";

        DeviceClient deviceClient;

        public MainPage()
        {
            this.InitializeComponent();
            deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Http1);
            SetupRemoteArduino();
        }

        public void SetupRemoteArduino()
        {
            //create a bluetooth connection and pass it to the RemoteDevice
            //I am using a constructor that accepts a device name or ID.
            connection = new UsbSerial("2341");
            arduino = new RemoteDevice(connection);

            //add a callback method (delegate) to be invoked when the device is ready, refer to the Events section for more info
            arduino.DeviceReady += Setup;
            arduino.DeviceConnectionFailed += Arduino_DeviceConnectionFailed; //(4)

            //always begin your IStream
            connection.begin(57600, SerialConfig.SERIAL_8N1);
        }

        private void Arduino_DeviceConnectionFailed(string message)
        {
            Debug.WriteLine(message);
        }

        //treat this function like "setup()" in an Arduino sketch.
        public void Setup()
        {
            //set digital pin 13 to OUTPUT
            arduino.pinMode(13, PinMode.OUTPUT);

            //set analog pin A0 to ANALOG INPUT
            arduino.pinMode("A0", PinMode.ANALOG);
            arduino.pinMode("A1", PinMode.ANALOG);
            loop();
        }
        
        //this async Task function will execute infinitely in the background
        private async Task loop()
        {
            while (true)
            {
                UInt16 left = arduino.analogRead("A0");
                UInt16 right = arduino.analogRead("A1");
                //100 being threshold
                if(left >= 100)
                {
                    messageLeft.Text = left.ToString();
                    grindLeft.Visibility = Visibility.Visible;
                    messageLeft.Visibility = Visibility.Visible;
                }

                if(right >= 100)
                {
                    messageRight.Text = right.ToString();
                    grindRight.Visibility = Visibility.Visible;
                    messageRight.Visibility = Visibility.Visible;
                }

                if (left >= 100 || right >= 100)
                {
                    //update the server when both reach threshold
                    SendEvent("{\"left\":" + left.ToString() + ", \"right\":" + right.ToString() + "}");
                }
                await Task.Delay(500);         //delay half a second
                grindRight.Visibility = Visibility.Collapsed;
                messageRight.Visibility = Visibility.Collapsed;
            }
        }

        async Task SendEvent(string data)
        {
            Debug.WriteLine("Device sending {0} messages to IoTHub...\n", data);
            Message eventMessage = new Message(Encoding.UTF8.GetBytes(data));
            await deviceClient.SendEventAsync(eventMessage);
        }
    }
}
