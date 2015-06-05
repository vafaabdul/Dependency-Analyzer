/**************************************************************************************************
 *FileName      : BlockingQueue.cs - 
 *Version       : 1.0
 *Langage       : C#, .Net Framework 4.5
 *Platform      : Dell Inspiron , Win 7, SP 3
 *Application   : Project Number 4 Demonstration, CSE681, Fall 2014
 *Author        : Dr.Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2014
 ***************************************************************************************************/
/*
 *   Module Operations
 *   -----------------
 *   This module demonstrates communication between two threads using a 
 *   blocking queue.  If the queue is empty when the reader attempts to deQ
 *   an item then the reader will block until the writing thread enQs an item.
 *   Thus waiting is efficient.
 * 
 *   NOTE:
 *   This blocking queue is implemented using a Monitor and lock, which is
 *   equivalent to using a condition variable with a lock.
 * 
 *   Public Interface
 *   ----------------
 *   BlockingQueue<string> bQ = new BlockingQueue<string>();
 *   bQ.enQ(msg);
 *   string msg = bQ.deQ();
 * 
 */
/*
 *   Build Process
 *   -------------
 *   - Required files:   BlockingQueue.cs, Program.cs
 *   - Compiler command: csc BlockingQueue.cs Program.cs
 * 
 *   Maintenance History
 *   -------------------
 *   ver 1.0 : 22 October 2014
 *     - first release
 * 
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Threading;

namespace DependencyAnalyzer
{
    public class BlockingQueue<T>
    {
        private Queue blockingQ;
        ManualResetEvent ev;

        //----< constructor >--------------------------------------------

        public BlockingQueue()
        {
            Queue Q = new Queue();
            blockingQ = Q;
            ev = new ManualResetEvent(false);
        }
        //----< enqueue a string >---------------------------------------

        public void enQ(T msg)
        {
            lock (blockingQ)
            {
                blockingQ.Enqueue(msg);
                ev.Set();
            }
        }
        //
        //----< dequeue a T >---------------------------------------
        //
        //  This looks more complicated than you might think it needs
        //  to be; however without the second count check:
        //    If a single item is in the queue and a thread
        //    moves toward the deQ but finishes its time allocation
        //    before deQ'ing another thread may get throught the locks
        //    and deQ.  Then the first thread wakes up and since its
        //    waitFlag is false, attempts to deQ the empty queue.
        //  This is the reason for the second count check.

        public T deQ()
        {
            T msg = default(T);
            while (true)
            {
                if (this.size() == 0)
                {
                    ev.Reset();
                    ev.WaitOne();
                }
                lock (blockingQ)
                {
                    if (blockingQ.Count != 0)
                    {
                        msg = (T)blockingQ.Dequeue();
                        break;
                    }
                }
            }
            return msg;
        }
        //----< return number of elements in queue >---------------------

        public int size()
        {
            int count;
            lock (blockingQ) { count = blockingQ.Count; }
            return count;
        }
        //----< purge elements from queue >------------------------------

        public void clear()
        {
            lock (blockingQ) { blockingQ.Clear(); }
        }
    }
}
