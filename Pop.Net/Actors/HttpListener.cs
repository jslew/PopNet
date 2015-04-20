using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Util;

namespace Pop.Net.Actors
{
    public class HttpListener
    {
        private System.Net.HttpListener _httpListener;

        private CancellationTokenSource _cts;
        ConcurrentSet<Task> _listeners = new ConcurrentSet<Task>();
      
        public async Task Start()
        {
            _cts = new CancellationTokenSource();
            _httpListener = new System.Net.HttpListener();            
            _httpListener.Prefixes.Add("http://localhost:8181/incoming/");
            _httpListener.Start();
            Console.WriteLine("Listening at {0}", _httpListener.Prefixes.First());
            while (!_cts.IsCancellationRequested)
            {
               var context = await _httpListener.GetContextAsync();
                var handlerTask = HandleNewConnection(context);
                _listeners.TryAdd(handlerTask);
#pragma warning disable 4014
                handlerTask.ContinueWith(t  =>_listeners.TryRemove(handlerTask));
#pragma warning restore 4014
            }
        }

        public void Stop()
        {
            Console.WriteLine("Stopping HTTP listener, waiting for {0} in-flight requests to complete...", _listeners.Count);
            _cts.Cancel();            
            Task.WaitAll(_listeners.ToArray(),TimeSpan.FromSeconds(30));
            Console.WriteLine("HTTP listener stopped.");
        }

     

        private async Task HandleNewConnection(HttpListenerContext context)
        {
            ExceptionDispatchInfo exceptionDispatchInfo;
            try
            {
                Console.WriteLine("Writing file received via http...");
                using (var file = File.OpenWrite("foo.txt"))
                {
                    await context.Request.InputStream.CopyToAsync(file);
                }
                await WriteResponse(context,"file saved " + DateTime.UtcNow);
                context.Response.StatusCode = 200;
                context.Response.StatusDescription = "OK";               
                context.Response.Close();
                Console.WriteLine("File written.");
                return;
            }
            catch (Exception ex)
            {
                exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);               
            }
            Console.Error.WriteLine(exceptionDispatchInfo.SourceException);
            context.Response.StatusCode = 500;            
            context.Response.StatusDescription = "Upload fail";
            await WriteResponse(context, "FAIL!");
            context.Response.Close();
            exceptionDispatchInfo.Throw();            
        }

        private static async Task WriteResponse(HttpListenerContext context, string message)
        {
            using (var writer = new StreamWriter(context.Response.OutputStream, Encoding.UTF8, 4096, true))
            {
                await writer.WriteAsync(message);
            }
        }
    }
}
