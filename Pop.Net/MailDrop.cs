using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Pop.Net
{
    internal interface IMailDrop
    {
        IEnumerable<MailMessage> Messages { get; }
        void Initialize(string user);
        MailMessage GetMessage(int index, bool includeDelted = false);        
        int UndeleteAll();
        void Commit();
    }

    internal class MailDrop : IMailDrop
    {
        private IEnumerable<MailMessage> _messages;

        public MailDrop()
        {
            DataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PopNet");
        }

        public void Initialize(string user)
        {
            if (!Directory.Exists(DataDir))
            {
                Directory.CreateDirectory(DataDir);
            }
            _messages = new DirectoryInfo(DataDir).EnumerateFiles("*.eml").OrderBy(x => x.LastWriteTime).Select((f, i) => new MailMessage
            {
                Number = i + 1,
                Date = f.LastWriteTimeUtc,
                Size = f.Length,
                Path = f.FullName,
                IsDeleted = false,
            }).ToArray();
        }

        public IEnumerable<MailMessage> Messages
        {
            get { return _messages.Where( x => !x.IsDeleted); }
        }
        
        public int UndeleteAll()
        {
            int count = 0;
            foreach (var msg in _messages.Where(m => m.IsDeleted))
            {
                ++count;
                msg.IsDeleted = false;
            }
            return count;
        }

        public void Commit()
        {           
            foreach (var msg in _messages.Where(m => m.IsDeleted))
            {
                File.Delete(msg.Path);
            }
        }


        public MailMessage GetMessage(int index, bool includeDeleted = false)
        {
            var messages = includeDeleted ? _messages : Messages;
            return messages.Skip(index - 1).Take(1).FirstOrDefault();
        }

        private static string DataDir { get; set;}
    }

    public class MailMessage
    {        
        public long Size { get; set; }
        public DateTime Date { get; set; }
        public int Number { get; set; }
        public string Path { get; set; }
        public bool IsDeleted { get; set; }
    }
}