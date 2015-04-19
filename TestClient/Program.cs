using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestClient
{
    class Program
    {
        private static TcpClient _client;
        private static NetworkStream _stream;

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Press a key to connect...");
                Console.ReadKey();
                _client = new TcpClient("localhost", 3857);
                using (_stream = _client.GetStream())
                {
                    Task.Run(() => Automate()).Wait();
                    var task1= Task.Run(() => Read());
                    var task2 = Task.Run(() => Write());                    
                    Task.WaitAll(task1, task2);
                }
            }
            catch (Exception ex)
            {                
                Console.WriteLine(ex.Message);
            }
        }

        private static string Response()
        {
            using (var reader = new StreamReader(_stream, Encoding.ASCII, false, 4096, true))
            {
                var ret =  reader.ReadLine();
                Console.WriteLine(ret);
                return ret;
            }
        }
        private static async Task Automate()
        {
            var line =  Response();
            await EchoAndWrite("USER User1");
            line = Response();
            await EchoAndWrite("PASS P@ssword1");
            line = Response();
            await EchoAndWrite("STAT");
            line = Response();
            await EchoAndWrite("LIST");
        }

        private static Task EchoAndWrite(string line)
        {
            Console.WriteLine(line);
            return WriteLine(line);
        }

        private static async Task Write()
        {
            while (true)
            {
                var line = Console.ReadLine();
                if (line == "")
                    break;
                await WriteLine(line).ConfigureAwait(false);
            }
        }

        private static Task WriteLine(string fmt, params object[] args)
        {
            var bytes = Encoding.ASCII.GetBytes(String.Format(fmt, args) + "\r\n");            

            return _stream.WriteAsync(bytes,0,bytes.Length);
        }

        private static async void Read()
        {
            using (var reader = new StreamReader(_stream))
            {
                while (true)
                {
                    var line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (line == null)
                        break;
                    Console.WriteLine(line);
                }
            }
        }
    }
}
