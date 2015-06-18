/////////////////////////////////////////////////////////////////////////////
//  BlockingQueue.cs - demonstrate threads communicating via Queue         //
//  ver 4.0                                                                //
//  Language:     C#, VS 2003                                              //
//  Platform:     Dell Dimension 8100, Windows 2000 Pro, SP2               //
//  Application:  Demonstration for CSE681 - Software Modeling & Analysis  //
//  Author:       Jim Fawcett, CST 2-187, Syracuse University              //
//                (315) 443-3948, jfawcett@twcny.rr.com                    //
/////////////////////////////////////////////////////////////////////////////
/*
 *   Module Operations
 *   -----------------
 *   This module demonstrates communication between two threads using a 
 *   blocking queue.  If the queue is empty when the reader attempts to deQ
 *   an item then the reader will block until the writing thread enQs an item.
 *   Thus waiting is efficient.
 * 
 *   NOTE!
 *   Double-checked locking, used in deQ() devised by Douglas Schmidt, as part
 *   of the ACE communication package developed at Washington Univ, St. Louis,
 *   Missouri.
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
 *   ver 4.0 : 12 Oct 2006
 *     - simplified double checked deQ(), eliminating waitFlag.
 *   ver 3.0 : 25 Jun 2006
 *     - Converted to generic class.  That required changing deQ loop termination.
 *   ver 2.3 : 11 May 2005
 *     - made clear() and size() functions public
 *   ver 2.2 : 08 May 2004
 *     - I've fixed a subtle bug in locking of the blocking
 *       queue.  If a single item is in the queue and a thread
 *       moves toward the deQ but finishes its time allocation
 *       before deQ'ing another thread may get throught the locks
 *       and deQ.  Then the first thread wakes up and since its
 *       waitFlag is false, attempts to deQ the empty queue.
 *   ver 2.1 : 12 Nov 2003
 *     - noticed I forgot to lock during dequeue.  Now fixed.
 *   ver 2.0 : 08 Oct 2003
 *     - changed the synchronization to lock for pedagogical reasons.
 *   ver 1.0 : 20 October 2002
 *     - first release
 * 
 */

//
using System;
using System.Collections;
using System.Threading;

namespace SWTools
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
      lock(blockingQ)
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
      while(true)
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
      lock(blockingQ) { blockingQ.Clear(); }
    }
  }
}
