using System.Collections.Generic;
using System.Net.Sockets;

namespace Pop.Net
{
    public static class Msg
    {
        public abstract class LineMessage
        {
            public string Line { get; set; }
        }

        public class ReceiveLine : LineMessage
        {            
        }        

        public class SendLine : LineMessage
        {
        }
        public class SendLines : LineMessage
        {
            public List<string> Lines { get; set; }
        }

        public class UserAuthenticated
        {
            public string User { get; set; }
            public string OkMessage { get; set; }
        }

        public class ClientQuit
        {
        }

        public class ConnectionClosed
        {
        }

        public class ConnectionOpened
        {
            public TcpClient Client { get; set; }
        }

        public class SendFile
        {            
            public string Path { get; set; }
        }

        public class StateHandlerInitialized
        {
        }

        public class InitializeStateHandler
        {
            public string User { get; set; }
        }
    }
}