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

            this.DataContext = joystickState;

            this.Closing += MainWindow_Closing;

            BindingOperations.EnableCollectionSynchronization(debugMessages, _syncLock);
            listBox.ItemsSource = debugMessages;

            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.RunWorkerAsync();
        }

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
