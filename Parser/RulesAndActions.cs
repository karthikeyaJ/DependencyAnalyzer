////////////////////////////////////////////////////////////////////////////
//  RulesAndActions.cs  - Parser rules specific to an application         //
//                                                                        //
//  ver 2.3                                                               //
//  Language:     Visual C# 2013, Ultimate                                //
//  Platform:     HP Split 13 *2 PC, Microsoft Windows 8, Build 9200      //
//  Application:  CSE681 Pr2, Code Analysis Project                       //
//  Author:       Venkata Karthikeya Jangal,                              // 
//				  Master's - Computer Engineering,                        //
//				  Syracuse University,                                    //
//				  vjangal@syr.edu                                          //
////////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * RulesAndActions package contains all of the Application specific
 * code required for most analysis tools.
 *
 * It defines the following rules which each have a
 * grammar construct detector and also a collection of IActions:
 *   - DetectNameSpace rule
 *   - DetectClass rule
 *   - DetectFunction rule
 *   - DetectScopeChange
 *   - DetectDelegate
 *   - DetectInheritance
 *   - DetectAggregation
 *   - DetectComposition
 *   - DetectUsing
 *   - DetectBracelessScope
 *   - DetectOpenBraceScope
 *   - DetectCloseBraceScope
 *   
 *   actions - some are specific to a parent rule:
 *   - Print
 *   - PrintFunction
 *   - PrintScope
 *   - PushBracelessScope
 *   - PushRelationship
 *   - PushScopeInStack
 *   - PushScopeFromStack
 * 
 * The package also defines a Repository class for passing data between
 * actions and uses the services of a ScopeStack, defined in a package
 * of that name.
 *
 * Note:
 * This package does not have a test stub since it cannot execute
 * without requests from Parser.
 *  
 */
