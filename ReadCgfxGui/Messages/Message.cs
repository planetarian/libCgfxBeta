using System;
using GalaSoft.MvvmLight.Messaging;

namespace ReadCgfxGui.Messages
{
    public class Message
    {
        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public DateTime Date { get; protected set; }

        public Message(string name, string description)
        {
            Date = DateTime.Now;

            Name = name;
            Description = description;
        }

        public void Send()
        {
            Messenger.Default.Send(this);
        }
    }
}
