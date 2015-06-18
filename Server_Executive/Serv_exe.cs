////////////////////////////////////////////////////////////////////////////
//Server_Executive.cs  - Initializes the server to start by making use    //
//                       of command line arguments                        //
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

 * It helps to start the server by taking port number as input
 * Normally port numbers used while testing this server is port:8080 and port:8081
 * Two servers can be started by giving two different port number and running twice
 * 
 * Compiler Command:
 * csc /target:exe /define:SERVER_EXECUTIVE Serv_exe.cs
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
 * no changes
 * 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace PackageAnalysis
{
    class Serv_exe
    {
        static void Main(string[] args)
        {          
            Display ds = new Display();
        //    ds.displayString("Enter two ports for 2 servers two start and change the port numbers in XML for the clients to connect");
            string firstPort = "";
            
            if (args.Length == 1)
            {
                try
                {
                    firstPort = args[0];
                }
                catch (Exception e)
                {
                    Console.WriteLine("exception message is {0}", e);
                }
            }
            else
            {
                ds.displayString("\n Enter a port for  server to start \n");
            }
            Console.WriteLine("\n  Starting Message Service on Server {0} \n",firstPort);
            Program pr = new Program();
            ds.displayString("\n  Server1 is listening \n");
            pr.Initialize_Listen(firstPort);
        }
    }    
}
