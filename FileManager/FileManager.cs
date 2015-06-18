////////////////////////////////////////////////////////////////////////////
//  FileManager.cs  - Collects files of a particular pattern              //
//						in a Directory, if needed recursively             //
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
/*
Package Operations:
===================
Provides support for collecting files of a particular set of patterns
in a Directory, if needed recursively.
Defines FileManager class.

Public Interfaces:
==================
findFiles();	   //searches files in a given directory with a given pattern.
addPattern();      //adds pattern for the file search
getFiles();        //returns the search files result in a String List
setRecurse();      //Sets recursive search value for finding files within sub-directories

Build Process:
==============
 
Required Files:
---------------
FileManager.cs

Build Command:
--------------
csc /target:exe /define:TEST_FILEMANAGER FileManager.cs

Maintanence History:
====================
ver 1.0 - 12 Sep 2014
- first release
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PackageAnalysis
{
    //----< FileManager class used for file search in a directory >----------------
    public class FileManager
    {
        private List<string> files = new List<string>();        // to hold search output
        private List<string> patterns = new List<string>();
        private bool recurse = false;


        //----< Searches files of given patterns in a given directory >-----------
        public void findFiles(string path)
        {
            if (patterns.Count == 0)
                addPattern("*.*");      //if no pattern is added, returns all files

            foreach(string pattern in patterns)
            {
                string[] newFiles = Directory.GetFiles(path, pattern);
                for (int i = 0; i < newFiles.Length; ++i)
                    newFiles[i] = Path.GetFullPath(newFiles[i]);

                files.AddRange(newFiles);       //adding to output file list
            }

            if(recurse)
            {
                string[] dirs = Directory.GetDirectories(path);
                foreach (string dir in dirs)
                    findFiles(dir);     //recursively finding files in subdirectories
            }
        }


        //----< Adds the given pattern to the PatternsList used for file search >-----------
        public void addPattern(string pattern)
        {
            patterns.Add(pattern);
        }


        //----< Gets the file result collection after search >-----------
        public List<string> getFiles()
        {
            return files;
        }


        //----< Sets recursive search value for finding files within sub-directories >-----------
        public void setRecurse(bool recurse)
        {
            this.recurse = recurse;
        }


        //----< Test Stub >--------------------------------------------------

        #if(TEST_FILEMANAGER)

        static void Main(string[] args)
        {
            Console.Write("\n  Demonstrating FileManager");
            Console.Write("\n ===========================\n");

            FileManager fm = new FileManager();
            fm.addPattern("*.cs");
            fm.setRecurse(true);    //if recursive search in sub directories is required
            fm.findFiles("../../../");            
            List<string> files = fm.getFiles();

            Console.Write("\n  Files Found:");
            Console.Write("\n --------------");
            foreach (string file in files)
                Console.Write("\n  {0}", file);
            Console.Write("\n\n");            
        }

        #endif    
    }
}
