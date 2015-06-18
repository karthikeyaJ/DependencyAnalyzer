////////////////////////////////////////////////////////////////////////////
//  Server.cs  - Handles the requests asked by the client and             //
//               performs task based on the client request                //
//  ver 1.0                                                               //
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
/*Package Operations:
===================

 * Provides support for client which is asking the package analysis of a project
 * It gets the type table from the other server and merges to do relationship 
 * and package analysis. Thus the result is sent back to the client , that inturn
 * gets displayed on the listbox window.
 * It makes use of the service provided by communicator.cs package
 * 
 Public Interfaces:
==================
public void Initialize_Listen(string Port);	   //Listens for the requests coming from the client
private void Connect();                        //This helps to get connected to the source destination address which is listening  
 
 
 * Compiler Command:
 * csc /target:exe /define:TEST_SERVER Program.cs
 *   
 * 
* Build Process
 * =============
 * Required Files:
 * Server_Executive.cs
 * 
 * 
 * Maintenance History
 * ===================
 * ver 1.0 : 14 nov 14
 * added conditions in the OnnewMessageHandler

 * 
 * Planned Modifications:
 * ----------------------
 * This can be extended by applying locks for multiple clients and servers
 * 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace PackageAnalysis
{
    public class Program
    {

        WCF_Peer_Comm.Receiver recvr;
        WCF_Peer_Comm.Sender sndr;
        WCF_Peer_Comm.SvcMsg rcvdMsg = null;
        //int MaxMsgCount = 100;
        static string port_num = "";
        Thread rcvThrd = null;
        delegate void NewMessage(WCF_Peer_Comm.SvcMsg msg);
        event NewMessage OnNewMessage;

        //----< receive thread processing >------------------------------

        static string getPort() { return port_num; }

        void ThreadProc()
        {
            while (true)
            {
                // get message out of receive queue - will block if queue is empty
                try
                {
                    rcvdMsg = recvr.GetMessage();
                    OnNewMessage.Invoke(rcvdMsg);
                }
                catch (Exception e)
                {
                    Console.Write(e.Message);
                }
            }
        }

        //----------------writing xml=-----------------------
        string doWriteXml(CodeAnalyzer ca)
        {

            XDocument xml = new XDocument();    //output xml file
            XElement root;                      //xml root
            xml.Declaration = new XDeclaration("1.0", "utf-8", "yes");
            XComment comment = new XComment("Types XML Output");
            xml.Add(comment);
            root = new XElement("TypeTableOutput");
            xml.Add(root);
            //writing types and functions into xml
            foreach (FileTypesInfo file in ca.fileTypes)
            {
                List<Elem> fileTypes = file.typesAndFuncs.FindAll(t => t.type != "function");
                foreach (Elem e in fileTypes)
                {
                    XElement typeinfo = new XElement("Types");
                    root.Add(typeinfo);
                    XElement fileName = new XElement("FileName");
                    fileName.Value = Path.GetFileName(file.fileName);
                    //fileTypesInfo.Add(fileName);
                    typeinfo.Add(fileName);
                    XElement fileType = new XElement("FileType");
                    fileType.Value = e.type;
                    typeinfo.Add(fileType);
                    XElement className = new XElement("ClassName");
                    className.Value = e.typeNamespace + "." + e.name;
                    typeinfo.Add(className);
                    XElement just_filename = new XElement("Just_FileName");
                    just_filename.Value = e.typeClassName;
                    typeinfo.Add(just_filename);
                    
               }
            }
            string direct = "../..";
            string paths = Path.GetFullPath(direct);
            xml.Save(paths + "/output.xml"); //saving the xml as output.xml
            string readr = paths + "/output.xml"; // storing the complete path ,so that xml can be read
            return readr;
        }

        //-------------------------------     Getting type info from other server           --------------------------------------
    void getType(WCF_Peer_Comm.SvcMsg rcvdMsg)
    {
        FileManager fm = new FileManager();
        fm.addPattern("*.cs");
        fm.setRecurse(true);    //if recursive search in sub directories is required
        string special = Path.GetFullPath("../../");
        string corrected_path = special + "Projects";
        fm.findFiles(corrected_path);
        List<string> files = fm.getFiles();
        CodeAnalyzer ca = new CodeAnalyzer(files, false);
        ca.doCodeAnalysis(); //doing code analysis
        //write to xml file
        string readr = doWriteXml(ca);
            string k="";
            using (System.IO.StreamReader rdr = new System.IO.StreamReader(readr))
            {
                k=rdr.ReadToEnd();
            }              
        WCF_Peer_Comm.SvcMsg msg = new WCF_Peer_Comm.SvcMsg();
        msg.cmd = "returnTypeinfo";
        msg.body = k;
        msg.dir_anal = rcvdMsg.dir_anal;
        msg.dest = rcvdMsg.src;
        msg.src = "http://localhost:" + getPort() + "/ICommunicator";

        msg.client_src = rcvdMsg.client_src;
        try
        {
            sndr = new WCF_Peer_Comm.Sender(rcvdMsg.src); 
            sndr.PostMessage(msg); //sending message
        }
        catch (Exception ex)
        {
            Console.WriteLine("{0} Exception caught.", ex.Message);
        }    
    }
        //--------------------------------------------------------------------------------------------------------------------------
        //------------------------------Do package analysis function-----------------------------------------
         void doPackage(WCF_Peer_Comm.SvcMsg rcvdMsg)
         {
             try
             {
                     string k = myAddress();
                    //string path = "../../Projects";
                    string remoteAddress = "http://localhost";
                    string endpoint = remoteAddress + ":" + k + "/ICommunicator";
                    sndr = new WCF_Peer_Comm.Sender(endpoint);
                    WCF_Peer_Comm.SvcMsg typemsg = new WCF_Peer_Comm.SvcMsg();
                    typemsg.cmd = "getTypeinfo";
                    typemsg.body = rcvdMsg.body;
                    typemsg.client_src = rcvdMsg.client_src;
                    typemsg.dir_anal = rcvdMsg.dir_anal;
                    //  string local = "http://localhost";// RemoteAddressTextBox.Text;
                    string LocalPort = getPort();
                    //  string endpoint = remoteAddress + ":" + LocalPort + "/ICommunicator";
                     typemsg.dest = endpoint;
                     typemsg.src = "http://localhost:" + LocalPort + "/ICommunicator";                             // endpoint;
                     sndr.PostMessage(typemsg);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
         }
        //--------------------------------------------------------------------------------------------
            void Timeout(WCF_Peer_Comm.SvcMsg rcvdMsg)
                    {
                     WCF_Peer_Comm.SvcMsg typemsg = new WCF_Peer_Comm.SvcMsg();
                    typemsg.client_src = rcvdMsg.client_src;
                    //get type info for current server
                    FileManager fm1 = new FileManager();
                    fm1.addPattern("*.cs");
                    fm1.setRecurse(true);    //if recursive search in sub directories is required
                    fm1.findFiles("../../Projects");
                    List<string> files = fm1.getFiles();
                    CodeAnalyzer ca = new CodeAnalyzer(files, false);
                    ca.doCodeAnalysis();

                    string pg = "../../Projects";
                    string p = pg + "/" + rcvdMsg.dir_anal;
                    FileManager Fexec = new FileManager();
                    Fexec.findFiles(p);
                    string[] getting_files = Fexec.getFiles().ToArray();
                    List<string> fl = new List<string>();
                    foreach (string fp in getting_files)
                    {
                        fl.Add(fp);
                    }
                    ca.findTypeRelationships(fl);
                    List<Relationship> relationships = Repository.getInstance().relationships;
                    if (relationships.Count == 0)
                    {
                        Console.WriteLine("\n  No 'Relationships' between types in the given files.");
                    }
                    //List<Relationship> inheritanceRel = relationships.FindAll(t => t.relation == "Inheritance");List<Relationship> compositionRel = relationships.FindAll(t => t.relation == "Composition");List<Relationship> aggregationRel = relationships.FindAll(t => t.relation == "Aggregation");List<Relationship> usingRel = relationships.FindAll(t => t.relation == "Using");
/*--------------*/  string readr= writeRelXml(relationships);
                    sendMessage(readr, rcvdMsg);                                   
            }
