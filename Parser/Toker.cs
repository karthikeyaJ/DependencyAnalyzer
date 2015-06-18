/////////////////////////////////////////////////////////////////////////
// Toker.cs  -  Tokenizer                                              //
//              Reads words and punctuation symbols from a file stream //
// ver 2.7                                                             //
// Language:    C#, Visual Studio 10.0, .Net Framework 4.0             //
// Platform:    Dell Precision T7400 , Win 7, SP 1                     //
// Application: Pr#2 Help, CSE681, Fall 2011                           //
// Author:      Jim Fawcett, CST 2-187, Syracuse University            //
//              (315) 443-3948, jfawcett@twcny.rr.com                  //
/////////////////////////////////////////////////////////////////////////
/*
 * Module Operations
 * =================
 * Toker provides, via the class CToker, the facilities to tokenize ASCII
 * text files.  That is, it composes the file's stream of characters into
 * words and punctuation symbols.
 * 
 * CToker works with a private buffer of characters from an attached file.
 * When the buffer is emptied CToker silently fills it again, so tokens
 * are always available until the end of file is reached.  End of file is
 * reported by tok = getTok() returning an empty token, e.g., tok == "".  
 *
 * Note: 
 * The tokenizer does not properly handle quoted strings that start
 * with the @ character to indicate \ should be treated as a character,
 * not the beginning of an escape sequence.
 * 
 * Public Interface
 * ================
 * CToker toker = new CToker();       // constructs CToker object
 * if(toker.openFile(fileName)) ...   // attaches toker to specified file
 * if(toker.openString(str)) ...      // attaches toker to specified string
 * toker.close();                     // closes stream
 * string tok = toker.getTok();       // extracts next token from stream
 * string tok = toker.peekNextTok();  // peeks but does not extract
 * toker.pushBack(tok);               // puts token back on stream
 * 
 */
/*
 * Build Process
 * =============
 * Required Files:
 *   Toker.cs
 * 
 * Compiler Command:
 *   csc /target:exe /define:TEST_CTOKER CToker.cs
 * 
 * Maintenance History
 * ===================
 * ver 2.7 : 21 Sep 14
 * - made returning comments optional
 * - fixed handling of @"..." strings
 * ver 2.6 : 19 Sep 14
 * - stopped returning comments in getTok function
 * ver 2.5 : 14 Aug 14
 * - added patch to handle @"..." string format
 * ver 2.4 : 24 Sep 11
 * - added a thrown exception if extract encounters a string with the 
 *   substring "@.  This should be handled but raises two many changes
 *   to fix immediately.
 * ver 2.3 : 05 Sep 11
 * - fixed bug collecting C Comments in eatCComment()
 * - fixed bug where token contained embedded newline, now broken
 *   into seperate tokens
 * - fixed ackward display formatting
 * - replaced untyped ArrayList with generic List<string> 
 * - added lineCount property
 * ver 2.2 : 10 Jun 08
 * - added IsGrammarPunctuation to make tokenizer treat underscore
 *   as an ASCII char rather than a punctuator and used that in
 *   fillBuffer and eatASCII
 * ver 2.1 : 14 Jun 05
 * - fixed newline handling bug in buffer filling routines:
 *   readLine, getLine, fillbuffer
 * - fixed newline handling bug in extractComment
 * ver 2.0 : 30 May 05
 * - added extraction of comments and quotes as tokens
 * - added openString(...) to attach tokenizer to string
 * ver 1.1 : 21 Sep 04
 * - added toker.close() in test stub
 * - added processing for all command line args
 * ver 1.0 : 31 Aug 03
 * - first release
 * 
 * Planned Changes:
 * ----------------
 * - Handle quoted strings that use the @"\X" construct to allow omitting 
 *   double \\ when \ should be treated like a character, not the beginning
 *   of an escape sequence. 
 * - Improve performance by change lineRemainder from string to StringBuilder
 *   to avoid a lot of copies.
 */
