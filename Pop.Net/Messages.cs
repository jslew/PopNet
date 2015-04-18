namespace Pop.Net
{
    public static class Messages
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

        public class UserAuthenticated
        {
            public string User { get; set; }
        }
    }
}