/*----*/    string writeRelXml(List<Relationship> relationships)
            {
                XDocument xml = new XDocument();    //output xml file
                XElement root;                      //xml root
                xml.Declaration = new XDeclaration("1.0", "utf-8", "yes");
                XComment comment = new XComment("Relationship XML Output");
                xml.Add(comment);
                root = new XElement("Relationships");
                xml.Add(root);
                //writing types and functions into xml
                // FileTypesInfo file in ca.fileTypes
                foreach (Relationship r in relationships)
                {
                    XElement typeinfo = new XElement("TypeDependency");
                    root.Add(typeinfo);
                    XElement relation = new XElement("Relation");
                    relation.Value = r.relation;
                    typeinfo.Add(relation);
                    XElement type1 = new XElement("Type1");
                    type1.Value = r.type1;
                    typeinfo.Add(type1);
                    XElement type1Namespace = new XElement("Type1Namespace");
                    type1Namespace.Value = r.type1Namespace;
                    typeinfo.Add(type1Namespace);
                    XElement type2 = new XElement("Type2");
                    type2.Value = r.type2;
                    typeinfo.Add(type2);
                    XElement type2Namespace = new XElement("Type2Namespace");
                    type2Namespace.Value = r.type2Namespace;
                    typeinfo.Add(type2Namespace);
                }
                string direct = "../..";
                string paths = Path.GetFullPath(direct);
                xml.Save(paths + "/relationship.xml");
                string readr = paths + "/relationship.xml";
                return readr;
            }


      void sendMessage(string r, WCF_Peer_Comm.SvcMsg rcvdMsg)
    {
        try
        {

            string k="";
            using (System.IO.StreamReader rdrs = new System.IO.StreamReader(r))
            {
                k=rdrs.ReadToEnd();
            }
      
      WCF_Peer_Comm.SvcMsg msg = new WCF_Peer_Comm.SvcMsg();
      msg.cmd = "takeRelationInfo";
      msg.body = k; 
      msg.dir_anal = rcvdMsg.dir_anal;
      msg.client_src = rcvdMsg.client_src;
      string m = getPort();
      msg.src = "http://localhost:" + m + "/ICommunicator";                             // endpoint;
          sndr = new WCF_Peer_Comm.Sender(msg.client_src);
          sndr.PostMessage(msg);                                                     //SendMsgTextBox.Text);
      }
      catch (Exception ex)
      {
          Console.WriteLine("{0} Exception caught.", ex);
      } 
    }
