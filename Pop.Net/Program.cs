using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using SimpleInjector;

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
            var container = new Container();         
            new PopNetPackage().RegisterServices(container);
            var sys = ActorSystem.Create("PopNet");
            DependencyResolver.Instance = new SimpleInjectorDependencyResolver(container, sys);
            container.GetInstance<IMailDrop>().Initialize();
            var listener = sys.ActorOf(DependencyResolver.Instance.Create<ConnectionManager>(), "ConnectionManager");
            listener.Tell("start");
            sys.AwaitTermination();                        
        }
    }
}
