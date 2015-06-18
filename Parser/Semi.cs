/////////////////////////////////////////////////////////////////////////
// Semi.cs   -  Builds semiExpressions                                 //
// ver 2.2                                                             //
// Language:    C#, Visual Studio 10.0, .Net Framework 4.0             //
// Platform:    Dell Precision T7400 , Win 7, SP 1                     //
// Application: Pr#2 Help, CSE681, Fall 2011                           //
// Author:      Jim Fawcett, CST 2-187, Syracuse University            //
//              (315) 443-3948, jfawcett@twcny.rr.com                  //
/////////////////////////////////////////////////////////////////////////
/*
 * Module Operations
 * =================
 * Semi provides, via class CSemiExp, facilities to extract semiExpressions.
 * A semiExpression is a sequence of tokens that is just the right amount
 * of information to parse for code analysis.  SemiExpressions are token
 * sequences that end in "{" or "}" or ";"
 * 
 * CSemiExp works with a private CToker object attached to a specified file.
 * It provides a get() function that extracts semiExpressions from the file
 * while filtering out comments and merging quotes into single tokens.
 * 
 * Public Interface
 * ================
 * CSemiExp semi = new CSemiEx;();    // constructs CSemiExp object
 * if(semi.open(fileName)) ...        // attaches semi to specified file
 * semi.close();                      // closes file stream
 * if(semi.Equals(se)) ...            // do these semiExps have same tokens?
 * int hc = semi.GetHashCode()        // returns hashcode
 * if(getSemi()) ...                  // extracts and stores next semiExp
 * int len = semi.count;              // length property
 * semi.verbose = true;               // verbose property - shows tokens
 * string tok = semi[2];              // access a semi token
 * string tok = semi[1];              // extract token
 * semi.flush();                      // removes all tokens
 * semi.initialize();                 // adds ";" to empty semi-expression
 * semi.insert(2,tok);                // inserts token as third element
 * semi.Add(tok);                     // appends token
 * semi.Add(tokArray);                // appends array of tokens
 * semi.display();                    // sends tokens to Console
 * string show = semi.displayStr();   // returns tokens as single string
 * semi.returnNewLines = false;       // property defines newline handling
 *                                    //   default is true
 */
//
/*
 * Build Process
 * =============
 * Required Files:
 *   Semi.cs Toker.cs
 * 
 * Compiler Command:
 *   csc /target:exe /define:TEST_SEMI Semi.cs Toker.cs
 * 
 * Maintenance History
 * ===================
 * ver 2.2 : 14 Aug 14
 * - added folding rule for "for(int i=0; i<count; ++i)" type statements
 * ver 2.1 : 24 Sep 11
 * - collect line starting with # and ending with \n as semiExpression.
 * ver 2.0 : 05 Sep 11
 * - Converted to new C# property syntax
 * - Converted from untyped ArrayList to generic List<string>
 * - Simplified display() and displayStr()
 * - Added new tests in test stub
 * ver 1.9 : 27 Sep 08
 * - Changed comments on manual page to say that semi.ReturnNewLines is true by default
 * ver 1.8 : 10 Jun 08
 * - Aniruddha Gore added Contains function and set returnNewLines as the default
 * ver 1.7 : 17 Jun 06
 * - added displayNewLines property
 * ver 1.6 : 16 Jun 06
 * - added CSemi member functions copy(), remove(int i), and remove(string tok).
 * ver 1.5 : 12 Jun 05
 * - added returnNewLines property
 * - modified way get() behaves so that it will not hang on files that
 *   end with text that have no semiExp terminator.
 * ver 1.4 : 30 May 05
 * - removed CppCommentFilter, CCommentFilter, SQuoteFilter, DQuoteFilter
 *   since Toker now returns comments and quotes as tokens.
 * - added isComment(string tok) member function
 * ver 1.3 : 16 Sep 03
 * - removed insert(tokenArray), added Add(tokenArray)
 *   Since this is a change to public interface it may break some code.
 *   It simply changes the name of the function to more directly 
 *   describe what it does - append a token array.
 * - added overrides of Equals(object) and GetHashCode()
 * - completed Manual Page description of public interface
 * ver 1.2 : 14 Sep 03
 * - cosmetic changes to comments
 * - Added formatting of extracted comments (see notes in code below)
 * ver 1.1 : 13 Sep 03
 * - fixed bug in CppCommentFilter() that caused collection to terminate
 *   if a C++ comment was on same line as a semiExpression.
 * - added calls to semiExp.Add(currTok) in SQuoteFilter() and DQuoteFilter()
 *   which simplified getSemi().
 * - added some functions to create and manipulate semi-expressions.
 * ver 1.0 : 31 Aug 03
 * - first release
 * 
 * Planned Modifications:
 * ----------------------
 * - return, or don't return, comments based on discardComments property
 *   which is now present but inactive.
 */
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using CStoker;

