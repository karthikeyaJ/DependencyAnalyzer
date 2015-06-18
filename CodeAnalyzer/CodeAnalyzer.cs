////////////////////////////////////////////////////////////////////////////
//  CodeAnalyzer.cs  - Analyzes code files.                               //
//                     Given a set of files. Finds all the types defined, //
//                     their functions and relationships with other types.//
//                     Finds functions size and complexity                //
//					   Type relationships include inheritance,            //
//                     composition, aggregation and using                 //
//  ver 1.0                                                               //
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
Provides support for findind types, their functions and relationships with
other types in the given file set.
Type relationships include inheritance, composition, aggregation and using.
Defines CodeAnalyzer class and FileTypesInfo class.

Public Interfaces:
==================
CodeAnalyzer.doCodeAnalysis();	         //does code analysis in the given files.
                                         //Finds types and thier functions. 
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

Maintanence History:
====================
ver 1.0 - 26 Sep 2014
- first release
 * 
 * second release nov,19 2014 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PackageAnalysis
{
    //----< FileTypesInfo class used to store file - types and functions details >----------------
    public class FileTypesInfo
    {
        //----< constructor - contains types and funtions for each file >-------
        public FileTypesInfo(string fileName, List<Elem> typesAndFuncs)
        {
            this.fileName = fileName;
            this.typesAndFuncs = typesAndFuncs;
        }
        public string fileName
        {
            get; set;
        }
        public List<Elem> typesAndFuncs
        {
            get; set;
        }
    }

    //----< CodeAnalyzer class used to do Code Analysis for the given file set >----------------
    public class CodeAnalyzer
    {
        private List<string> files;     //to store input files
        private bool isRelationshipsReq;
        CSsemi.CSemiExp semi;
        BuildCodeAnalyzer builder;
        public List<FileTypesInfo> fileTypes    //to store code analysis output
        { 
            get; set; 
        }
        //----< Constructor -- stores the files need to be analysed >----
        public CodeAnalyzer(List<string> files, bool isRelationshipsReq)
        {            
            this.files = files;
            this.isRelationshipsReq = isRelationshipsReq;
            fileTypes = new List<FileTypesInfo>();
            semi = new CSsemi.CSemiExp();
            semi.returnNewLines = false;
            builder = new BuildCodeAnalyzer(semi);
        }


        //----< Does Code Analysis for the given files. Find types and functions. Find type relationships if required >----
        public void doCodeAnalysis()
        {
            findTypesAndFuncs();
            if(isRelationshipsReq)
                findTypeRelationships(files);            
        }


        //----< Find types and their function details >----------------
        private void findTypesAndFuncs()
        {
            Parser parser = builder.build();
            int typesTableCount = 0;

            foreach (string file in files)
            {
                try
                {
                    if (!semi.open(file as string))
                        Console.WriteLine("cant open the file");

                    string f = (string)file;

                    string file_Name = Path.GetFileNameWithoutExtension(f);
                    Repository rep = Repository.getInstance();
                    rep.str_fil = file_Name;
                    //parsing each file
                    while (semi.getSemi())
                        parser.parse(semi);

                    semi.close();

                    //storing each file information

                    fileTypes.Add(new FileTypesInfo(file, rep.locations.GetRange(typesTableCount, rep.locations.Count - typesTableCount)));
                }
                catch (Exception e)
                {
                    Console.WriteLine("\n\n  Error while parsing file " + file + ": " + e.Message + "\n");
                }
                finally
                {
                    typesTableCount = Repository.getInstance().locations.Count;
                }
            }
        }


        //----< Find relationships between types in the given files >----------------
        public void findTypeRelationships(List<string> files)
        {
            Parser parser = builder.buildRelsParser();

            foreach (object file in files)
            {
                try
                {
                    if (!semi.open(file as string))
                        Console.WriteLine("Cannot open the file");
                       // Display.displayStr("\n\n  Error: Can't open " + file + "\n");

                    //parsing
                    while (semi.getSemi())
                        parser.parse(semi);

                    semi.close();
                }
                catch (Exception e)
                {
                    Console.WriteLine("\n\n  Error while parsing file " + file + ": " + e.Message + "\n");
                }
            }
        }


        //----< Test Stub >--------------------------------------------------

        #if(TEST_CODEANALYZER)

        static void Main(string[] args)
        {
            Console.Write("\n  Demonstrating CodeAnalyzer");
            Console.Write("\n =============================\n");

            List<string> files = new List<string>();
            files.Add("../../CodeAnalyzer.cs");

            CodeAnalyzer ca = new CodeAnalyzer(files, false);
            ca.doCodeAnalysis();              

            foreach (FileTypesInfo file in ca.fileTypes)
            {
                List<Elem> typesAndFuncs = file.typesAndFuncs;
                List<Elem> fileTypes = typesAndFuncs.FindAll(t => t.type != "function");
                List<Elem> fileFunctions = typesAndFuncs.FindAll(t => t.type == "function");
                
                Console.WriteLine("Processing File: " + file.fileName, '*');

                Console.WriteLine("\n");
                Console.WriteLine("Types defined:", '=');
                Console.WriteLine("\n  {0,-10}\t{1}", "Type", "Name");
                Console.Write("\n  {0,-10}\t{1}", "----", "----");
                foreach (Elem e in fileTypes)
                    Console.Write("\n  {0,-10}\t{1}", e.type, e.typeNamespace + "." + e.name);

                                       
            }

        }
        #endif
    } //CodeAnalyzer class
}
