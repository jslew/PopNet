using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Akka.Actor;

namespace Pop.Net.Actors
{
    public class ConnectionHandler : ReceiveActor
    {
        private TcpClient _client;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IActorRef _connectionManager;
        private NetworkStream _stream;
        private StreamReader _reader;
        private IActorRef _stateHandler;
        private string _user;

        public ConnectionHandler(CancellationTokenSource cts)
        {
            _connectionManager = Context.Parent;
            _cancellationTokenSource = cts;            
            Receive<Msg.ConnectionOpened>(msg => HandleConnectionOpened(msg));        
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

        private void HandleReceiveLine(Msg.ReceiveLine message)
        {
            if (message.Line == null)
            {
                Console.WriteLine("Client disconnected, shutting down...");
                _connectionManager.Tell(new Msg.ConnectionClosed());
                return;
            }
            Console.WriteLine("incoming: " + message.Line);
            _stateHandler.Tell(message);
            ReadNextLine();
        }

        private void HandleConnectionOpened(Msg.ConnectionOpened msg)
        {
            _client = msg.Client;
            _stream = _client.GetStream();
            _reader = new StreamReader(_stream);           
            ReadNextLine();
            BecomeStateHandler<AuthenticationHandler>(() =>
            {
                Receive<Msg.UserAuthenticated>(x => HandleUserAuthenticated(x));
                Receive<Msg.StateHandlerInitialized>(_=>
                {
                    WriteLine("+OK PopNet server ready.");
                });
            });
        }

        private void BecomeStateHandler<TActor>(Action becomeAction = null ) where TActor : ActorBase
        {            
            StopStateHandler();
            _stateHandler = Context.ActorOf(DependencyResolver.Instance.Create<TActor>(), typeof(TActor).Name);
            Become(() =>
            {                
                Receive<Msg.ReceiveLine>(msg => HandleReceiveLine(msg));
                Receive<Msg.SendLine>(msg => HandleSendLine(msg));
                Receive<Msg.SendLines>(msg => HandleSendLines(msg));
                Receive<Msg.ClientQuit>(msg => HandleClientQuit());
                Receive<Msg.SendFile>(msg => HandleSendFile(msg));
                if (becomeAction != null)
                {
                    becomeAction();
                }
            });
            _stateHandler.Tell(new Msg.InitializeStateHandler { User = _user});
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
            _connectionManager.Tell(new Msg.ConnectionClosed());            
        }

        private void HandleUserAuthenticated(Msg.UserAuthenticated msg)
        {
            _user = msg.User;
            BecomeStateHandler<TransactionHandler>(() =>
            {
                Receive<Msg.StateHandlerInitialized>(_ =>
                {
                    WriteLine(msg.OkMessage);
                });                
            });
        }

        private void HandleSendLine(Msg.SendLine sendLine)
        {
            WriteLine(sendLine.Line);
        }
        
        private void HandleSendLines(Msg.SendLines msg)
        {
            msg.Lines.ForEach(WriteLine);
        }
        private void WriteLine(string text)
        {
            var bytes = Encoding.ASCII.GetBytes(text + "\r\n");
            Console.WriteLine("send: {0}", text);
            _stream.WriteAsync(bytes, 0, bytes.Length, _cancellationTokenSource.Token);
        }

        private void HandleSendFile(Msg.SendFile msg)
        {
            var file = File.OpenRead(msg.Path);
            file.CopyToAsync(_stream).ContinueWith(t =>
            {                
                file.Close();
            });                
        }

        private void ReadNextLine()
        {
            _reader.ReadLineAsync()
                .ContinueWith((t) => new Msg.ReceiveLine {Line = t.Result}, _cancellationTokenSource.Token)
                .PipeTo(Self);
        }       
    }
}