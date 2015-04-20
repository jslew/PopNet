using System;
using Akka.Actor;

namespace Pop.Net.Actors
{
    internal class AuthenticationHandler : PopStateHandler
    {        
        private string _userName;

        public AuthenticationHandler() : base(Context.Parent)
        {                        
            Become(ExpectUserCommand);                         
        }

        
        private void ExpectUserCommand()
        {
            ReceiveCommand("USER", ReceiveUser);            
            ReceiveDefault();
        }        

        public void ReceiveUser(Msg.ReceiveLine msg)
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
        
        private void ExpectPassCommand()
        {
            Receive(ReceivePass, IsCommandPredicate("PASS"));
            ExpectUserCommand();            
        }

        private void ReceivePass(Msg.ReceiveLine msg)
        {
            if (msg.GetArgs() == "P@ssword1")
            {                
                ConnectionHandler.Tell(new Msg.UserAuthenticated
                {
                    User = _userName,
                    OkMessage = String.Format("+OK user {0} is authenticated.", _userName)
                });
            }
            else
            {
                SendErrResponse("invalid password.");
            }
        }

        private Predicate<Msg.ReceiveLine> IsCommandPredicate(string command)
        {
            return l => l.GetCommand() == command;
        }       
    }
}