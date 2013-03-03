/*
Copyright (c) 2013 Patrik Sletmo

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaDXInject
{
    public delegate void DebugMessage(String message);

    public static class MessageManager
    {
        // The pending event queue.
        static List<object[]> PendingEvents = new List<object[]>();

        // Event called when a debug message is being sent.
        public static event DebugMessage OnDebugMessage;
        
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

        // Sends a debug message to the main program.
        public static void AddDebugMessage(String message)
        {
            if (OnDebugMessage != null)
                OnDebugMessage(message);
        }

    }
}
