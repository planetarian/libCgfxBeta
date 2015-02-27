using System;

namespace ReadCgfxGui.Messages
{
    public class ErrorMessage : Message
    {
        public string Details { get; private set; }

        public Exception Exception { get; private set; }

        public ErrorMessage(string description, string details = null)
            : base("Error", description)
        {
            Details = details;
        }

        public ErrorMessage(string description, Exception ex)
            : base("Error", description)
        {
            Exception = ex;
            Details = ex.Message;

            Exception inner = ex.InnerException;
            if (inner != null)
                Details += Environment.NewLine; // extra line
            while (inner != null)
            {
                Details += Environment.NewLine + "Inner Exception: " + inner.Message;
                inner = inner.InnerException;
            }
        }
    }
}
