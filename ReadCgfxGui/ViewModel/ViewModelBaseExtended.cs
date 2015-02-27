using System;
using System.Linq.Expressions;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using ReadCgfxGui.Messages;

namespace ReadCgfxGui.ViewModel
{
    public class ViewModelBaseExtended : ViewModelBase
    {
        protected readonly object _lock = new object();

        protected bool SetProperty<T>(Expression<Func<T>> propertyExpression, ref T var, T value)
        {
            if (Equals(var, value)) return false;
            var = value;
            RaisePropertyChanged(propertyExpression);
            return true;
        }

        protected void NotifyReady()
        {
            Messenger.Default.Send(new ReadyMessage(GetType()));
        }


        protected void LogError(string message, string details = null)
        {
            Messenger.Default.Send(new ErrorMessage(message, details));
        }

        protected void LogError(string message, Exception ex)
        {
            Messenger.Default.Send(new ErrorMessage(message, ex));
        }
    }
}
