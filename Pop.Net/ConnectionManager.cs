using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Akka.Actor;

namespace Pop.Net
{
    internal class ConnectionManager : ReceiveActor
    {
        private TcpListener _tcpListener;
        private CancellationTokenSource _cts;

        public ConnectionManager()
        {
            Receive<string>(msg => msg == "start", _ => Listen());
            Receive<TcpClient>(client => HandleNewClient(client));
        }

        protected override void PreStart()
        {
            base.PreStart();
            _cts = new CancellationTokenSource();
        }

        private void Listen()
        {
            _tcpListener = new TcpListener(IPAddress.Any, 3857);            
            _tcpListener.Start();
            Console.WriteLine("Listening on port {0}", 3857);
            AcceptNextClient();
        }

        private void AcceptNextClient()
        {            
            _tcpListener.AcceptTcpClientAsync().PipeTo(Context.Self);            
        }

        private void HandleNewClient(TcpClient client)
        {
            var handlerProps = Props.Create(() => new ConnectionHandler(client,_cts.Token));
            Console.WriteLine("Client conneected...");
            Context.ActorOf(handlerProps).Tell("start");
            AcceptNextClient();
        }
    
    }
}