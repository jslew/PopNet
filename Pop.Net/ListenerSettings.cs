using System;
using System.Net;

namespace Pop.Net
{
    internal class ListenerSettings
    {
        public ListenerSettings()
        {
            IpAddress = IPAddress.Any;
            Port = 3857;
            ListenLength = 500;
            ReceiveTimeout = SendTimeout = TimeSpan.FromSeconds(30);
        }
        public IPAddress IpAddress { get; set; }
        public int Port { get; set; }
        public int ListenLength { get; set; }
        public TimeSpan ReceiveTimeout { get; set; }
        public TimeSpan SendTimeout { get; set; }            
    }
}