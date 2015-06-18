///////////////////////////////////////////////////////////////////////
// eligibleCheck.cs - check the inputs' eligibility                  //
// ver 1.0                                                           //
// Language:    C#, 2008, .Net Framework 4.0                         //
// Platform:    Win XP, SP1                                          //
// Application:  Project #2, Fall 2011                               //
// Author:      Weimin Huang, whuang16@syr.edu                       //
///////////////////////////////////////////////////////////////////////
/*
 * 
 */

//#define test
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeAnalysis;
using myFileSystem;
using System.IO;

namespace myEligibleCheck
{
    class eligibleCheck
    {
        public List<string> preCreate(List<string> sourceFiles) 
        {
            string htmFile;
            List<string> htmFiles = new List<string>();
            fileSystem myFile = new fileSystem();
            foreach (string file in sourceFiles) {
                StreamReader reader = myFile.openToRead(file);
                if (reader == null) continue;
                myFile.closeReadFile(reader);
                
                htmFile =Path.GetDirectoryName(file)+@"\" + Path.GetFileNameWithoutExtension(file) + ".htm";
                if (!myFile.ifExist(htmFile))
                {
                    //Console.WriteLine("processing {0}", htmFile);
                    StreamWriter writer = myFile.openToWrite(htmFile);
                    if (writer == null)
                    {
                        Console.WriteLine("writer is null");
                        continue;
                    }
                    else
                    {
                        myFile.closeWriteFile(writer);
                        htmFiles.Add(htmFile);
                    }
                }
                else {
                    htmFile = "";
                    htmFiles.Add(htmFile);
                }
            }
            return htmFiles;
        }
        public List<string> travers(string[] args) 
        {
            List<string> files1 = TestParser.ProcessCommandline(args);
            List<string> files2 = new List<string>();
            foreach (string file in files1) {
                if (Path.GetExtension(file) == ".cs")
                    files2.Add(file);
            }
            return files2;
        }
        public List<string> coverFiles(List<string> sourceFiles, List<string> htmFiles) 
        {
            List<string> htmFiles2 = new List<string>();
            fileSystem myFile = new fileSystem();
            string tempFile;
            int count = 0;

            foreach (string htmFile in htmFiles) {
                if (htmFile != "") htmFiles2.Add(htmFile);
                else {
                    tempFile = Path.GetDirectoryName(sourceFiles[count]) + @"\" + Path.GetFileNameWithoutExtension(sourceFiles[count]) + ".htm";
                    StreamWriter writer = myFile.openToWrite(tempFile);
                    if (writer == null) continue;
                    else
                    {
                        myFile.closeWriteFile(writer);
                        htmFiles2.Add(tempFile);
                    }
                }
                count++;
            }

            return htmFiles2;
        }
        public List<string> renameFiles(List<string> sourceFiles, List<string> htmFiles) 
        {
            List<string> htmFiles2 = new List<string>();
            int count = 0;
            fileSystem myFile = new fileSystem();
            string tempFile;
            string time;

            foreach (string htmFile in htmFiles) { 
                if (htmFile != "") htmFiles2.Add(htmFile);
                else
                {
                    time = DateTime.Now.ToString("T").Replace(":","0");
                    tempFile = Path.GetDirectoryName(sourceFiles[count]) + @"\" + Path.GetFileNameWithoutExtension(sourceFiles[count])+time+ ".htm";
                    StreamWriter writer = myFile.openToWrite(tempFile);
                    if (writer == null) continue;
                    else
                    {
                        myFile.closeWriteFile(writer);
                        htmFiles2.Add(tempFile);
                    }
                }
                count++;
            }

            return htmFiles2;
        }

#if(test)
        static void Main(string[] args)
        {

            eligibleCheck checker = new eligibleCheck();
            List<string> sourceFiles = checker.travers(args);
            List<string> htmFiles;
            int scount = 0;
            int fcount = 0;
            htmFiles=checker.preCreate(sourceFiles);
            foreach (string file in htmFiles) {
                if (file != "") scount++;
                else fcount++;
            }
            Console.Write("\n success:{0},   fail:{1}", scount, fcount);
            scount = 0;
            fcount = 0;
            htmFiles = checker.renameFiles(sourceFiles,htmFiles);
            foreach (string file in htmFiles)
            {
                if (file != "") scount++;
                else fcount++;
            }
            Console.Write("\n success:{0},   fail:{1}", scount, fcount);
        }
#endif
    }
}
