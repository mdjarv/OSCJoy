using Bespoke.Common.Osc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Data;

namespace OSCJoy
{
    public class DebugMessage
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
    }

    public partial class MainWindow : Window
    {
        //static public vJoy joystick;
        //static public vJoy.JoystickState iReport;
        static public uint id = 1;

        private static OscServer server;
        public int port = 8000;
        public IPAddress ipAddress = GetLocalIPAddress();

        private readonly BackgroundWorker worker = new BackgroundWorker();

        public static ObservableCollection<DebugMessage> debugMessages = new ObservableCollection<DebugMessage>();
        public static object _syncLock = new object();

        public static JoystickState joystickState = new JoystickState();

        public static void debug(string message)
        {
            lock(_syncLock)
            {
                debugMessages.Insert(0, new DebugMessage() { Timestamp = DateTime.Now, Message = message});
            }
        }

        public static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }

        public MainWindow()
        {
            InitializeComponent();

            // SetupJoystick();

            this.DataContext = joystickState;

            this.Closing += MainWindow_Closing;

            BindingOperations.EnableCollectionSynchronization(debugMessages, _syncLock);
            listBox.ItemsSource = debugMessages;

            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.RunWorkerAsync();
        }

        #region Joystick Setup
        /*
        private void SetupJoystick()
        {
            // Create one joystick object and a position structure.
            joystick = new vJoy();
            iReport = new vJoy.JoystickState();

            id = 1;

            if (id <= 0 || id > 16)
            {
                Console.WriteLine("Illegal device ID {0}\nExit!", id);
                return;
            }

            // Get the driver attributes (Vendor ID, Product ID, Version Number)
            if (!joystick.vJoyEnabled())
            {
                Console.WriteLine("vJoy driver not enabled: Failed Getting vJoy attributes.\n");
                return;
            }
            else
                Console.WriteLine("Vendor: {0}\nProduct :{1}\nVersion Number:{2}\n", joystick.GetvJoyManufacturerString(), joystick.GetvJoyProductString(), joystick.GetvJoySerialNumberString());

            // Get the state of the requested device
            VjdStat status = joystick.GetVJDStatus(id);
            switch (status)
            {
                case VjdStat.VJD_STAT_OWN:
                    Console.WriteLine("vJoy Device {0} is already owned by this feeder\n", id);
                    break;
                case VjdStat.VJD_STAT_FREE:
                    Console.WriteLine("vJoy Device {0} is free\n", id);
                    break;
                case VjdStat.VJD_STAT_BUSY:
                    Console.WriteLine("vJoy Device {0} is already owned by another feeder\nCannot continue\n", id);
                    return;
                case VjdStat.VJD_STAT_MISS:
                    Console.WriteLine("vJoy Device {0} is not installed or disabled\nCannot continue\n", id);
                    return;
                default:
                    Console.WriteLine("vJoy Device {0} general error\nCannot continue\n", id);
                    return;
            };

            // Check which axes are supported
            bool AxisX = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_X);
            bool AxisY = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Y);
            bool AxisZ = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Z);
            bool AxisRX = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RX);
            bool AxisRZ = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RZ);
            // Get the number of buttons and POV Hat switchessupported by this vJoy device
            int nButtons = joystick.GetVJDButtonNumber(id);
            int ContPovNumber = joystick.GetVJDContPovNumber(id);
            int DiscPovNumber = joystick.GetVJDDiscPovNumber(id);

            Console.WriteLine("\nvJoy Device {0} capabilities:\n", id);
            Console.WriteLine("Numner of buttons\t\t{0}\n", nButtons);
            Console.WriteLine("Numner of Continuous POVs\t{0}\n", ContPovNumber);
            Console.WriteLine("Numner of Descrete POVs\t\t{0}\n", DiscPovNumber);
            Console.WriteLine("Axis X\t\t{0}\n", AxisX ? "Yes" : "No");
            Console.WriteLine("Axis Y\t\t{0}\n", AxisX ? "Yes" : "No");
            Console.WriteLine("Axis Z\t\t{0}\n", AxisX ? "Yes" : "No");
            Console.WriteLine("Axis Rx\t\t{0}\n", AxisRX ? "Yes" : "No");
            Console.WriteLine("Axis Rz\t\t{0}\n", AxisRZ ? "Yes" : "No");

            // Test if DLL matches the driver
            UInt32 DllVer = 0, DrvVer = 0;
            bool match = joystick.DriverMatch(ref DllVer, ref DrvVer);
            if (match)
                Console.WriteLine("Version of Driver Matches DLL Version ({0:X})\n", DllVer);
            else
                Console.WriteLine("Version of Driver ({0:X}) does NOT match DLL Version ({1:X})\n", DrvVer, DllVer);


            // Acquire the target
            if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!joystick.AcquireVJD(id))))
            {
                Console.WriteLine("Failed to acquire vJoy device number {0}.\n", id);
                return;
            }
            else
                Console.WriteLine("Acquired: vJoy device number {0}.\n", id);
        }
        */
        #endregion

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (server != null && server.IsRunning)
            {
                debug("Application exiting, stopping server");
                server.Stop();
            }
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            debug("Background worker completed");
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            debug("Running background worker");
            debug("Starting server on " + ipAddress.ToString() + ":" + port);

            server = new OscServer(TransportType.Udp, ipAddress, port);

            server.BundleReceived += Server_BundleReceived;
            server.MessageReceived += Server_MessageReceived;
            server.PacketReceived += Server_PacketReceived;
            server.ReceiveErrored += Server_ReceiveErrored;

            server.Start();
        }

        private void Server_ReceiveErrored(object sender, Bespoke.Common.ExceptionEventArgs e)
        {
            debug("Unhandled Receive Errored");
        }

        private void Server_PacketReceived(object sender, OscPacketReceivedEventArgs e)
        {
            if (e.Packet.Data.Count == 1)
            {
                debug("Packet Received: " + e.Packet.Address + " = " + e.Packet.Data[0]);
            }
            else
            {
                debug("Packet Received: " + e.Packet.Address);
                foreach (object o in e.Packet.Data)
                {
                    debug("    " + o.ToString());
                }
            }

            switch(e.Packet.Address)
            {
                case "/1/fader1":
                    joystickState.AxisX = (float) e.Packet.Data[0];
                    
                    break;
                case "/1/fader2":
                    joystickState.AxisY = (float) e.Packet.Data[0];
                    break;
            }
            debug("Packet parsed");
        }

        private void Server_MessageReceived(object sender, OscMessageReceivedEventArgs e)
        {
            debug("Unhandled Message Received");
        }

        private void Server_BundleReceived(object sender, OscBundleReceivedEventArgs e)
        {
            debug("Unhandled Bundle Received");
        }
    }
}
