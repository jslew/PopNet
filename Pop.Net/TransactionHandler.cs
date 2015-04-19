using System;
using System.Linq;
using Akka.Actor;

namespace Pop.Net
{
    internal class TransactionHandler : PopStateHandler
    {
        private readonly IMailDrop _mailDrop;

        public TransactionHandler(IMailDrop mailDrop) : base(Context.Parent)
        {
            _mailDrop = mailDrop;
            ReceiveCommand("STAT", HandleStat);
            ReceiveCommand("LIST", HandleList);
            ReceiveCommand("RETR", NotImplemented);
            ReceiveCommand("DELE", NotImplemented);
            ReceiveCommand("NOOP", NotImplemented);
            ReceiveCommand("RSET", NotImplemented);
            ReceiveDefault();
        }

        private void HandleList(Messages.ReceiveLine msg)
        {
            var args = msg.GetArgs();
            if (args == "")
            {
                var messages = _mailDrop.Messages.ToArray();
                SendMultilineResponse(String.Format("{0} messages ({1} octets)", messages.Length,
                    messages.Sum(m => m.Size)),
                    messages.Select((m, i) => String.Format("{0} {1}", i + 1, m.Size)).ToArray());
                return;
            }
            int argNum;
            if (!Int32.TryParse(args, out argNum))
            {
                SendErrResponse("invalid argument: {0}", args);
                return;
            }
            var message = _mailDrop.GetMessage(argNum);
            if (message == null)
            {
                SendErrResponse("no such message.");
                return;
            }
            SendOkResponse("{0} {1}", argNum, message.Size);
        }

        private void NotImplemented(Messages.ReceiveLine msg)
        {
            SendErrResponse("{0} not implemented", msg.GetCommand());            
        }

        private void HandleStat(Messages.ReceiveLine msg)
        {
            var messages = _mailDrop.Messages.ToArray();
            SendOkResponse("{0} {1}", messages.Count(), messages.Sum(m => m.Size));
        }
    }
}