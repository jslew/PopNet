using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;

namespace Pop.Net
{
    class Program
    {
        public static async Task ReadLines(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8, false, 4096, true))
                while (true)
                {
                    var line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (line == null) break;
                    Console.WriteLine("server: " + line);
                }
        }

        public static async Task WriteLines(Stream stream)
        {
            while (true)
            {
                var line = Console.ReadLine();
                 if (line == "." || line == null)
                    break;
                Console.WriteLine("client: " + line);

                var bytes = Encoding.ASCII.GetBytes(line + "\r\n");
                await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }
        }

        static void Main(string[] args)
        {
            var sys = ActorSystem.Create("PopNet");
            var clientProps = Props.Create<ConnectionManager>();
            var listener = sys.ActorOf(clientProps);
            listener.Tell("start");
            sys.AwaitTermination();                        
        }

        private static async Task AcceptClientsAsync(TcpListener listener, CancellationToken token)
        {
            var clientCounter = 0;
            while (!token.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                await EchoAsync(client, ++clientCounter, token);
            }
            
        }

        private static async Task EchoAsync(TcpClient client, int clientCounter, CancellationToken token)
        {
            Console.WriteLine("New client ({0}) connected", clientCounter);
            using (client)
            {                
                var stream = client.GetStream();
                stream.WriteLine("+OK PopNET POP3 server ready.");
                using(var reader = new StreamReader(stream))                   
                while (!token.IsCancellationRequested)
                {
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(120));
                    var readLineTask = reader.ReadLineAsync();                    
                    var completedTask = await Task.WhenAny(timeoutTask, readLineTask).ConfigureAwait(false);
                    if (completedTask == timeoutTask)
                    {
                        var msg = Encoding.ASCII.GetBytes("Client timed out");
                        await stream.WriteAsync(msg, 0, msg.Length,token).ConfigureAwait(false);
                        break;
                    }
                    var line = readLineTask.Result;
                    if (line == ".") break;
                    stream.WriteLine(line);                    
                }

            }
            Console.WriteLine("Client ({0}) disconnected", clientCounter);

        }
    }

    public static class StreamExtensions
    {
        public static async void WriteLine(this Stream stream, string text)
        {
            using (var writer = new StreamWriter(stream, Encoding.ASCII, 4096, true))
            {
                await writer.WriteLineAsync(text).ConfigureAwait(false);
            }
        }
    }
}