/* Required Files:
 *   IRuleAndAction.cs, RulesAndActions.cs, Parser.cs, ScopeStack.cs,
 *   Semi.cs, Toker.cs
 *   
 * Build command:
 *   csc /D:TEST_PARSER Parser.cs IRuleAndAction.cs RulesAndActions.cs \
 *                      ScopeStack.cs Semi.cs Toker.cs
 *   
 * Maintenance History:
 * --------------------
 * ver 2.3 : 28 Sep 2014
 * - added type relationship detection rules and actions
 * - storing identified relationshiops in a list in Repository
 * - Added rules to detect brace less scopes
 * - Added a rule to detect delegate
 * - Counted complexity for a function whenever new scope is encountered.
 * ver 2.2 : 24 Sep 2011
 * - modified Semi package to extract compile directives (statements with #)
 *   as semiExpressions
 * - strengthened and simplified DetectFunction
 * - the previous changes fixed a bug, reported by Yu-Chi Jen, resulting in
 * - failure to properly handle a couple of special cases in DetectFunction
 * - fixed bug in PopStack, reported by Weimin Huang, that resulted in
 *   overloaded functions all being reported as ending on the same line
 * - fixed bug in isSpecialToken, in the DetectFunction class, found and
 *   solved by Zuowei Yuan, by adding "using" to the special tokens list.
 * - There is a remaining bug in Toker caused by using the @ just before
 *   quotes to allow using \ as characters so they are not interpreted as
 *   escape sequences.  You will have to avoid using this construct, e.g.,
 *   use "\\xyz" instead of @"\xyz".  Too many changes and subsequent testing
 *   are required to fix this immediately.
 * ver 2.1 : 13 Sep 2011
 * - made BuildCodeAnalyzer a public class
 * ver 2.0 : 05 Sep 2011
 * - removed old stack and added scope stack
 * - added Repository class that allows actions to save and 
 *   retrieve application specific data
 * - added rules and actions specific to Project #2, Fall 2010
 * ver 1.1 : 05 Sep 11
 * - added Repository and references to ScopeStack
 * - revised actions
 * - thought about added folding rules
 * ver 1.0 : 28 Aug 2011
 * - first release
 *
 * Planned Modifications (not needed for Project #2):
 * --------------------------------------------------
 * - add folding rules:
 *   - CSemiExp returns for(int i=0; i<len; ++i) { as three semi-expressions, e.g.:
 *       for(int i=0;
 *       i<len;
 *       ++i) {
 *     The first folding rule folds these three semi-expression into one,
 *     passed to parser. 
 *   - CToker returns operator[]( as four distinct tokens, e.g.: operator, [, ], (.
 *     The second folding rule coalesces the first three into one token so we get:
 *     operator[], ( 
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace PackageAnalysis
{
    public class Elem  // holds scope information
    {
        public string type { get; set; }
        public string name { get; set; }
        public int begin { get; set; }
        public int end { get; set; }
        public int complexity { get; set; }
        public string typeNamespace { get; set; }
        public string typeClassName { get; set; }

        public override string ToString()
        {
            StringBuilder temp = new StringBuilder();
            temp.Append("{");
            temp.Append(String.Format("{0,-10}", type)).Append(" : ");
            temp.Append(String.Format("{0,-10}", name)).Append(" : ");
            temp.Append(String.Format("{0,-5}", begin.ToString()));  // line of scope start
            temp.Append(String.Format("{0,-5}", end.ToString()));    // line of scope end
            temp.Append("}");
            return temp.ToString();
        }
    }


    //----< Relationship class - holds type relationship information >----------------
    public class Relationship
    {
        public string type1Namespace { get; set; }
        public string type2Namespace { get; set; }
        public string type1 { get; set; }
        public string type2 { get; set; }
        public string relation { get; set; }

        public bool dependent { get; set; }
        public override string ToString()
        {
            StringBuilder temp = new StringBuilder();
            temp.Append("{");
            temp.Append(String.Format("{0,-10}", type1Namespace)).Append(".");
            temp.Append(String.Format("{0,-10}", type1)).Append(" - ");
            temp.Append(String.Format("{0,-10}", type2Namespace)).Append(".");
            temp.Append(String.Format("{0,-10}", type2)).Append(" : ");
            temp.Append(String.Format("{0,-25}", relation));
            temp.Append("}");
            return temp.ToString();
        }
    }

    public class Repository
    {
        ScopeStack<Elem> stack_ = new ScopeStack<Elem>();
        List<Elem> locations_ = new List<Elem>();
        List<Relationship> relationships_ = new List<Relationship>();
        static Repository instance;

        public string str_fil { get; set; }
        public int complexityCount { get; set; }
        public Repository()
        {
            instance = this;
        }

        public static Repository getInstance()
        {
            return instance;
        }
        // provides all actions access to current semiExp

        public CSsemi.CSemiExp semi
        {
            get;
            set;
        }

        // semi gets line count from toker who counts lines
        // while reading from its source

        public int lineCount  // saved by newline rule's action
        {
            get { return semi.lineCount; }
        }
        public int prevLineCount  // not used in this demo
        {
            get;
            set;
        }
        // enables recursively tracking entry and exit from scopes

        public ScopeStack<Elem> stack  // pushed and popped by scope rule's action
        {
            get { return stack_; }
        }
        // the locations table is the result returned by parser's actions
        // in this demo

        public List<Elem> locations
        {
            get { return locations_; }
        }

        //returns relationships list
        public List<Relationship> relationships
        {
            get { return relationships_; }
        }

        //To get Current Namespace
        public string getCurrentNamespace()
        {
            //checks from the stack the recent namespace and returns it.
            for (int i = stack.count - 1; i >= 0; i--)
            {
                if (stack[i].type == "namespace")
                {
                    return stack[i].name;
                }
            }
            return "";
        }


        //To get Current Type  
        public string getCurrentType()
        {
            //checks from the stack the recent type and returns it.
            for (int i = stack.count - 1; i >= 0; i--)
            {
                if (stack[i].type == "class" || stack[i].type == "struct" || stack[i].type == "interface")
                {
                    return stack[i].name;
                }
            }
            return "";
        }
    }
    /////////////////////////////////////////////////////////
    // pushes scope info on stack when entering new scope

    public class PushStack : AAction
    {
        Repository repo_;

        public PushStack(Repository repo)
        {
            repo_ = repo;
        }
        public override void doAction(CSsemi.CSemiExp semi)
        {
            Elem elem = new Elem();
            elem.typeNamespace = repo_.getCurrentNamespace();
            elem.type = semi[0];  // expects type
            elem.name = semi[1];  // expects name
            elem.begin = repo_.semi.lineCount - 1;
            elem.end = 0;
            elem.complexity = 0;
            elem.typeClassName = repo_.str_fil;
            //storing class name for functions
            if (elem.type == "function")
                elem.typeClassName = repo_.getCurrentType();

            repo_.stack.push(elem);
            if (elem.type == "function")
                repo_.complexityCount = 1;
            else if (elem.type != "array")    //not counting arrays braces towards complexity
                repo_.complexityCount++;

            if (elem.type == "control" || elem.name == "anonymous" || elem.type == "namespace")
                return;
            repo_.locations.Add(elem);

            if (AAction.displaySemi)
            {
                Console.Write("\n  line# {0,-5}", repo_.semi.lineCount - 1);
                Console.Write("entering ");
                string indent = new string(' ', 2 * repo_.stack.count);
                Console.Write("{0}", indent);
                this.display(semi); // defined in abstract action
            }
            if (AAction.displayStack)
                repo_.stack.display();
        }
    }
    /////////////////////////////////////////////////////////
    // pops scope info from stack when leaving scope

    public class PopStack : AAction
    {
        Repository repo_;

        public PopStack(Repository repo)
        {
            repo_ = repo;
        }
        public override void doAction(CSsemi.CSemiExp semi)
        {
            Elem elem;
            try
            {
                elem = repo_.stack.pop();
                for (int i = 0; i < repo_.locations.Count; ++i)
                {
                    Elem temp = repo_.locations[i];
                    if (elem.type == temp.type)
                    {
                        if (elem.name == temp.name)
                        {
                            if ((repo_.locations[i]).end == 0)
                            {
                                (repo_.locations[i]).end = repo_.semi.lineCount;
                                //updating functions complexity count
                                if (elem.type == "function")
                                    (repo_.locations[i]).complexity = repo_.complexityCount;
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
                Console.Write("popped empty stack on semiExp: ");
                semi.display();
                return;
            }
            CSsemi.CSemiExp local = new CSsemi.CSemiExp();
            local.Add(elem.type).Add(elem.name);
            if (local[0] == "control")
                return;

            if (AAction.displaySemi)
            {
                Console.Write("\n  line# {0,-5}", repo_.semi.lineCount);
                Console.Write("leaving  ");
                string indent = new string(' ', 2 * (repo_.stack.count + 1));
                Console.Write("{0}", indent);
                this.display(local); // defined in abstract action
            }
        }
    }
    ///////////////////////////////////////////////////////////
    // action to print function signatures - not used in demo

    public class PrintFunction : AAction
    {
        Repository repo_;

        public PrintFunction(Repository repo)
        {
            repo_ = repo;
        }
        public override void display(CSsemi.CSemiExp semi)
        {
            Console.Write("\n    line# {0}", repo_.semi.lineCount - 1);
            Console.Write("\n    ");
            for (int i = 0; i < semi.count; ++i)
                if (semi[i] != "\n" && !semi.isComment(semi[i]))
                    Console.Write("{0} ", semi[i]);
        }
        public override void doAction(CSsemi.CSemiExp semi)
        {
            this.display(semi);
        }
    }
    /////////////////////////////////////////////////////////
    // concrete printing action, useful for debugging

    public class Print : AAction
    {
        Repository repo_;

        public Print(Repository repo)
        {
            repo_ = repo;
        }
        public override void doAction(CSsemi.CSemiExp semi)
        {
            Console.Write("\n  line# {0}", repo_.semi.lineCount - 1);
            this.display(semi);
        }
    }
    /////////////////////////////////////////////////////////
    // rule to detect namespace declarations

    public class DetectNamespace : ARule
    {
        public override bool test(CSsemi.CSemiExp semi)
        {
            int index = semi.Contains("namespace");
            if (index != -1 && index + 1 < semi.count)
            {
                CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                // create local semiExp with tokens for type and name
                local.displayNewLines = false;
                local.Add(semi[index]).Add(semi[index + 1]);
                doActions(local);
                return true;
            }
            return false;
        }
    }
    /////////////////////////////////////////////////////////
    // rule to dectect class definitions

    public class DetectClass : ARule
    {
        public override bool test(CSsemi.CSemiExp semi)
        {
            /*int indexCL = semi.Contains("class");
            int indexIF = semi.Contains("interface");
            int indexST = semi.Contains("struct");

            int index = Math.Max(indexCL, indexIF);
            index = Math.Max(index, indexST);  */
            int index = indexOfType(semi);
            if (index != -1 && index + 1 < semi.count)
            {
                CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                // local semiExp with tokens for type and name
                local.displayNewLines = false;
                local.Add(semi[index]).Add(semi[index + 1]);
                doActions(local);
                return true;
            }
            return false;
        }
    }
    /////////////////////////////////////////////////////////
    // rule to dectect function definitions

    //public class DetectFunction : ARule
    //{
    //  public static bool isSpecialToken(string token)
    //  {
    //    string[] SpecialToken = { "if", "for", "foreach", "while", "catch", "using" };
    //    foreach (string stoken in SpecialToken)
    //      if (stoken == token)
    //        return true;
    //    return false; 
    //  }
    //  public override bool test(CSsemi.CSemiExp semi)
    //  {
    //    if (semi[semi.count - 1] != "{")
    //      return false;

    //    int index = semi.FindFirst("(");
    //    if (index > 0 && !isSpecialToken(semi[index - 1]))
    //    {
    //      CSsemi.CSemiExp local = new CSsemi.CSemiExp();
    //      local.Add("function").Add(semi[index - 1]);
    //      doActions(local);
    //      return true;
    //    }
    //    return false;
    //  }
    //}
    /////////////////////////////////////////////////////////
    // detect entering anonymous scope
    // - expects namespace, class, and function scopes
    //   already handled, so put this rule after those
    public class DetectAnonymousScope : ARule
    {
        public override bool test(CSsemi.CSemiExp semi)
        {
            int index = semi.Contains("{");
            if (index != -1)
            {
                CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                // create local semiExp with tokens for type and name
                local.displayNewLines = false;
                if (index > 0 && semi[index - 1] == "=")
                    local.Add("array").Add("anonymous");
                else
                    local.Add("control").Add("anonymous");
                doActions(local);
                return true;
            }
            return false;
        }
    }
    /////////////////////////////////////////////////////////
    // detect leaving scope

    public class DetectLeavingScope : ARule
    {
        public override bool test(CSsemi.CSemiExp semi)
        {
            int index = semi.Contains("}");
            if (index != -1)
            {
                doActions(semi);
                return true;
            }
            return false;
        }
    }


    /////////////////////////////////////////////////////////
    // rule to detect delegate declarations
    public class DetectDelegate : ARule
    {
        //----< checks for delegate keyword >---------
        public override bool test(CSsemi.CSemiExp semi)
        {
            //searching for delegate keyword
            int index = semi.Contains("delegate");
            int indexBrace = semi.Contains("(");
            if (index != -1 && indexBrace != -1 && indexBrace > index)
            {
                CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                // create local semiExp with tokens for type and name
                local.displayNewLines = false;
                local.Add(semi[index]).Add(semi[indexBrace - 1]);
                doActions(local);
                return true;
            }
            return false;
        }
    }


    //////////////////////////////////////////////
    // Delegate detection rule action
    public class PushDelegate : AAction
    {
        Repository repo_;

        public PushDelegate(Repository repo)
        {
            repo_ = repo;
        }

        //----< pushes delegate information into Repository locations >---------
        public override void doAction(CSsemi.CSemiExp semi)
        {
            Elem elem = new Elem();
            elem.typeNamespace = repo_.getCurrentNamespace();
            elem.type = semi[0];  // expects type
            elem.name = semi[1];  // expects name
            elem.begin = repo_.semi.lineCount - 1;
            elem.end = 0;
            elem.complexity = 0;

            repo_.locations.Add(elem);        //adding to location table

            if (AAction.displaySemi)
            {
                Console.Write("\n  line# {0,-5}", repo_.semi.lineCount - 1);
                Console.Write("entering ");
                string indent = new string(' ', 2 * repo_.stack.count);
                Console.Write("{0}", indent);
                this.display(semi); // defined in abstract action
            }
            if (AAction.displayStack)
                repo_.stack.display();
        }
    }


    /////////////////////////////////////////////////////////
    // rule to dectect braceless scopes 
    public class DetectBracelesScope : ARule
    {
        //----< tokens which can have braceless scopes >---------
        public bool isSpecialToken(string token)
        {
            string[] SpecialToken = { "if", "else", "for", "foreach", "while", "using" };
            foreach (string stoken in SpecialToken)
                if (stoken == token)
                    return true;
            return false;
        }

        //----< checks for special keywords without a brace "{" >---------
        public override bool test(CSsemi.CSemiExp semi)
        {
            //does not contain "{", contains special keyword , contain ";"
            if (semi.Contains("{") == -1 && semi.Contains(";") != -1)
            {
                for (int i = 0; i < semi.count; i++)
                {
                    if (isSpecialToken(semi[i]))
                    {
                        CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                        doActions(local);
                    }
                }
            }
            return false;
        }
    }


    /////////////////////////////////////////////////////////
    // Braceless scope detection rule action 
    public class PushBracelessScope : AAction
    {
        Repository repo_;

        public PushBracelessScope(Repository repo)
        {
            repo_ = repo;
        }

        //----< increases complexity count for braceless scopes >---------
        public override void doAction(CSsemi.CSemiExp semi)
        {
            repo_.complexityCount++;
        }
    }


    /////////////////////////////////////////////////////////
    // rule to dectect inheritance relationships
    public class DetectInheritance : ARule
    {
        //----< Calling action once the inheritance is detected >---------
        private void addInheritanceRelation(string typeNamespace2, string typeName1)
        {
            string typeName2 = "";
            //checking for multiple inheritance, if ',', add inheritance relation to all
            string[] type2NamespaceSplits = typeNamespace2.Split(',');
            for (int i = 0; i < type2NamespaceSplits.Length; i++)
            {
                typeNamespace2 = "";
                for (int j = 0; j < type2NamespaceSplits[i].Length; j++)
                {
                    //removing genercis part from the name of the type
                    if (type2NamespaceSplits[i][j] == '<')
                        break;
                    typeNamespace2 = typeNamespace2 + type2NamespaceSplits[i][j];
                }

                string[] fullNamespace = typeNamespace2.Split('.');
                typeName2 = fullNamespace[fullNamespace.Length - 1];  //getting only typeName, leaving namespace if it has

                //if the type exists in the locations table, call corresponding action
                if (Repository.getInstance().locations.Exists(t => t.type != "function" && t.name == typeName2))
                {
                    CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                    local.displayNewLines = false;

                    local.Add(typeName1).Add(Repository.getInstance().locations.Find(t => t.type != "function" && t.name == typeName2).typeNamespace).Add(typeNamespace2).Add("Inheritance");
                    doActions(local);
                }
            }
        }

        //----< Inheritance Relationship detection rule test >---------
        public override bool test(CSsemi.CSemiExp semi)
        {
            //checking for ":" and "{" in the semi and for type keywords like class
            int index = semi.Contains(":"), indexType = indexOfType(semi), indexBrace = semi.Contains("{");
            if (index != -1 && indexType != -1 && indexBrace != -1)
            {
                //getting derived class namespace and typename
                string typeNamespace1 = "", typeNamespace2 = "", typeName1 = "";
                for (int i = indexType + 1; i < semi.count; i++)
                {
                    if (semi[i] == ":" || semi[i] == "<")
                        break;
                    typeNamespace1 = typeNamespace1 + semi[i];
                }
                string[] type1NamespaceSplits = typeNamespace1.Split('.');
                typeName1 = type1NamespaceSplits[type1NamespaceSplits.Length - 1];    //seperating className from namespace

                //checking for type entry in locations table
                if (Repository.getInstance().locations.Exists(t => t.type != "function" && t.name == typeName1))
                {
                    for (int i = index + 1; i < indexBrace; i++)
                        typeNamespace2 = typeNamespace2 + semi[i];

                    //adds relation entry if type2 exists in relation table
                    addInheritanceRelation(typeNamespace2, typeName1);
                }
            }
            return false;
        }
    }


    /////////////////////////////////////////////////////////
    // rule to dectect aggregation relationships
    public class DetectAggregation : ARule
    {
        //----< Aggregation Relationship detection rule test >---------
        public override bool test(CSsemi.CSemiExp semi)
        {
            //checking for new keyword
            int index = semi.Contains("new");
            string typeNamespace2 = "", typeName2 = "";
            for (int i = index + 1; i < semi.count; i++)
            {
                if (semi[i] == "(" || semi[i] == "<")
                    break;
                typeNamespace2 = typeNamespace2 + semi[i];
            }

            //finding namespace and typename
            string[] type2NamespaceSplits = typeNamespace2.Split('.');
            typeName2 = type2NamespaceSplits[type2NamespaceSplits.Length - 1];

            //checking for the entry in locations table
            if (Repository.getInstance().locations.Exists(t => t.type != "function" && t.name == typeName2))
            {
                CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                local.displayNewLines = false;

                local.Add(Repository.getInstance().getCurrentType()).Add(Repository.getInstance().locations.Find(t => t.type != "function" && t.name == typeName2).typeNamespace).Add(typeNamespace2).Add("Aggregation");
                doActions(local);
                return true;
            }
            return false;
        }
    }


    /////////////////////////////////////////////////////////
    // rule to dectect composition relationships
    public class DetectComposition : ARule
    {
        //----< checks whether semi contains some keywords which cannot contain composition relationships >---------
        private bool isSemiContainsSpecialToken(CSsemi.CSemiExp semi)
        {
            string[] SpecialToken = { "if", "for", "foreach", "while", "catch", "using", "delegate", "new" };
            foreach (string stoken in SpecialToken)
                if (semi.Contains(stoken) != -1)
                    return true;
            return false;
        }


        //----< For removing types in generic containers >---------
        private bool isIndependent(CSsemi.CSemiExp semi, int i)
        {
            for (int j = i - 1; j >= 0; j--)
            {
                if (semi[j] == "<")
                {
                    for (int k = i + 1; k < semi.count; k++)
                        if (semi[k] == ">")
                            return false;
                }
            }
            return true;
        }

        //----< Composition Relationship detection rule test >---------
        public override bool test(CSsemi.CSemiExp semi)
        {
            if ((indexOfType(semi) == -1) && (semi.Contains(";") != -1) && (semi.Contains("{") == -1) && !(isSemiContainsSpecialToken(semi)))
            {
                for (int i = 0; i < semi.count; i++)
                {
                    //checking for entry in locations table
                    if (Repository.getInstance().locations.Exists(t => t.type != "function" && t.name == semi[i]) && isIndependent(semi, i))
                    {
                        //for removing static uses of a type
                        if (i < semi.count - 1 && semi[i + 1] == ".")
                            continue;

                        String type2Name = semi[i];
                        for (int j = i - 1; j > 0 && semi[j] == "."; j = j - 2)
                            type2Name = semi[j - 1] + semi[j] + type2Name;

                        CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                        local.displayNewLines = false;

                        if (Repository.getInstance().locations.Exists(t => (t.type == "struct" || t.type == "enum") && t.name == semi[i]))
                        {
                            string type2Namespace = Repository.getInstance().locations.Find(t => (t.type == "struct" || t.type == "enum") && t.name == semi[i]).typeNamespace;
                            //if contains "=", its using otherwise composition
                            if (semi.Contains("=") != -1)
                                local.Add(Repository.getInstance().getCurrentType()).Add(type2Namespace).Add(type2Name).Add("Using");
                            else
                                local.Add(Repository.getInstance().getCurrentType()).Add(type2Namespace).Add(type2Name).Add("Composition");
                            doActions(local);
                        }
                        else if (Repository.getInstance().locations.Exists(t => (t.type == "interface" || t.type == "class" || t.type == "delegate") && t.name == semi[i]) && semi.Contains("=") != -1)
                        {
                            string type2Namespace = Repository.getInstance().locations.Find(t => (t.type == "interface" || t.type == "class" || t.type == "delegate") && t.name == semi[i]).typeNamespace;
                            local.Add(Repository.getInstance().getCurrentType()).Add(type2Namespace).Add(type2Name).Add("Using");
                            doActions(local);
                        }
                    }
                }
            }
            return false;
        }
    }


    /////////////////////////////////////////////////////////
    // rule to dectect using relationships
    public class DetectUsing : ARule
    {
        //----< checks whether semi contains some keywords which cannot contain using relationships >---------
        private bool isSpecialToken(string token)
        {
            string[] SpecialToken = { "if", "for", "foreach", "while", "catch", "using" };
            foreach (string stoken in SpecialToken)
                if (stoken == token)
                    return true;
            return false;
        }

        //----< For removing types in generic containers >---------
        private bool isIndependent(CSsemi.CSemiExp semi, int i, int limit)
        {
            for (int j = i - 1; j >= limit && j >= 0; j--)
            {
                if (semi[j] == ",")
                    break;

                if (semi[j] == "<")
                {
                    for (int k = i + 1; k < semi.count; k++)
                        if (semi[k] == ">")
                            return false;
                }
            }
            return true;
        }

        //----< checking for static uses >---------      
        private void checkStaticUses(CSsemi.CSemiExp semi)
        {
            //checking for typeName followed by "."
            for (int i = 0; i < semi.count - 1; i++)
            {
                if (Repository.getInstance().locations.Exists(t => t.type != "function" && t.name == semi[i]) && semi[i + 1] == ".")
                {
                    String type2Name = semi[i];
                    for (int j = i - 1; j > 0; j = j - 2)
                    {
                        if (semi[j] == ".")
                            type2Name = semi[j - 1] + semi[j] + type2Name;
                        else
                            break;
                    }

                    CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                    local.displayNewLines = false;

                    string type2Namespace = Repository.getInstance().locations.Find(t => t.type != "function" && t.name == semi[i]).typeNamespace;
                    local.Add(Repository.getInstance().getCurrentType()).Add(type2Namespace).Add(type2Name).Add("Using");
                    doActions(local);
                }
            }
        }

        //----< Using Relationship detection rule test >---------
        public override bool test(CSsemi.CSemiExp semi)
        {
            //checking for function definitions 
            if (semi.Contains("{") != -1 && semi.FindFirst("(") > 0 && semi.FindFirst(")") > 0)
            {
                int indexOpenBrace = semi.FindFirst("(");
                int indexCloseBrace = semi.FindFirst(")");
                if (indexOpenBrace > 0 && !isSpecialToken(semi[indexOpenBrace - 1]))
                {
                    for (int i = indexOpenBrace + 1; i < indexCloseBrace; i++)
                    {
                        if (Repository.getInstance().locations.Exists(t => t.type != "function" && t.name == semi[i]) && isIndependent(semi, i, indexOpenBrace + 1))
                        {
                            String type2Name = semi[i];
                            for (int j = i - 1; j > 0; j = j - 2)
                            {
                                if (semi[j] == ".")
                                    type2Name = semi[j - 1] + semi[j] + type2Name;
                                else
                                    break;
                            }

                            CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                            local.displayNewLines = false;

                            string type2Namespace = Repository.getInstance().locations.Find(t => t.type != "function" && t.name == semi[i]).typeNamespace;
                            local.Add(Repository.getInstance().getCurrentType()).Add(type2Namespace).Add(type2Name).Add("Using");
                            doActions(local);
                        }
                    }
                }
            }

            checkStaticUses(semi);
            return false;
        }
    }


    ////////////////////////////////////////////////////////////////////////
    // Relationship detection rule action 
    public class PushRelationship : AAction
    {
        Repository repo_;

        public PushRelationship(Repository repo)
        {
            repo_ = repo;
        }

        //----< pushes relationship information into Repository relationships List >---------
        public override void doAction(CSsemi.CSemiExp semi)
        {
            Relationship relationship = new Relationship();
            relationship.type1Namespace = repo_.getCurrentNamespace();
            relationship.type1 = semi[0];  // expects type
            relationship.type2Namespace = semi[1];
            relationship.type2 = semi[2];  // expects name
            relationship.relation = semi[3];

            repo_.relationships.Add(relationship);

            if (AAction.displaySemi)
            {
                Console.Write("\n  {0,25}, {1,25}, {2,25}", relationship.type1Namespace + "." + relationship.type1, relationship.type2Namespace + "." + relationship.type2, relationship.relation);
                this.display(semi); // defined in abstract action
            }
        }
    }


    /////////////////////////////////////////////////////////
    // detect entering any scope. to store info in stack
    public class DetectOpenBraceScope : ARule
    {
        //----< detects open braces >---------
        public override bool test(CSsemi.CSemiExp semi)
        {
            //searching for brace "{"
            int index = semi.Contains("{");
            if (index != -1)
            {
                CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                // create local semiExp with tokens for type and name
                local.displayNewLines = false;
                if (semi.Contains("class") != -1)
                    local.Add("class").Add(semi[semi.Contains("class") + 1]);
                else if (semi.Contains("interface") != -1)
                    local.Add("interface").Add(semi[semi.Contains("interface") + 1]);
                else if (semi.Contains("struct") != -1)
                    local.Add("struct").Add(semi[semi.Contains("struct") + 1]);
                else if (semi.Contains("namespace") != -1)
                    local.Add("namespace").Add(semi[semi.Contains("namespace") + 1]);
                else
                    local.Add("control").Add("anonymous");
                doActions(local);
            }
            return false;
        }
    }


    /////////////////////////////////////////////////////////////////////////
    // pushes scope info on stack when entering new scope for second parse
    public class PushScopeInStack : AAction
    {
        Repository repo_;

        public PushScopeInStack(Repository repo)
        {
            repo_ = repo;
        }

        //----< pushes new scope information into stack >---------
        public override void doAction(CSsemi.CSemiExp semi)
        {
            Elem elem = new Elem();
            elem.typeNamespace = repo_.getCurrentNamespace();
            elem.type = semi[0];  // expects type
            elem.name = semi[1];  // expects name
            elem.begin = repo_.semi.lineCount - 1;
            elem.end = 0;
            elem.complexity = 0;
            repo_.stack.push(elem);
            if (AAction.displaySemi)
            {
                Console.Write("\n  line# {0,-5}", repo_.semi.lineCount - 1);
                Console.Write("entering ");
                string indent = new string(' ', 2 * repo_.stack.count);
                Console.Write("{0}", indent);
                this.display(semi); // defined in abstract action
            }
            if (AAction.displayStack)
                repo_.stack.display();
        }
    }


    /////////////////////////////////////////////////////////
    // detect leaving any scope. to remove info from stack
    public class DetectCloseBraceScope : ARule
    {
        //----< detects closing brace { >---------
        public override bool test(CSsemi.CSemiExp semi)
        {
            int index = semi.Contains("}");
            if (index != -1)
            {
                doActions(semi);
            }
            return false;
        }
    }


    /////////////////////////////////////////////////////////////////////
    // pops scope info from stack when leaving scope for second parse
    public class PopScopeFromStack : AAction
    {
        Repository repo_;

        public PopScopeFromStack(Repository repo)
        {
            repo_ = repo;
        }

        //----< pops the latest element from stack when closing brace is detected >---------
        public override void doAction(CSsemi.CSemiExp semi)
        {
            Elem elem;
            try
            {
                elem = repo_.stack.pop();

            }
            catch
            {
                Console.Write("popped empty stack on semiExp: ");
                semi.display();
                return;
            }
            CSsemi.CSemiExp local = new CSsemi.CSemiExp();
            local.Add(elem.type).Add(elem.name);

            if (AAction.displaySemi)
            {
                Console.Write("\n  line# {0,-5}", repo_.semi.lineCount);
                Console.Write("leaving  ");
                string indent = new string(' ', 2 * (repo_.stack.count + 1));
                Console.Write("{0}", indent);
                this.display(local); // defined in abstract action
            }
        }
    }


    //////////////////////////////////////////////////
    // configures and builds Parser
    public class BuildCodeAnalyzer
    {
        Repository repo = new Repository();

        public BuildCodeAnalyzer(CSsemi.CSemiExp semi)
        {
            repo.semi = semi;
        }
        public virtual Parser build()
        {
            Parser parser = new Parser();
            AAction.displaySemi = false;
            AAction.displayStack = false;  // this is default so redundant

            PushStack push = new PushStack(repo);
            PushDelegate pushDelegate = new PushDelegate(repo);

            // capture namespace info
            DetectNamespace detectNS = new DetectNamespace();
            detectNS.add(push);
            parser.add(detectNS);

            // capture class info
            DetectClass detectCl = new DetectClass();
            detectCl.add(push);
            parser.add(detectCl);

            // capture function info
            //DetectFunction detectFN = new DetectFunction();
            //detectFN.add(push);
            //parser.add(detectFN);

            // capture delegate info
            DetectDelegate detectDel = new DetectDelegate();
            detectDel.add(pushDelegate);
            parser.add(detectDel);

            // handle entering anonymous scopes, e.g., if, while, etc.
            DetectAnonymousScope anon = new DetectAnonymousScope();
            anon.add(push);
            parser.add(anon);

            // handle leaving scopes
            DetectLeavingScope leave = new DetectLeavingScope();
            PopStack pop = new PopStack(repo);
            leave.add(pop);
            parser.add(leave);

            // handle braceless scopes
            DetectBracelesScope detectBracelesScope = new DetectBracelesScope();
            PushBracelessScope pushBracelessScope = new PushBracelessScope(repo);
            detectBracelesScope.add(pushBracelessScope);
            parser.add(detectBracelesScope);

            return parser;
        }


        //----< buils and returns parser for finding type relationships >----------
        public virtual Parser buildRelsParser()
        {
            Parser parser = new Parser();

            // decide what to show
            AAction.displaySemi = false;
            AAction.displayStack = false;  // this is default so redundant

            // actions
            PushRelationship pushRelAction = new PushRelationship(repo);
            PushScopeInStack pushScopeInStack = new PushScopeInStack(repo);
            PopScopeFromStack popScopeFromStack = new PopScopeFromStack(repo);

            //adding detect entering scope rule to parser
            DetectOpenBraceScope detectOpenBraceScope = new DetectOpenBraceScope();
            detectOpenBraceScope.add(pushScopeInStack);
            parser.add(detectOpenBraceScope);

            //adding detect leaving scope rule to parser
            DetectCloseBraceScope detectCloseBraceScope = new DetectCloseBraceScope();
            detectCloseBraceScope.add(popScopeFromStack);
            parser.add(detectCloseBraceScope);

            //adding detect inheritance relationship rule to parser
            DetectInheritance detectInheritance = new DetectInheritance();
            detectInheritance.add(pushRelAction);
            parser.add(detectInheritance);

            //adding detect aggregation relationship rule to parser
            DetectAggregation detectAggregation = new DetectAggregation();
            detectAggregation.add(pushRelAction);
            parser.add(detectAggregation);

            //adding detect composition relationship rule to parser
            DetectComposition detectComposition = new DetectComposition();
            detectComposition.add(pushRelAction);
            parser.add(detectComposition);

            //adding detect using relationship rule to parser
            DetectUsing detectUsing = new DetectUsing();
            detectUsing.add(pushRelAction);
            parser.add(detectUsing);

            // parser configured
            return parser;
        }
    }
}

