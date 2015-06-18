/////////////////////////////////////////////////////////////////////////////
// Communication.svc.cs - Peer-To-Peer WCF Communicator                    //
// ver 2.1                                                                //
//  Language:     Visual C# 2013, Ultimate                                //
//  Platform:     Dell Inspiron, Microsoft Windows 8.1, Build 9200        //
//  Application:  CSE681 Pr4, Dependency Analyzer Project                 //
//  Author:       Venkata Karthikeya Jangal,                              // 
//				  Master's - Computer Engineering,                        //
//				  Syracuse University,                                    //
//				  vjangal@syr.edu                                         //
////////////////////////////////////////////////////////////////////////////
/*
 * /*
Package Operations:
===================
Provides service for the client and the server to communicate with each other
It implements interfaces defined in ICommunicator.cs.
It has blocking queue which helps to stop pulling messages when it is null.
 Sender class is present to communicate with another Peer's Communication service.

Public Interfaces:
==================
CodeAnalyzer.doCodeAnalysis();	         //does code analysis in the given files.
                                         //Finds types and thier functions. Finds function size and complexity.
                                         //Finds relationships between types which include inheritance,
                                            composition, aggregation and using.

Build Process:
==============
 
Required Files:
---------------
CodeAnalyzer.cs, Parser.cs, RulesAndActions.cs, Display.cs

Build Command:
--------------
csc /target:exe /define:TEST_CODEANALYZER CodeAnalyzer.cs Parser.cs RulesAndActions.cs Display.cs
 * Maintenance History:
 * ====================
 * ver 2.2 : 01 Nov 11
 * - Removed unintended local declaration of ServiceHost in Receiver's 
 *   CreateReceiveChannel function
 * ver 2.1 : 10 Oct 11
 * - removed [OperationContract] from GetMessage() so only local client
 *   can dequeue messages
 * - added send thread to keep clients from blocking on slow sends
 * - added retries when creating Communication channel proxy
 * - added comments to clarify what code is doing
 * ver 2.0 : 06 Nov 08
 * - added close functions that close the service and receive channel
 * ver 1.0 : 14 Jul 07
 * - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using SWTools;
using System.Windows;


namespace WCF_Peer_Comm
{
  /////////////////////////////////////////////////////////////
  // Receiver hosts Communication service used by other Peers

  public class Receiver : ICommunicator
  {
    static BlockingQueue<SvcMsg> rcvBlockingQ = null;
    ServiceHost service = null;

    public Receiver()
    {      
      if (rcvBlockingQ == null)
        rcvBlockingQ = new BlockingQueue<SvcMsg>();
    }

    public void Close()
    {
      service.Close();
    }

    //  Create ServiceHost for Communication service

    public void CreateRecvChannel(string address)
    {
      BasicHttpBinding binding = new BasicHttpBinding();
      Uri baseAddress = new Uri(address);
      service = new ServiceHost(typeof(Receiver), baseAddress);
      service.AddServiceEndpoint(typeof(ICommunicator), binding, baseAddress);
      service.Open();
    }

    // Implement service method to receive messages from other Peers

    public void PostMessage(SvcMsg msg)
    {
      rcvBlockingQ.enQ(msg);
    }

    // Implement service method to extract messages from other Peers.
    // This will often block on empty queue, so user should provide
    // read thread.

    public SvcMsg GetMessage()
    {
      return rcvBlockingQ.deQ();
    }

  }
  ///////////////////////////////////////////////////
  // client of another Peer's Communication service

  public class Sender
  {
    ICommunicator channel;
    string lastError = "";
    BlockingQueue<SvcMsg> sndBlockingQ = null;
    Thread sndThrd = null;
    int tryCount = 0, MaxCount = 10;
    //string source;
    //List<string> dir;
    // Processing for sndThrd to pull msgs out of sndBlockingQ
    // and post them to another Peer's Communication service

    void ThreadProc()
    {
      while (true)
      {
          SvcMsg msg = sndBlockingQ.deQ();

          try
          {
              channel.PostMessage(msg);   
              if (msg.body == "quit")
                  break;
          }
          catch (Exception ex)
          {
             //if exception is occured that means there is no endpoint,so sending the message to client which inturns sends to server
              string endpoint = msg.client_src;
              WCF_Peer_Comm.SvcMsg new_msg = new WCF_Peer_Comm.SvcMsg();
              new_msg.src = msg.src;
              new_msg = msg;
              new_msg.cmd = "Timeout";
              new_msg.body = "Server2Down";
              msg.dest = msg.client_src;
              WCF_Peer_Comm.Sender
                  sndr = new WCF_Peer_Comm.Sender(endpoint);
              //  SvcMsg msg = new SvcMsg();
              //msg.cmd = "Timeout";

              sndr.PostMessage(new_msg);
              //continue;

             Console.WriteLine(ex.Message);
          }
      }
    }

    // Create Communication channel proxy, sndBlockingQ, and
    // start sndThrd to send messages that client enqueues

    public Sender(string url)
    {
      sndBlockingQ = new BlockingQueue<SvcMsg>();
      while (true)
      {
        try
        {
          CreateSendChannel(url);
          tryCount = 0;
          break;
        }
        catch(Exception ex)
        {
          if (++tryCount < MaxCount)
            Thread.Sleep(100);
          else
          {
            lastError = ex.Message;
            break;
          }
        }
      }
      sndThrd = new Thread(ThreadProc);
      sndThrd.IsBackground = true;
      sndThrd.Start();
    }

    // Create proxy to another Peer's Communicator

    public void CreateSendChannel(string address)
    {
      EndpointAddress baseAddress = new EndpointAddress(address);
      BasicHttpBinding binding = new BasicHttpBinding();
      ChannelFactory<ICommunicator> factory 
        = new ChannelFactory<ICommunicator>(binding, address);
      channel = factory.CreateChannel();
    }

    // Sender posts message to another Peer's queue using
    // Communication service hosted by receipient via sndThrd

    public void PostMessage(SvcMsg msg)
    {
      sndBlockingQ.enQ(msg);
    }

    public string GetLastError()
    {
      string temp = lastError;
      lastError = "";
      return temp;
    }

    public void Close()
    {
      ChannelFactory<ICommunicator> temp = (ChannelFactory<ICommunicator>)channel;
      temp.Close();
    }
  }
}