//
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace CStoker
{
  ///////////////////////////////////////////////////////////////////////
  // class CToker - tokenizer

  class CToker
  {
    private TextReader ts = null;            // source of tokens
    private List<string> tokBuffer = null;   // intermediate token store
    private string lineRemainder;            // unprocessed line fragment

    //----< return comments? property >----------------------------------

    public bool returnComments
    {
      get; set;
    }
    //----< line count property >----------------------------------------

    public int lineCount
    {
      get;
      private set;
    }
    //----< constructor >------------------------------------------------

    public CToker()
    {
      tokBuffer = new List<string>();
      lineCount = 0;
      returnComments = false;
    }
    //----< opens file stream for tokenizing >---------------------------

    public bool openFile(string fileName)
    {
      lineRemainder = "";
      try
      {
        ts = new StreamReader(fileName);
      }
      catch(Exception)
      {
        return false;
      }
      return true;
    }
    //----< opens string for tokenizing >------------------------------

    public bool openString(string source)
    {
      lineRemainder = "";
      try
      {
        ts = new StringReader(source);
      }
      catch(Exception)
      {
        return false;
      }
      return true;
    }
    //----< closes filestream >------------------------------------------

    public void close()
    {
      ts.Close();
    }
    //----< remove return character from StringBuilder >-----------------

    void removeReturn(ref StringBuilder tok)
    {
      for (int i = 0; i < tok.Length; ++i)
      {
        // stream readers tend to hand back strings with '\r' which
        // make processing more complicated, so we remove them
        if (tok[i] == '\r')
          tok.Remove(i, 1);
      }
    }
    //----< remove return character from string >------------------------

    string removeReturn(string tok)
    {
      StringBuilder temp = new StringBuilder();
      for (int i = 0; i < tok.Length; ++i)
      {
        if (tok[i] != '\r')
          temp.Append(tok[i]);
      }
      return temp.ToString();
    }
    //----< read a single line, retaining newline character >------------

    public string readLine()
    {
      StringBuilder temp = new StringBuilder();
      while(true)
      {
        int i = ts.Read();
        if ((char)i == '\n')
          lineCount++;
        if(i == -1)
        {
          return temp.ToString();
        }
        char ch = (char)i;
        temp.Append(ch);
        if(ch == '\n')
          break;
      }
      removeReturn(ref temp);
      string outstr = temp.ToString();
      return outstr;
    }
    //----< extracts line of text for tokenizing >-----------------------
    //
    //  Passes back a line to process for tokens as a side effect
    //  through the out string parameter.
    //  - if line has a leading comment or quote it is extracted and 
    //    saved in tokBuffer and remaining string is passed back
    //  - if line has a trailing comment or quote the line fragment
    //    at the front is passed back after saving the rest of the
    //    line for later processing
    //  - always passes back a line to process until end of file
    //  - returns true if end of file has not been reached
    //
    bool getLine(out string line)
    {
      do
      {
        if(lineRemainder == "")  // previously saved line fragment is empty
        {
          try
          {
            lineRemainder = readLine();

            if(lineRemainder == null || lineRemainder == "")
            {
              line = "";
              return false;     // end of file
            }
          }
          catch(Exception except)
          {
            line = except.Message.ToString();
            return false;       // error reading file
          }
        }
        line = extract(ref lineRemainder);
        
        // keep extracting until there is a line to tokenize
        // or tokBuffer has contents

      } while(line == "" && tokBuffer.Count == 0);
      return true;
    }
    //
    //----< extract tokens and comments >------------------------------
    //
    //  Extract the first of:
    //    C++ comments, C comments, double quotes, single quotes5
    //
    string extract(ref string lineRemainder)
    {
      char[] whiteChars = { ' ', '\r', '\t', '\f' };  // newlines are tokens
      lineRemainder = lineRemainder.TrimStart(whiteChars);

      int posErr = lineRemainder.IndexOf("@\"");
      if (posErr != -1)
        lineRemainder = mapToOldDoubleQuoteStyle(lineRemainder);

      int posCppComm = lineRemainder.IndexOf("//");
      int posCComm   = lineRemainder.IndexOf("/*");
      int posDQuote  = lineRemainder.IndexOf('\"');
      int posSQuote  = lineRemainder.IndexOf('\'');

      // find first of the above

      int[] positions = { posCppComm, posCComm, posDQuote, posSQuote };
      for(int i=0; i<positions.Length; ++i)
        if(positions[i] == -1)
          positions[i] = Int32.MaxValue;
      Array.Sort(positions);
      
      if(positions[0] == Int32.MaxValue)    // nothing to extract
      {
        string retStr = lineRemainder;
        lineRemainder = "";
        return retStr;
      }
      if (posCppComm == positions[0] || posCComm == positions[0])
        return extractComment(ref lineRemainder);
      if(posDQuote == positions[0])
        return extractDQuote(ref lineRemainder);
      if(posSQuote == positions[0])
        return extractSQuote(ref lineRemainder);
      throw new Exception("extract failed");
    }
    //
    //----< convert @ style string to old style >--------------------

    string mapToOldDoubleQuoteStyle(string str)
    {
      bool foundNewStyle = false;
      System.Text.StringBuilder temp = new StringBuilder();
      int i;
      for (i = 0; i < str.Length; ++i)
      {
        if (str[i] == '@')
        {
          foundNewStyle = true;
          continue;
        }
        temp.Append(str[i]);
        if (foundNewStyle)
        {
          if (str[i] == '\\')
            temp.Append('\\');
          if (str[i] == '"' && str[i - 1] != '\\' && str[i-1] != '@')
            break;
        }
      }
      for (int j = i + 1; j < str.Length; ++j)
        temp.Append(str[j]);
      return temp.ToString();
    }
    //
    //----< extract double quote >-------------------------------------

    string extractDQuote(ref string lineRemainder)
    {
      string retStr = "";
      int pos = lineRemainder.IndexOf('\"');
      if(pos == 0)
      {
        StringBuilder quote = new StringBuilder();
        quote.Append('\"');
        for(int i=1; i<lineRemainder.Length; ++i)
        {
          quote.Append(lineRemainder[i]);
          if(lineRemainder[i] == '\"')
          {
            if(lineRemainder[i-1] != '\\' || lineRemainder[i-2] == '\\')
            {
              tokBuffer.Add(quote.ToString());
              lineRemainder = lineRemainder.Remove(0,i+1);
              return "";
            }
          }
        }
      }
      else
      {
        retStr = lineRemainder.Remove(pos,lineRemainder.Length-pos);
        lineRemainder = lineRemainder.Remove(0,pos);
        return retStr;
      }
      //throw new Exception("extractDQuote failed");
      return retStr;
    }
    //
    //----< extract single quote >-------------------------------------

    string extractSQuote(ref string lineRemainder)
    {
      string retStr;
      int pos = lineRemainder.IndexOf('\'');
      if(pos == 0)
      {
        StringBuilder quote = new StringBuilder();
        quote.Append('\'');
        for(int i=1; i<lineRemainder.Length; ++i)
        {
          quote.Append(lineRemainder[i]);
          if(lineRemainder[i] == '\'')
          {
            if(lineRemainder[i-1] != '\\' || lineRemainder[i-2] == '\\')
            {
              tokBuffer.Add(quote.ToString());
              lineRemainder = lineRemainder.Remove(0,i+1);
              return "";
            }
          }
        }
      }
      else
      {
        retStr = lineRemainder.Remove(pos,lineRemainder.Length-pos);
        lineRemainder = lineRemainder.Remove(0,pos);
        return retStr;
      }
      throw new Exception("extractSQuote failed");
    }
    //
    //----< extract comment >------------------------------------------

    string extractComment(ref string lineRemainder)
    {
      char[] WhiteChars = { ' ', '\t', '\r' };
      string line;
      int pos = lineRemainder.IndexOf("//");
      if(pos == 0)                          // whole line is C++ comment
      {
        if(lineRemainder[lineRemainder.Length-1] == '\n')
        {
          lineRemainder = lineRemainder.Remove(lineRemainder.Length-1,1);
          tokBuffer.Add(lineRemainder);
          lineRemainder = "";
          return "\n";
        }
        else
        {
          tokBuffer.Add(lineRemainder);
          lineRemainder = "";
        }
        return lineRemainder;
      }
      if(pos > -1)                          // end of line is C++ comment
      {
        line = lineRemainder.Remove(pos,lineRemainder.Length-pos).TrimEnd(WhiteChars);
        lineRemainder = lineRemainder.Remove(0,pos);
        return line;
      }
      pos = lineRemainder.IndexOf("/*");    // line contains C comment
      if(pos > -1)
      {
        if(pos == 0)
        {
          eatCComment();
          return "";
        }
        else
        {
          string retStr = lineRemainder.Remove(pos,lineRemainder.Length-pos);
          lineRemainder = lineRemainder.Remove(0,pos);
          return retStr;
        }
      }
      // if we get here there is no comment in line

      line = lineRemainder;
      lineRemainder = "";
      return line;
    }
    //----< eat C comment - may consume more lines >---------------------

    void eatCComment()
    {
      List<char> comment = new List<char>();
      while(true)
      {
        int pos = lineRemainder.IndexOf("*/");
        for (int i = 0; i < lineRemainder.Length; ++i)
        {
          if(pos != i)  // not at end of comment
            comment.Add(lineRemainder[i]);
          else
          { // end of comment
            comment.Add(lineRemainder[i]);
            comment.Add(lineRemainder[i + 1]);
            string temp = new string(comment.ToArray());
            tokBuffer.Add(temp);
            lineRemainder = lineRemainder.Remove(0,i+2);
            return;
          }
        }
        // end of lineRemainder
        lineRemainder = ts.ReadLine();  // ReadLine discards newline
        lineCount++;
        if(lineRemainder == null)
        {
          throw new Exception("encountered eof while processing comment");
        }
        lineRemainder = lineRemainder + "\n";  // replace newline
        lineRemainder = removeReturn(lineRemainder);
      }
    }
    //
    //----< treat underscore as ASCII >----------------------------------

    bool IsGrammarPunctuation(char ch)
    {
      if (ch == '_')
        return false;
      if (Char.IsPunctuation(ch))
        return true;
      return false;
    }
    //----< consumes ASCII characters from stream >----------------------

    string eatAscii(ref string tok)
    {
      string retStr = tok;
      for(int i=0; i<tok.Length; ++i)
      {
        if(IsGrammarPunctuation(tok[i]) || Char.IsSymbol(tok[i]))
        {
          retStr = tok.Remove(i,tok.Length-i);
          tok = tok.Remove(0,i);
          return retStr;
        }
      }
      tok = "";
      return retStr;
    }
    //----< consumes a single punctuator from stream >-------------------

    string eatPunctuationChar(ref string tok)
    {
      string retStr = tok.Remove(1,tok.Length-1);
      tok = tok.Remove(0,1);
      return retStr;
    }
    //----< fills internal buffer with tokens >--------------------------

    bool fillBuffer()
    {
      string line;
      if(!this.getLine(out line))
        return false;             // end of token source
      if(line == "")
        return (tokBuffer.Count > 0);
      char [] delim = { ' ', '\t', '\f' };
      string [] toks = line.Split(delim);
      foreach(string tok in toks)
      {
        string temp = tok;
        while(temp.Length > 0)
        {
          if(IsGrammarPunctuation(temp[0]) || Char.IsSymbol(temp[0]))
          {
            string punc = this.eatPunctuationChar(ref temp);
            tokBuffer.Add(punc);
          }
          else
          {
            string ascii = this.eatAscii(ref temp);
            tokBuffer.Add(ascii);
          }
        }
      }
      return true;
    }
    //----< extracts tokens from internal buffer, filling if needed >----

    public string getTok()
    {
      char[] trimChar = { '\n' };
      string tok = peekNextTok();
      if(tok != "")
        tokBuffer.RemoveAt(0);
      if (tok.IndexOf('\n') == tok.Length - 1 && tok.Length > 1)
      {
        tok = tok.TrimEnd(trimChar);
        tokBuffer.Insert(0, "\n");
      }
      if (returnComments)
        return tok;

      while(true)  // skip comments
      {
        if(tok.Length > 1 && tok[0] == '/' && (tok[1] == '*' || tok[1] == '/'))
          tok = getTok();
        else
          break;
      }
      return tok;
    }
    //----< look at next token without extracting >----------------------

    public string peekNextTok()
    {
      if(tokBuffer.Count == 0)
        if(!fillBuffer())
          return "";
      string tok = (string)tokBuffer[0];
      return tok;
    }
    //----< put token back into tokBuffer >------------------------------

    public void pushBack(string tok)
    {
      tokBuffer.Insert(0,tok);
    }

    //----< test stub >--------------------------------------------------

#if(TEST_TOKER)

    [STAThread]
    static void Main(string[] args)
    {
      Console.Write("\n  Testing CToker - Tokenizer ");
      Console.Write("\n ============================\n");

      try
      {
        CToker toker = new CToker();
        //toker.returnComments = true;

        if (args.Length == 0)
        {
          Console.Write("\n  Please enter name of file to tokenize\n\n");
          return;
        }
        foreach (string file in args)
        {
          string msg1;
          if (!toker.openFile(file))
          {
            msg1 = "Can't open file " + file;
            Console.Write("\n\n  {0}", msg1);
            Console.Write("\n  {0}", new string('-', msg1.Length));
          }
          else
          {
            msg1 = "Processing file " + file;
            Console.Write("\n\n  {0}", msg1);
            Console.Write("\n  {0}", new string('-', msg1.Length));
            string tok = "";
            while ((tok = toker.getTok()) != "")
              if (tok != "\n")
                Console.Write("\n{0}", tok);
            toker.close();
          }
        }
        Console.Write("\n");
        //
        string[] msgs = new string[12];
        msgs[0] = "abc";
        msgs[11] = "-- \"abc def\" --";
        msgs[1] = "string with double quotes \"first quote\""
                  + " and \"second quote\" but no more";
        msgs[2] = "string with single quotes \'1\' and \'2\'";
        msgs[3] = "string with quotes \"first quote\" and \'2\'";
        msgs[4] = "string with C comments /* first */ and /*second*/ but no more";
        msgs[10] = @"string with @ \\stuff";
        msgs[5] = "/* single C comment */";
        msgs[6] = " -- /* another single comment */ --";
        msgs[7] = "// a C++ comment\n";
        msgs[8] = "// another C++ comment\n";
        msgs[9] = "/*\n *\n *\n */";

        foreach (string msg in msgs)
        {
          if (!toker.openString(msg))
          {
            string msg2 = "Can't open string for reading";
            Console.Write("\n\n  {0}", msg2);
            Console.Write("\n  {0}", new string('-', msg2.Length));
          }
          else
          {
            string msg2 = "Processing \"" + msg + "\"";
            Console.Write("\n\n  {0}", msg2);
            Console.Write("\n  {0}", new string('-', msg2.Length));
            string tok = "";
            while ((tok = toker.getTok()) != "")
            {
              if (tok != "\n")
                Console.Write("\n{0}", tok);
              else
                Console.Write("\nnewline");
            }
            toker.close();
          }
        }
        Console.Write("\n\n");
      }
      catch (Exception ex)
      {
        Console.Write("\n\n  token \"{0}\" has embedded newline\n\n", ex.Message);
      }
    }
#endif
  }
}
