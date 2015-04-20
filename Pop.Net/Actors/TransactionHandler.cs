using System;
using System.Linq;
using Akka.Actor;

namespace Pop.Net.Actors
{
    internal class TransactionHandler : PopStateHandler
    {
        private readonly IMailDrop _mailDrop;

        public TransactionHandler(IMailDrop mailDrop)
            : base(Context.Parent)
        {
            _mailDrop = mailDrop;
            ReceiveCommand("STAT", HandleStat);
            ReceiveCommand("LIST", HandleList);
            ReceiveCommand("RETR", HandleRetrieve);
            ReceiveCommand("DELE", HandleDelete);
            ReceiveCommand("NOOP", _ => SendOkResponse(""));
            ReceiveCommand("RSET", HandleReset);
            ReceiveDefault();
        }

        protected override void OnInitialize(Msg.InitializeStateHandler msg)
        {
            _mailDrop.Initialize(msg.User);
            base.OnInitialize(msg);
        }

        protected override void OnQuit()
        {
            try
            {
                _mailDrop.Commit();
                base.OnQuit();
            }
            catch (Exception ex)
            {
                SendErrResponse(ex.GetType().Name);
                Quit();
            }
            
        }

        private void HandleReset(Msg.ReceiveLine msg)
        {
            var undeleted = _mailDrop.UndeleteAll();
            SendOkResponse("{0} messages undeleted, {1} messages now in maildrop.", undeleted,
                _mailDrop.Messages.Count());
        }

        private void HandleDelete(Msg.ReceiveLine msg)
        {
            var mailMsg = GetMailMessage(msg);
            if (mailMsg != null)
            {
                mailMsg.IsDeleted = true;
                SendOkResponse("message {0} marked as deleted.", mailMsg.Number);
            }
        }

        private void HandleRetrieve(Msg.ReceiveLine msg)
        {
            var mailMsg = GetMailMessage(msg);
            if (mailMsg != null)
            {
                SendOkResponse("{0} octets", mailMsg.Size);
                ConnectionHandler.Tell(new Msg.SendFile
                {
                    Path = mailMsg.Path
                });
            }
        }

        private void HandleList(Msg.ReceiveLine msg)
        {
            var args = msg.GetArgs();
            if (args == "")
            {
                var messages = _mailDrop.Messages.ToArray();
                SendMultilineResponse(String.Format("{0} messages ({1} octets)", messages.Length,
                    messages.Sum(m => m.Size)),
                    messages.Select(m => String.Format("{0} {1}", m.Number, m.Size)).ToArray());
                return;
            }
            var mailMessage = GetMailMessage(msg);
            if (mailMessage != null)
            {
                SendOkResponse("{0} {1}", mailMessage.Number, mailMessage.Size);
            }
        }

        private void HandleStat(Msg.ReceiveLine msg)
        {
            var messages = _mailDrop.Messages.ToArray();
            SendOkResponse("{0} {1}", messages.Count(), messages.Sum(m => m.Size));
        }

        private MailMessage GetMailMessage(Msg.ReceiveLine msg, bool includeDeleted = false)
        {
            MailMessage message = null;
            var args = msg.GetArgs();
            int msgNum;
            if (args == "")
            {
                SendErrResponse("message number required.");
            }
            else if (!Int32.TryParse(args, out msgNum))
            {
                SendErrResponse("invalid argument: {0}", args);
            }
            else
            {
                message = _mailDrop.GetMessage(msgNum, includeDeleted);
                if (message == null)
                {
                    SendErrResponse("no such message.");
                }
            }
            return message;
        }
    }
}