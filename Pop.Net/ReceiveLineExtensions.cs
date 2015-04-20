namespace Pop.Net
{
    public static class ReceiveLineExtensions
    {
        public static string GetCommand(this Msg.ReceiveLine line)
        {
            return line.Line.Split(' ')[0];
        }

        public static string GetArgs(this Msg.ReceiveLine line)
        {
            var idx = line.Line.IndexOf(' ');
            if (idx == -1)
                return "";
            return line.Line.Substring(idx + 1).Trim();
        }        
    }
}