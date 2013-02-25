using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaDXInject
{
    public class ChatInterface : MarshalByRefObject
    {
        // Provides a way to check if the connection is still alive without really doing anything.
        public void Ping()
        {
        }

        // Returns the first event in the queue, or null if the queue is empty.
        public object[] GetPendingEvent()
        {
            return MessageManager.GetPendingEvent();
        }
    }
}