namespace CSsemi
{
  ///////////////////////////////////////////////////////////////////////
  // class CSemiExp - filters token stream and collects semiExpressions

  public class CSemiExp
  {
    CToker toker = null;
    List<string> semiExp = null;
    string currTok = "";
    string prevTok = "";

    //----< line count property >----------------------------------------

    public int lineCount
    {
      get { return toker.lineCount; }
    }
    //----< constructor >------------------------------------------------

    public CSemiExp()
    {
      toker = new CToker();
      semiExp = new List<string>();
      discardComments = true;  // not implemented yet
      returnNewLines = true;
      displayNewLines = false;
    }

    //----< test for equality >------------------------------------------

    override public bool Equals(Object semi)
    {
      CSemiExp temp = (CSemiExp)semi;
      if(temp.count != this.count)
        return false;
      for(int i=0; i<temp.count && i<this.count; ++i)
        if(this[i] != temp[i])
          return false;
      return true;
    }

    //---< pos of first str in semi-expression if found, -1 otherwise >--

    public int FindFirst(string str)
    {
      for (int i = 0; i < count - 1; ++i)
        if (this[i] == str)
          return i;
      return -1;
    }
    //---< pos of last str in semi-expression if found, -1 otherwise >--- 

    public int FindLast(string str)
    {
      for (int i = this.count - 1; i >= 0; --i)
        if (this[i] == str)
          return i;
      return -1;
    }
    //----< deprecated: here to avoid breakage with old code >----------- 

    public int Contains(string str)
    {
      return FindLast(str);
    }
    //----< have to override GetHashCode() >-----------------------------

    override public System.Int32 GetHashCode()
    {
      return base.GetHashCode();
    }
    //----< opens member tokenizer with specified file >-----------------

    public bool open(string fileName)
    {
      return toker.openFile(fileName);
    }
    //----< close file stream >------------------------------------------

    public void close()
    {
      toker.close();
    }
    //----< is this the last token in the current semiExpression? >------

    bool isTerminator(string tok)
    {
      switch(tok)
      {
        case ";" : return true;
        case "{" : return true;
        case "}" : return true;
        case "\n" :
          if (this.FindFirst("#") != -1)  // expensive - may wish to cache in get
            return true;
          return false;
        default  : return false;
      }
    }
    //----< get next token, saving previous token >----------------------

    string get()
    {
      prevTok = currTok;
      currTok = toker.getTok();
      if(verbose)
        Console.Write("{0} ",currTok);
      return currTok;
    }
    //----< is this character a punctuator> >----------------------------

    bool IsPunc(char ch)
    {
      return (Char.IsPunctuation(ch) || Char.IsSymbol(ch));
    }
    //
    //----< are these characters an operator? >--------------------------
    //
    // Performance issue - C# would not let me make opers static, so
    // it is being constructed on every call.  This is not desireable,
    // but neither is using a static data member that is initialized
    // remotely.  I will think more about this later.

    bool IsOperatorPair(char first, char second)
    { 
      string[] opers = new string[]
      { 
        "/*", "*/", "//", "!=", "==", ">=", "<=", "&&", "||", "--", "++",
        "+=", "-=", "*=", "/=", "%=", "&=", "^=", "|=", "<<", ">>",
        "\\n", "\\t", "\\r", "\\f"
      };

      StringBuilder test = new StringBuilder();
      test.Append(first).Append(second);
      foreach(string oper in opers)
        if(oper.Equals(test.ToString()))
          return true;
      return false;
    }
    //----< collect semiExpression from filtered token stream >----------

    public bool getSemi()
    {
      semiExp.RemoveRange(0,semiExp.Count);  // empty container
      do
      {
        get();
        if(currTok == "")
          return false;  // end of file
        if(returnNewLines || currTok != "\n")
          semiExp.Add(currTok);
      } while(!isTerminator(currTok) || count == 0);
      
      // if for then append next two semiExps, e.g., for(int i=0; i<se.count; ++i) {

      if(semiExp.Contains("for"))
      {
        CSemiExp se = clone();
        getSemi();
        se.Add(semiExp.ToArray());
        getSemi();
        se.Add(semiExp.ToArray());
        semiExp.Clear();
        for (int i = 0; i < se.count; ++i)
          semiExp.Add(se[i]);
      }
      return (semiExp.Count > 0);
    }
    //----< get length property >----------------------------------------

