////////////////////////////////////////////////////////////////////////////
// Window1.xaml.cs - WPF User Interface for WCF Communicator              //
//  ver 2.3                                                               //
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
 * Maintenance History:
 * ====================
 * ver 2.1 : 15 Nov 2014
 *removed send message button and added list boxes for dependency and relationship disply
 * ver 2.2 :19 Nov 2014
 * Few changes done to GUI to suit the project requirements 
 * 
 * 
 * PUBLIC Interfaces:
 * public class Projectinfo : to store the address and directories coming from the server
 * which can be used to send back to them
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Xml.XPath;
using System.Collections;

namespace WPF_GUI
{
    public partial class Window1 : Window
    {
        WCF_Peer_Comm.Receiver recvr;
        WCF_Peer_Comm.Sender sndr;
        WCF_Peer_Comm.SvcMsg rcvdMsg = null;
        //int MaxMsgCount = 100;
        List<Projectinfo> lstprojects = new List<Projectinfo>();

        Thread rcvThrd = null;
        delegate void NewMessage(WCF_Peer_Comm.SvcMsg msg);
        event NewMessage OnNewMessage;

        //----< receive thread processing >------------------------------
        void ThreadProc()
        {
            try
            {
                while (true)
                {
                    // get message out of receive queue - will block if queue is empty
                    rcvdMsg = recvr.GetMessage();
                    // call window functions on UI thread
                    this.Dispatcher.BeginInvoke(
                      System.Windows.Threading.DispatcherPriority.Normal,
                      OnNewMessage,
                      rcvdMsg);
                }
            }
            catch (Exception e)
            {

                Window temp = new Window();
                StringBuilder msgs = new StringBuilder(e.Message);
                temp.Content = msgs.ToString();
                temp.Height = 100;
                temp.Width = 500;
                temp.Show();
            }
        }

        //----< called by UI thread when dispatched from rcvThrd >-------

        void OnNewMessageHandler(WCF_Peer_Comm.SvcMsg msg)
        {
            try
            {
                if (rcvdMsg.cmd == "takeRelationInfo")
                {
                    XmlDocument xmlDoc = new XmlDocument(); //creating a new xml file to save the output
                    xmlDoc.LoadXml(rcvdMsg.body);           //loading from the received message
                    string paths = System.IO.Directory.GetCurrentDirectory();
                    string full = "../../";
                    xmlDoc.Save(full + "/final_relationship.xml");
                    packageAndSendResults();                //sending result to listbox
                }
                else if (rcvdMsg.cmd == "returnProjects")
                {
                    listBox3.Items.Clear();
                    foreach (String item in msg.directories)
                    {
                        Projectinfo objPr = new Projectinfo();
                        objPr.Name = item;
                        objPr.Server = rcvdMsg.src;
                        lstprojects.Add(objPr);
                        listBox3.Items.Insert(0, item);
                    }
                }
                else if (rcvdMsg.cmd == "Timeout")
                {
                        WCF_Peer_Comm.SvcMsg an_msg = new WCF_Peer_Comm.SvcMsg();
                        string remoteAddress = "http://localhost";// RemoteAddressTextBox.Text;
                        string LocalPort = LocalPortTextBox.Text;
                        string endpoint = remoteAddress + ":" + LocalPort + "/ICommunicator";
                        an_msg.client_src = endpoint;
                        an_msg.dir_anal = msg.dir_anal;
                        an_msg.cmd = "doPackageAnalysis_in_this_server";
                        sndr = new WCF_Peer_Comm.Sender(rcvdMsg.src);
                        sndr.PostMessage(an_msg);
                 }
            }
            catch (Exception e)
            {

                Window temp = new Window();
                StringBuilder msgs = new StringBuilder(e.Message);
                temp.Content = msgs.ToString();
                temp.Height = 100;
                temp.Width = 500;
                temp.Show();
            }
        }
        //----< subscribe to new message events >------------------------

        public Window1()
        {
            try
            {
                InitializeComponent();
                Title = "Peer Comm";
                OnNewMessage += new NewMessage(OnNewMessageHandler);
                // ConnectButton.IsEnabled = false;
                listBox2.Items.Add(
            new { itemFileName = "Relation", itemMethodName = "type1", itemStart = "type1_Namespace", itemSize = "type2", itemComplexity = "Type2Namespace" });
            }

            catch (Exception e)
            {

                Window temp = new Window();
                StringBuilder msgs = new StringBuilder(e.Message);
                temp.Content = msgs.ToString();
                temp.Height = 100;
                temp.Width = 500;
                temp.Show();
            }
        }
        //----< start listener >-----------------------------------------

        private void ListenButton_Click(object sender, RoutedEventArgs e)
        {
            WCF_Peer_Comm.SvcMsg msg = new WCF_Peer_Comm.SvcMsg();
            string localPort = LocalPortTextBox.Text;
            string endpoint = "http://localhost:" + localPort + "/ICommunicator";
            msg.src = endpoint;
            msg.client_src = endpoint;
            try
            {
                recvr = new WCF_Peer_Comm.Receiver();
                recvr.CreateRecvChannel(endpoint);
                // create receive thread which calls rcvBlockingQ.deQ() (see ThreadProc above)
                rcvThrd = new Thread(new ThreadStart(this.ThreadProc));
                rcvThrd.IsBackground = true;
                rcvThrd.Start();
                //  ConnectButton.IsEnabled = true;
                ListenButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                Window temp = new Window();
                StringBuilder msgs = new StringBuilder(ex.Message);
                msgs.Append("\nport = ");
                msgs.Append(localPort.ToString());
                temp.Content = msgs.ToString();
                temp.Height = 100;
                temp.Width = 500;
                temp.Show();
            }
        }
        //----< connect to remote listener >-----------------------------

