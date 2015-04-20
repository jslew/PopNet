using System;
using System.Linq;
using Akka.Actor;
using Akka.Util.Internal;

namespace Pop.Net.Actors
{
    abstract class PopStateHandler : ReceiveActor
    {
        protected readonly IActorRef ConnectionHandler;

        protected PopStateHandler(IActorRef connectionHandler)
        {
            ConnectionHandler = connectionHandler;
        }

        protected void ReceiveDefault()
        {
            ReceiveCommand("QUIT", _ => OnQuit());
            Receive<Msg.ReceiveLine>(msg => ReceiveUnknown(msg));
            Receive<Msg.InitializeStateHandler>(msg => OnInitialize(msg));
        }

        
        protected virtual void OnInitialize(Msg.InitializeStateHandler msg)
        {
            ConnectionHandler.Tell(new Msg.StateHandlerInitialized());
        }

        protected virtual void OnQuit()
        {
            SendOkResponse("seeya.");
            Quit();

        }

        protected void Quit()
        {
            ConnectionHandler.Tell(new Msg.ClientQuit());
        }

        public void ReceiveUnknown(Msg.ReceiveLine msg)
        {
            SendErrResponse("Unknown command: {0}", msg.Line);
        }

        

        protected void ReceiveCommand(string command, Action<Msg.ReceiveLine> handler)
        {
            Receive(l => l.GetCommand() == command, handler);
        }

        protected void SendOkResponse(string text, params object[] args)
        {
            ConnectionHandler.Tell(new Msg.SendLine { Line = "+OK " + String.Format(text,args)});
        }

        protected void SendMultilineResponse(string okMsg, params string[] lines)
        {            
            ConnectionHandler.Tell(new Msg.SendLines
            {
                Lines = new[] { "+OK " + okMsg }.Concat(lines).Concat(".").ToList()
            });
        }

        protected void SendErrResponse(string text, params object[] args)
        {
            ConnectionHandler.Tell(new Msg.SendLine { Line = "-ERR " + String.Format(text, args) });
        }

        protected override void PreStart()
        {
            Console.WriteLine("{0} starting...", GetType().Name);
            base.PreStart();
        }

        protected override void PostStop()
        {
            Console.WriteLine("{0} stopped.", GetType().Name);
            base.PostStop();
        }
    }
    
}