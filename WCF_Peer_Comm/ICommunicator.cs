/////////////////////////////////////////////////////////////////////
// ICommunicator.cs - Peer-To-Peer Communicator Service Contract   //
// ver 2.0                                                         //
//  Language:     Visual C# 2013, Ultimate                                //
//  Platform:     HP Split 13 *2 PC, Microsoft Windows 8, Build 9200      //
//  Application:  CSE681 Pr2, Code Analysis Project                       //
//  Language:     Visual C# 2013, Ultimate                                //
//  Platform:     Dell Inspiron, Microsoft Windows 8.1, Build 9200        //
//  Application:  CSE681 Pr4, Dependency Analyzer Project                 //
//  Author:       Venkata Karthikeya Jangal,                              // 
//				  Master's - Computer Engineering,                        //
//				  Syracuse University,                                    //
//				  vjangal@syr.edu                                          //
////////////////////////////////////////////////////////////////////////////
/*
 Public Interfaces:
ICommunicator : this is used for posting a message to a endpoint
 * Its implementation is done in Communicator.cs

* Maintenance History
 * ===================
 * ver 1.0 : 19 nov 14
*/
using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Collections;
using System.Collections.Generic;

namespace WCF_Peer_Comm
{
   

  [ServiceContract]
  public interface ICommunicator
  {
    [OperationContract(IsOneWay = true)]
    void PostMessage(SvcMsg msg);

    // used only locally so not exposed as service method

    SvcMsg GetMessage();
  }

  [DataContract(Namespace = "WCF_Peer_Comm")]
  public class SvcMsg
  {
      [DataMember]
      public string src;

      [DataMember]
      public string client_src;

      [DataMember]
      public string dest;
      [DataMember]
      public string body;
      [DataMember]
      public List<string> direc;

      [DataMember]
      public string cmd;
      [DataMember]
      public string[] directories;
      [DataMember]
      public string dir_anal;
  }
   
}
