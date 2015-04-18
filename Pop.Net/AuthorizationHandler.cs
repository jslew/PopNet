using System;
using Akka.Actor;

namespace Pop.Net
{
    internal class AuthorizationHandler : PopStateHandler
    {        
        private string _userName;

        public AuthorizationHandler(IActorRef connectionHandler) : base(connectionHandler)
        {                        
            Become(ExpectUserCommand);                         
        }

        private void ExpectUserCommand()
        {
            Receive(ReceiveUser, IsCommandPredicate("USER"));
            Receive<Messages.ReceiveLine>(msg => ReceiveUnknown(msg));
        }        

        public void ReceiveUser(Messages.ReceiveLine msg)
        {
            var user = msg.GetArgs();
            if (user == "User1")
            {
                SendOkResponse("found mailbox for {0}.", user);
                _userName = user;
                Become(ExpectPassCommand);
            }
            else
            {
                SendErrResponse("no such user: {0}.", user);
            }
        }

        public void ReceiveUnknown(Messages.ReceiveLine msg)
        {
            SendErrResponse("Unknown command: {0}", msg.Line);
        }

        private void ExpectPassCommand()
        {
            Receive(ReceivePass, IsCommandPredicate("PASS"));
            ExpectUserCommand();            
        }

        private void ReceivePass(Messages.ReceiveLine msg)
        {
            if (msg.GetArgs() == "P@ssword1")
            {
                SendOkResponse("user {0} is authenticated.", _userName);
                ConnectionHandler.Tell(new Messages.UserAuthenticated {User = _userName});                
            }
            else
            {
                SendErrResponse("invalid password.");
            }
        }

        private Predicate<Messages.ReceiveLine> IsCommandPredicate(string command)
        {
            return l => l.GetCommand() == command;
        }       
    }

    public static class ReceiveLineExtensions
    {
        public static string GetCommand(this Messages.ReceiveLine line)
        {
            return line.Line.Split(' ')[0];
        }

        public static string GetArgs(this Messages.ReceiveLine line)
        {
            var idx = line.Line.IndexOf(' ');
            if (idx == -1)
                return "";
            return line.Line.Substring(idx + 1).Trim();
        }
    }
}