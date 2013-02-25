using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaDXInject
{
    public delegate void DebugMessage(Int32 clientPID, string message);

    public static class MessageManager
    {
        // The pending event queue.
        static List<object[]> PendingEvents = new List<object[]>();
        
        // Adds a pending event to the queue.
        public static void AddPendingEvent(int type, object[] args)
        {
            object[] pendingEvent = new object[args.Length + 1];
            pendingEvent[0] = type;

            int i = 1;
            foreach (object o in args)
            {
                pendingEvent[i] = o;
                i++;
            }

            lock (PendingEvents)
            {
                PendingEvents.Add(pendingEvent);
            }
        }

        // Returns the first event in the queue, or null if the queue is empty.
        public static object[] GetPendingEvent()
        {
            lock (PendingEvents)
            {
                if (PendingEvents.Count == 0)
                    return null;

                object[] pendingEvent = PendingEvents[0];
                PendingEvents.RemoveAt(0);

                return pendingEvent;
            }
        }

    }
}
