using System;
using Akka.Actor;

namespace Pop.Net
{
    abstract class PopStateHandler : ReceiveActor
    {
        protected readonly IActorRef ConnectionHandler;

        protected PopStateHandler(IActorRef connectionHandler)
        {
            ConnectionHandler = connectionHandler;
        }

        protected void SendOkResponse(string text, params object[] args)
        {
            ConnectionHandler.Tell(new Messages.SendLine { Line = "+OK " + String.Format(text,args)});
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