using Akka.Actor;

namespace Pop.Net
{
    internal class TransactionHandler : PopStateHandler
    {
        public TransactionHandler(IActorRef self) : base(self)
        {            
        }
    }
}