    public int count
    {
      get { return semiExp.Count; }
    }
    //----< indexer for semiExpression >---------------------------------

    public string this[int i]
    {
      get { return semiExp[i]; }
      set { semiExp[i] = value;        }
    }
    //----< insert token - fails if out of range and returns false>------

    public bool insert(int loc, string tok)
    {
      if(0 <= loc && loc < semiExp.Count)
      {
        semiExp.Insert(loc,tok);
        return true;
      }
      return false;
    }
    //----< append token to end of semiExp >-----------------------------

    public CSemiExp Add(string token)
    {
      semiExp.Add(token);
      return this;
    }
    //----< load semiExp from array of strings >-------------------------

    public void Add(string [] source)
    {
      foreach(string tok in source)
        semiExp.Add(tok);
    }
    //--< initialize semiExp with single ";" token - used for testing >--

    public bool initialize()
    {
      if(semiExp.Count > 0)
        return false;
      semiExp.Add(";");
      return true;
    }
    //----< remove all contents of semiExp >-----------------------------

    public void flush()
    {
      semiExp.RemoveRange(0,semiExp.Count);
    }
    //----< is this token a comment? >-----------------------------------

    public bool isComment(string tok)
    {
      if(tok.Length > 1)
        if(tok[0] == '/')
          if(tok[1] == '/' || tok[1] == '*')
            return true;
      return false;
    }
    //----< display semiExpression on Console >--------------------------

    public void display()
    {
      Console.Write("\n");
      Console.Write(displayStr());
    }
    //----< return display string >--------------------------------------

    public string displayStr()
    {
      StringBuilder disp = new StringBuilder("");
      foreach (string tok in semiExp)
      {
        disp.Append(tok);
        if (tok.IndexOf('\n') != tok.Length-1)
          disp.Append(" ");
      }
      return disp.ToString();
    }
    //----< announce tokens when verbose is true >-----------------------

    public bool verbose
    {
      get;
      set;
    }
    //----< determines whether new lines are returned with semi >--------

    public bool returnNewLines
    {
      get;
      set;
    }
    //----< determines whether new lines are displayed >-----------------

    public bool displayNewLines
    {
      get;
      set;
    }
    //----< determines whether comments are discarded >------------------

    public bool discardComments
    {
      get;
      set;
    }
    //
    //----< make a copy of semiEpression >-------------------------------

    public CSemiExp clone()
    {
      CSemiExp copy = new CSemiExp();
      for (int i = 0; i < count; ++i)
      {
        copy.Add(this[i]);
      }
      return copy;
    }
    //----< remove a token from semiExpression >-------------------------

    public bool remove(int i)
    {
      if (0 <= i && i < semiExp.Count)
      {
        semiExp.RemoveAt(i);
        return true;
      }
      return false;
    }
    //----< remove a token from semiExpression >-------------------------

    public bool remove(string token)
    {
      if (semiExp.Contains(token))
      {
        semiExp.Remove(token);
        return true;
      }
      return false;
    }
    //
#if(TEST_SEMI)

    //----< test stub >--------------------------------------------------

    [STAThread]
    static void Main(string[] args)
    {
      Console.Write("\n  Testing semiExp Operations");
      Console.Write("\n ============================\n");

      CSemiExp test = new CSemiExp();
      test.returnNewLines = true;
      test.displayNewLines = true;

      string testFile = "../../testSemi.txt";
      if(!test.open(testFile))
        Console.Write("\n  Can't open file {0}",testFile);
      while(test.getSemi())
        test.display();
      
      test.initialize();
      test.insert(0,"this");
      test.insert(1,"is");
      test.insert(2,"a");
      test.insert(3,"test");
      test.display();

      Console.Write("\n  2nd token = \"{0}\"\n",test[1]);

      Console.Write("\n  removing first token:");
      test.remove(0);
      test.display();
      Console.Write("\n");

      Console.Write("\n  removing token \"test\":");
      test.remove("test");
      test.display();
      Console.Write("\n");

      Console.Write("\n  making copy of semiExpression:");
      CSemiExp copy = test.clone();
      copy.display();
      Console.Write("\n");

      if(args.Length == 0)
      {
        Console.Write("\n  Please enter name of file to analyze\n\n");
        return;
      }
      CSemiExp semi = new CSemiExp();
      semi.returnNewLines = true;
      if(!semi.open(args[0]))
      {
        Console.Write("\n  can't open file {0}\n\n",args[0]);
        return;
      }

      Console.Write("\n  Analyzing file {0}",args[0]);
      Console.Write("\n ----------------------------------\n");

      while(semi.getSemi())
        semi.display();
      semi.close();
    }
#endif
  }
}
