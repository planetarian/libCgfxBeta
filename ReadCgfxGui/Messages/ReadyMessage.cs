using System;

namespace ReadCgfxGui.Messages
{
    public class ReadyMessage : Message
    {
        public Type Subject { get; private set; }

        public ReadyMessage(Type subject) : base("Ready", subject.Name + " is now ready for operation.")
        {
            Subject = subject;
        }
    }
}
