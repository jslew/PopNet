using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pop.Net
{
    internal interface IMailDrop
    {
        IEnumerable<MailMessage> Messages { get; }
        void Initialize();
        MailMessage GetMessage(int index);
    }

    internal class MailDrop : IMailDrop
    {
        public MailDrop()
        {
            DataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PopNet");
        }

        public IEnumerable<MailMessage> Messages
        {
            get
            {
                return new DirectoryInfo(DataDir).EnumerateFiles("*.eml").Select( f => new MailMessage
                {
                    Size = f.Length
                });
            }
        }

        public void Initialize()
        {
            if (!Directory.Exists(DataDir))
            {
                Directory.CreateDirectory(DataDir);
            }
        }

        public MailMessage GetMessage(int index)
        {
            return Messages.Skip(index - 1).Take(1).FirstOrDefault();
        }

        private static string DataDir { get; set;}
    }

    internal class MailMessage
    {        
        public long Size { get; set; }
    }
}