        private string Connect(string port)
        {
            try
            {
                string remoteAddress = "http://localhost";                            //RemoteAddressTextBox.Text;
                // string remotePort = RemotePortTextBox.Text;
                string endpoint = remoteAddress + ":" + port + "/ICommunicator";

                sndr = new WCF_Peer_Comm.Sender(endpoint);
                return endpoint;
            }
            catch (Exception e)
            {

                Window temp = new Window();
                StringBuilder msgs = new StringBuilder(e.Message);
                temp.Content = msgs.ToString();
                temp.Height = 100;
                temp.Width = 500;
                temp.Show();
                return null;
            }
        }
        //----< send message to connected listener >---------------------


        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            WCF_Peer_Comm.SvcMsg msg = new WCF_Peer_Comm.SvcMsg();
            msg.body = "quit";
            sndr.PostMessage(msg);
            sndr.Close();
            recvr.Close();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            XDocument doc = XDocument.Load("..\\..\\ServersAndPorts.xml");
            var q = from x in doc.Elements("LectureNote").Elements("server") select x.Value;
            List<string> items = q.ToList();
            listBox0.ItemsSource = items;
        }
        private void listBox0_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                XmlElement item = (XmlElement)listBox0.SelectedItem;
                string temporary = item.InnerText;
                string port_number = item.LastChild.InnerXml;            //Child.Value;
                string dest = Connect(port_number);

                WCF_Peer_Comm.SvcMsg msg = new WCF_Peer_Comm.SvcMsg();
                string remoteAddress = "http://localhost";// RemoteAddressTextBox.Text;
                string LocalPort = LocalPortTextBox.Text;
                string endpoint = remoteAddress + ":" + LocalPort + "/ICommunicator";
                msg.client_src = endpoint;
                msg.body = temporary;
                msg.cmd = "getProjects";
                msg.dest = dest;
                msg.src = msg.client_src;
                //Messages 
                sndr.PostMessage(msg);
            }
            catch (Exception ex)
            {
                Window temp = new Window();
                temp.Content = ex.Message;
                temp.Height = 100;
                temp.Width = 500;
            }
        }

        private void listBox3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {


            string item1 = (string)listBox3.SelectedItem;
            try
            {
                string ports = "";
                WCF_Peer_Comm.SvcMsg msg = new WCF_Peer_Comm.SvcMsg();
                string remoteAddress = "http://localhost";// RemoteAddressTextBox.Text;
                string LocalPort = LocalPortTextBox.Text;
                string endpoint = remoteAddress + ":" + LocalPort + "/ICommunicator";
                msg.client_src = endpoint;
                msg.dir_anal = item1;
                msg.src = msg.client_src;

                foreach (Projectinfo pr in lstprojects)
                {
                    if (pr.Name == item1)
                    {
                        ports = pr.Server;
                    }
                }
                msg.cmd = "doPackageAnalysis";
                //Messages 
                //string endpointAtServer = remoteAddress + ":" + ports + "/ICommunicator";

                sndr = new WCF_Peer_Comm.Sender(ports);
                msg.dest = ports;

                sndr.PostMessage(msg);
            }
            catch (Exception ex)
            {
                Window temp = new Window();
                temp.Content = ex.Message;
                temp.Height = 100;
                temp.Width = 500;
            }

        }

        private void listBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void listBox2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void listBox0_Copy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        class AnalDataItem
        {
            public string itemFileName { get; set; }
            public string itemMethodName { get; set; }
            public string itemStart { get; set; }
            public string itemSize { get; set; }
            public string itemComplexity { get; set; }
        }

        void addAnalItem(AnalDataItem ad)
        {
            listBox4.Items.Add(ad);
        }

        private void packageAndSendResults()
        {
            try
            {
                listBox4.Items.Clear();
                string k = "../../final_relationship.xml";
                XDocument doc = XDocument.Load(k);
                var m = from x in doc.Elements("Relationships").Elements("TypeDependency").Descendants() select x;
                AnalDataItem ai = new AnalDataItem();
                int count = 0;
                foreach (var elem in m)
                {
                    if (count == 5)
                    {
                        count = 0;
                        ai = new AnalDataItem();
                    }
                    if (elem.Name == "Relation")
                        ai.itemFileName = elem.Value;
                    if (elem.Name == "Type1")
                        ai.itemMethodName = elem.Value;
                    if (elem.Name == "Type1Namespace")
                        ai.itemStart = elem.Value;
                    if (elem.Name == "Type2")
                        ai.itemSize = elem.Value;
                    if (elem.Name == "Type2Namespace")
                        ai.itemComplexity = elem.Value;
                    count++;
                    if (count == 5)
                    {
                        Action actItem = () => addAnalItem(ai);
                        Dispatcher.Invoke(actItem);
                    }
                }
            }
            catch (Exception ex)
            {
                Window temp = new Window();
                temp.Content = ex.Message;
                temp.Height = 100;
                temp.Width = 500;
            }
        }
        public class Projectinfo
        {

            public string Name { get; set; }
            public string Server { get; set; }

        }
    }
}