//--------------------------------------------------------------------------------------------------------------------------
        //----< called by UI thread when dispatched from rcvThrd >-------
        void OnNewMessageHandler(WCF_Peer_Comm.SvcMsg rcvdMsg)
        {
            try
            {
                if (rcvdMsg.cmd == "getTypeinfo")
                {
                    getType(rcvdMsg);     
                }  
 //---------------------------------------------< send message to connected listener >----------------------------------------------------
                else  if (rcvdMsg.cmd == "getProjects")
                {
                    string path = "../../Projects";
                    string[] dirs = Directory.GetDirectories(path);
                    for (int i = 0; i < dirs.Length; i++)
                    dirs[i] = Path.GetFileName(dirs[i]);
                    WCF_Peer_Comm.SvcMsg msg = new WCF_Peer_Comm.SvcMsg();
                    msg.cmd = "returnProjects";
                    msg.directories = dirs;
                    msg.src = rcvdMsg.dest;
                    msg.client_src = rcvdMsg.client_src;
                    sndr = new WCF_Peer_Comm.Sender(msg.client_src);
                    sndr.PostMessage(msg);                                                            
                }
                else if (rcvdMsg.cmd == "doPackageAnalysis")
                {
                    doPackage(rcvdMsg);                   
                }
//-------------------------------------------timeout--------------------------------------------------//
                else if (rcvdMsg.cmd == "timeOut" || rcvdMsg.cmd == "doPackageAnalysis_in_this_server")
                {
                    Timeout(rcvdMsg);
                }
//---------------------------------------<getting type table from the other server>----------------------------------------------------
                else if (rcvdMsg.cmd == "returnTypeinfo")
                {
                    returnType(rcvdMsg);
                }
            }
            catch (Exception exe)
            {
                Console.WriteLine(exe.Message);
            }
        }

        //-----------------------------------------------------------------------------------

        string myAddress() 
        {
            string return_variable_port = "";
            string p = "../../../";
            string q = p + "WPF_GUI/ServersAndPorts.xml";
            string ps = Path.GetFullPath(q);
            XmlDocument doc = new XmlDocument();
            doc.Load(ps);
            string xmlcontents = doc.InnerXml;

            XmlNodeList nodeList = doc.DocumentElement.SelectNodes("/LectureNote/server");

            foreach (XmlNode node in nodeList)
            {
               string proID = node.SelectSingleNode("port").InnerText;
               if (proID != getPort())
               {
                   return_variable_port = proID;
                   break;
               }
            }
            return return_variable_port; ;
        }



        //----------------------------------------------------------------------------
        void returnType(WCF_Peer_Comm.SvcMsg rcvdMsg)
        {
            try
            {
                WCF_Peer_Comm.SvcMsg typemsg = new WCF_Peer_Comm.SvcMsg();
                typemsg.client_src = rcvdMsg.client_src;
                typemsg.body = rcvdMsg.body;
                FileManager fm1 = new FileManager();                //get type info for current server
                fm1.addPattern("*.cs");
                fm1.setRecurse(true);                              //if recursive search in sub directories is required
                fm1.findFiles("../../Projects");
                List<string> files = fm1.getFiles();
                CodeAnalyzer ca = new CodeAnalyzer(files, false);
                ca.doCodeAnalysis();
                readXml(ca, typemsg);                          //Merging type table which we got from other servers
                string pg = "../../Projects";
                string p = pg + "/" + rcvdMsg.dir_anal;
                FileManager Fexec = new FileManager();
                Fexec.findFiles(p);
                string[] getting_files = Fexec.getFiles().ToArray();
                List<string> fl = new List<string>();
                foreach (string fp in getting_files)
                fl.Add(fp);
                ca.findTypeRelationships(fl);
                List<Relationship> relationships = Repository.getInstance().relationships;
                if (relationships.Count == 0)
                    Console.WriteLine("\n  No 'Relationships' between types in the given files.");
                string rs = writeRelXml(relationships);
                string k = "";
                using (System.IO.StreamReader rdrs = new System.IO.StreamReader(rs))
                {
                    k = rdrs.ReadToEnd();
                }
                WCF_Peer_Comm.SvcMsg msg = new WCF_Peer_Comm.SvcMsg();
                msg.cmd = "takeRelationInfo";
                msg.body = k;
                msg.dir_anal = rcvdMsg.dir_anal;
                msg.client_src = rcvdMsg.client_src;
                msg.dest = msg.client_src;
                msg.src = "http://localhost:" + getPort() + "/ICommunicator";                             // endpoint;
                sndr = new WCF_Peer_Comm.Sender(msg.dest);
                sndr.PostMessage(msg); //sending message
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex.Message);
            }    
                }
        //-------------------------------------------<Read from xml and adding type table>--------------------------
                   void readXml(CodeAnalyzer ca, WCF_Peer_Comm.SvcMsg typemsg)
                   {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(typemsg.body);
                    XmlNodeList xnList = xmlDoc.SelectNodes("/TypeTableOutput/Types");

                    //List<FileTypesInfo> f = new List<FileTypesInfo>();
                    Repository rep = Repository.getInstance();

                    foreach (XmlNode xn in xnList)
                    {
                        if (xn["FileType"].InnerText != null)
                        {
                            List<Elem> list = new List<Elem>();
                            Elem k = new Elem();
                            string filename = xn["FileName"].InnerText;
                            string type = xn["FileType"].InnerText;
                            string classname = xn["ClassName"].InnerText;
                            k.type = type;
                            k.name = classname;
                            list.Add(k);
                            ca.fileTypes.Add(new FileTypesInfo(filename, list));
                        }
                    }                          
               }
        //-----------------------------------------< subscribe to new message events >----------------------------------------------------------

        public Program()
        {
            OnNewMessage += new NewMessage(OnNewMessageHandler);
        }
        //----< start listener >-----------------------------------------

        public void Initialize_Listen(string Port)
        {
            string localPort = Port;
            port_num = Port;
            string endpoint = "http://localhost:" + localPort + "/ICommunicator";
            try
            {
                recvr = new WCF_Peer_Comm.Receiver();
                recvr.CreateRecvChannel(endpoint);

                // create receive thread which calls rcvBlockingQ.deQ() (see ThreadProc above)
                rcvThrd = new Thread(new ThreadStart(this.ThreadProc));
                rcvThrd.IsBackground = true;
                rcvThrd.Start();
                rcvThrd.Join();
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        //---------------------------------------------< connect to remote listener >----------------------------------------------------------

        private void Connect()
        {
            WCF_Peer_Comm.SvcMsg msg = new WCF_Peer_Comm.SvcMsg();
            string endpoint = msg.src;
            sndr = new WCF_Peer_Comm.Sender(endpoint);
        }



        //-------------------------------------------------TEST STUB ----------------------------------------------------------------------------
#if(SERVER)
        static void Main(string[] args)
        {
            Console.Write("\n  Starting Message Service on Server \n");
            Program pr = new Program();
            Console.Write("\n  Server is listening \n");
            pr.Initialize_Listen(getPort());
            pr.Connect();
            Console.Write("\n  Server is connecting \n");
            List<string> projects = new List<string>();
            projects.Add("Project #1");
            projects.Add("Project #2");
            WCF_Peer_Comm.SvcMsg msg = new WCF_Peer_Comm.SvcMsg();
            string k = getPort();
            string path = "http://localhost:" + k + "/MessageService";
            msg.src = "http://localhost:8080/MessageService";
            msg.cmd = "Command_ProjectList";
        }
#endif
    }
}