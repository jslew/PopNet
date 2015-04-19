using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Akka.Actor;

namespace Pop.Net
{
    public class ConnectionHandler : ReceiveActor
    {
        private TcpClient _client;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IActorRef _connectionManager;
        private NetworkStream _stream;
        private StreamReader _reader;
        private IActorRef _stateHandler;

        public ConnectionHandler(CancellationTokenSource cts)
        {
            _connectionManager = Context.Parent;
            _cancellationTokenSource = cts;            
            Receive<Messages.ConnectionOpened>(msg => HandleConnectionOpened(msg));        
        }

        protected override void PostStop()        
        {        
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
            if (_client != null)
            {
                _client.Close();
                _client = null;
            }
            base.PostStop();
        }

        private void HandleReceiveLine(Messages.ReceiveLine message)
        {
            if (message.Line == null)
            {
                Console.WriteLine("Client disconnected, shutting down...");
                _connectionManager.Tell(new Messages.ConnectionClosed());
                return;
            }
            Console.WriteLine("incoming: " + message.Line);
            _stateHandler.Tell(message);
            ReadNextLine();
        }

        private void HandleConnectionOpened(Messages.ConnectionOpened msg)
        {
            _client = msg.Client;
            _stream = _client.GetStream();
            _reader = new StreamReader(_stream);
            Self.Tell(new Messages.SendLine
            {
                Line = "+OK PopNet server ready."
            });
            ReadNextLine();
            BecomeStateHandler<AuthorizationHandler>(() =>
            {
                Receive<Messages.UserAuthenticated>(x => HandleUserAuthenticated(x));
            });
        }

        private void BecomeStateHandler<TActor>(Action becomeAction = null ) where TActor : ActorBase
        {            
            StopStateHandler();
            _stateHandler = Context.ActorOf(DependencyResolver.Instance.Create<TActor>(), typeof(TActor).Name);
            Become(() =>
            {                
                Receive<Messages.ReceiveLine>(l => HandleReceiveLine(l));
                Receive<Messages.SendLine>(l => HandleSendLine(l));
                Receive<Messages.SendLines>(l => HandleSendLines(l));
                Receive<Messages.ClientQuit>(l => HandleClientQuit());
                if (becomeAction != null)
                {
                    becomeAction();
                }
            });
        }

        private void StopStateHandler()
        {
            if (_stateHandler != null)
            {
                Context.Stop(_stateHandler);
            }
        }

        private void HandleClientQuit()
        {
            Console.WriteLine("QUIT received, shutting down connection...");            
            _connectionManager.Tell(new Messages.ConnectionClosed());            
        }

        private void HandleUserAuthenticated(Messages.UserAuthenticated msg)
        {
            BecomeStateHandler<TransactionHandler>();
        }

        private void HandleSendLine(Messages.SendLine sendLine)
        {
            WriteLine(sendLine.Line);
        }
        
        private void HandleSendLines(Messages.SendLines msg)
        {
            msg.Lines.ForEach(WriteLine);
        }
        private void WriteLine(string text)
        {
            var bytes = Encoding.ASCII.GetBytes(text + "\r\n");
            Console.WriteLine("send: {0}", text);
            _stream.WriteAsync(bytes, 0, bytes.Length, _cancellationTokenSource.Token);
        }

        private void ReadNextLine()
        {
            _reader.ReadLineAsync()
                .ContinueWith((t) => new Messages.ReceiveLine {Line = t.Result}, _cancellationTokenSource.Token)
                .PipeTo(Self);
        }       
    }
}