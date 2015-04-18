using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;

namespace Pop.Net
{
    public class ConnectionHandler : ReceiveActor
    {
        private TcpClient _client;
        private readonly CancellationToken _token;
        private NetworkStream _stream;
        private StreamReader _reader;
        private IActorRef _stateHandler;

        public ConnectionHandler(TcpClient client, CancellationToken token)
        {            
            _client = client;
            _token = token;
            Receive<string>(s => s == "start", s => HandleStart());        
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
                Context.Stop(Self);
                return;
            }
            Console.WriteLine("incoming: " + message.Line);
            _stateHandler.Tell(message);
            ReadNextLine();
        }

        private void HandleStart()
        {
            _stream = _client.GetStream();
            _reader = new StreamReader(_stream);
            Self.Tell(new Messages.SendLine
            {
                Line = "+OK PopNet server ready."
            });
            ReadNextLine();
            BecomeStateHandler(Props.Create(() => new AuthorizationHandler(Self)), () =>
            {
                Receive<Messages.UserAuthenticated>(msg => HandleUserAuthenticated(msg));
            });
        }

        private void BecomeStateHandler(Props props, Action becomeAction = null )
        {            
            if (_stateHandler != null)
            {
                Context.Stop(_stateHandler);
            }
            _stateHandler = Context.ActorOf(props);
            Become(() =>
            {                
                Receive<Messages.ReceiveLine>(l => HandleReceiveLine(l));
                Receive<Messages.SendLine>(l => HandleSendLine(l));
                if (becomeAction != null)
                {
                    becomeAction();
                }
            });
        }

        private void HandleUserAuthenticated(Messages.UserAuthenticated msg)
        {
            BecomeStateHandler(Props.Create(() => new TransactionHandler(Self)));
        }

        private void HandleSendLine(Messages.SendLine sendLine)
        {
            var bytes = Encoding.ASCII.GetBytes(sendLine.Line + "\r\n");
            Console.WriteLine("send: {0}", sendLine.Line);
            _stream.WriteAsync(bytes, 0, bytes.Length, _token);
        }

        private void ReadNextLine()
        {
            _reader.ReadLineAsync().ContinueWith((t) => new Messages.ReceiveLine {Line = t.Result}, _token).PipeTo(Self);
        }       
    }
}