using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Akka.Actor;

namespace Pop.Net.Actors
{
    internal class ConnectionManager : ReceiveActor
    {
        private readonly CancellationTokenSource _ctx;
        private TcpListener _tcpListener;
        private CancellationTokenSource _cts;
        private int _connectionNum = 0;

        public ConnectionManager(CancellationTokenSource ctx)
        {
            _ctx = ctx;
            Receive<string>(msg => msg == "start", _ => Listen());
            Receive<TcpClient>(client => HandleNewClient(client));
            Receive<Msg.ConnectionClosed>(msg => HandleConnectionClosed(msg));
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
            var handlerProps = DependencyResolver.Instance.Create<ConnectionHandler>();
            Console.WriteLine("Client conneected...");
            Context.ActorOf(handlerProps,"ConnectionHandler" + _connectionNum++).Tell(new Msg.ConnectionOpened
            {
                Client = client
            });
            AcceptNextClient();
        }

        private void HandleConnectionClosed(Msg.ConnectionClosed msg)
        {            
            Context.Stop(Context.Sender);
        }    
    }
}