using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Util.Internal;

namespace Pop.Net
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
            ReceiveCommand("QUIT",_ => ConnectionHandler.Tell(new Messages.ClientQuit()));
            Receive<Messages.ReceiveLine>(msg => ReceiveUnknown(msg));
        }

        public void ReceiveUnknown(Messages.ReceiveLine msg)
        {
            SendErrResponse("Unknown command: {0}", msg.Line);
        }

        protected void ReceiveCommand(string command, Action<Messages.ReceiveLine> handler)
        {
            Receive(l => l.GetCommand() == command, handler);
        }

        protected void SendOkResponse(string text, params object[] args)
        {
            ConnectionHandler.Tell(new Messages.SendLine { Line = "+OK " + String.Format(text,args)});
        }

        protected void SendMultilineResponse(string okMsg, params string[] lines)
        {            
            ConnectionHandler.Tell(new Messages.SendLines
            {
                Lines = new[] { "+OK " + okMsg }.Concat(lines).Concat(".").ToList()
            });
        }

        protected void SendErrResponse(string text, params object[] args)
        {
            ConnectionHandler.Tell(new Messages.SendLine { Line = "-ERR " + String.Format(text, args) });
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