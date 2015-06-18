////////////////////////////////////////////////////////////////////////////
//  Display.cs  - shows all the display for the individual packages       //
//						by calling public interfaces in the display.cs    //
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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackageAnalysis
{
   public class Display
    {

        public void displayString(string str)
        {
            Console.Write(str);
        }


 #if(DISPLAY)

        static void Main(string[] args)
        {
            Display ds = new Display();
            ds.displayString("hello");
        }
#endif
    }
}
