﻿/////////////////////////////////////////////////////////////////////////
// FileMgr.cs   -  Class manages the files from the system             //
// ver 1.0                                                             //
// Language:    C#, Visual Studio 12.0, .Net Framework 4.5             //
// Platform:    Dell Inspiron , Win 7, SP 3                            //
// Application: Project Number 4 Demonstration, CSE681, Fall 2014      //
//Author : Dr.Jim Fawcett, CSE681 - Software Modeling and Analysis     //
//Modified by   : Abdulvafa Choudhary, Syracuse University             //
 //                  (315) 289-3444, aachoudh@syr.edu                   //
/////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * RulesAndActions package contains all of the Application specific
 * code required for most analysis tools.
 *
 * It defines the following Four rules which each have a
 * grammar construct detector and also a collection of IActions:
 *   - DetectNameSpace rule
 *   - DetectClass rule
 *   - DetectFunction rule
 *   - DetectScopeChange
 *   - DetectInheritance rule
 *   - DetectComposition rule
 *   - DetectAggregation rule
 *   - DetectUsing rule
 *   
 *   Three actions - some are specific to a parent rule:
 *   - Print
 *   - PrintFunction
 *   - PrintScope
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

namespace DependencyAnalyzer
{
    public class Elem  // holds scope information
    {
        public string type { get; set; }
        public string name { get; set; }
        public int begin { get; set; }
        public int end { get; set; }

        public int cc { get; set; }

        public string package { get; set; }

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

    public class Repository
    {
        ScopeStack<Elem> stack_ = new ScopeStack<Elem>();
        List<Elem> locations_ = new List<Elem>();
        static Repository instance;
        public static int complexityCount = 1;

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
            elem.type = semi[0];  // expects type
            elem.name = semi[1];  // expects name
            elem.begin = repo_.semi.lineCount - 1;
            elem.end = 0;
           
            repo_.stack.push(elem);
            if (elem.type == "control" || elem.name == "anonymous" || elem.type == "conditional")
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
                                if (repo_.locations[i].type == "function")
                                {

                                    repo_.locations[i].cc = Repository.complexityCount;
                                    Repository.complexityCount = 1;
                                }
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
            if (local[0] == "control" || local[0] == "conditional")
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
            if (index != -1)
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
            int indexCL = semi.Contains("class");
            int indexIF = semi.Contains("interface");
            int indexST = semi.Contains("struct");
            int indexEN = semi.Contains("enum");

            int index = Math.Max(indexCL, indexIF);
            index = Math.Max(index, indexST);
            index = Math.Max(index, indexEN);

            if (index != -1)
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
    // rule to dectect class definitions

    public class DetectDepClass : ARule
    {

        List<Elem> typeInfo = null;

       public  DetectDepClass(List<Elem> type)
        {
            typeInfo = type;
        }
        public override bool test(CSsemi.CSemiExp semi)
        {
            int indexCL = semi.Contains("class");
            int indexIF = semi.Contains("interface");
            int indexST = semi.Contains("struct");
            int indexEN = semi.Contains("enum");

            int index = Math.Max(indexCL, indexIF);
            index = Math.Max(index, indexST);
            index = Math.Max(index, indexEN);

            if (index != -1)
            {
                CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                // local semiExp with tokens for type and name
                local.displayNewLines = false;
                String pkg = "";
                String s=semi[index + 1];
                foreach (Elem elem in typeInfo)
                {
                    if (elem.name.Equals(semi[index + 1]))
                        pkg = elem.package;

                }
                local.Add(semi[index]).Add(pkg);
                doActions(local);
                return true;
            }
            return false;
        }
    }
    /////////////////////////////////////////////////////////
    // rule to dectect Inheritance

    public class DetectInheritance : ARule
    {
        List<Elem> typeInfo;

        public DetectInheritance(List<Elem> types)
        {
            typeInfo = types;
        }

        public bool detectType(String s)
        {
            foreach (Elem e in typeInfo)
            {
                if (s.Equals(e.name))
                {
                    return true;
                }
            }
            return false;
        }
        public override bool test(CSsemi.CSemiExp semi)
        {
            int indexC = semi.Contains("case");
            int indexD = semi.Contains("default");

            if ((indexC != -1) || (indexD != -1))
                return false;
            int index = semi.Contains(":");
            Repository repo = Repository.getInstance();

            if (index != -1)
            {
                Elem e = new Elem();
                e.type = "class";
                e.name = semi[index - 1];
                repo.locations.Add(e);
                bool a = detectType(semi[index + 1]);
                if (a)
                {

                    CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                    // create local semiExp with tokens for type and name
                    local.displayNewLines = false;

                    local.Add("inherits").Add(semi[index + 1]);
                    doActions(local);
                }

                return true;

            }
            return false;
        }
    }


    /////////////////////////////////////////////////////////
    // rule to dectect dependency based on Inheritance

    public class DetectDepInheritance : ARule
    {
        List<Elem> typeInfo;

        public DetectDepInheritance(List<Elem> types)
        {
            typeInfo = types;
        }

        public bool detectType(String s)
        {
            foreach (Elem e in typeInfo)
            {
                if (s.Equals(e.name))
                {
                    return true;
                }
            }
            return false;
        }
        public override bool test(CSsemi.CSemiExp semi)
        {
            int indexC = semi.Contains("case");
            int indexD = semi.Contains("default");

            if ((indexC != -1) || (indexD != -1))
                return false;
            int index = semi.Contains(":");
            Repository repo = Repository.getInstance();

            if (index != -1)
            {
                Elem e = new Elem();
                e.type = "class";
                String pakg="";
                foreach (Elem elem in typeInfo)
                {
                    if (elem.name == semi[index - 1])
                        pakg = elem.package;
                }
                e.name = pakg;
                repo.locations.Add(e);
                bool a = detectType(semi[index + 1]);
                if (a)
                {

                    CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                    // create local semiExp with tokens for type and name
                    local.displayNewLines = false;
                    String pkg="";
                    foreach (Elem elem in typeInfo)
                    {
                        if (elem.name.Equals(semi[index + 1]))
                            pkg = elem.package;

                    }
                    local.Add("depends").Add(pkg);
                    doActions(local);
                }

                return true;

            }
            return false;
        }
    }
    /////////////////////////////////////////////////////////
    // rule to dectect Aggregation

    public class DetectAggregation : ARule
    {

        List<Elem> typeInfo;

        public DetectAggregation(List<Elem> types)
        {
            typeInfo = types;
        }

        public bool detectType(String s)
        {
            foreach (Elem e in typeInfo)
            {
                if (s.Equals(e.name))
                {
                    return true;
                }
            }
            return false;
        }
        public override bool test(CSsemi.CSemiExp semi)
        {

            int index = semi.Contains("new");

            if (index != -1)
            {
                bool a = detectType(semi[index + 1]);
                bool b = false;
                if (semi[index + 2].Equals("."))
                b = detectType(semi[index + 3]);

                if (a||b)
                {
                    CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                    // create local semiExp with tokens for type and name
                    local.displayNewLines = false;
                    if (semi[index + 2].Equals("."))
                    {
                        local.Add("aggregates").Add(semi[index + 3]);

                    }
                    else
                    local.Add("aggregates").Add(semi[index + 1]);
                    doActions(local);
                    return true;
                }
            }
            return false;
        }
    }

    /////////////////////////////////////////////////////////
    // rule to dectect Dependency based on Aggregation

    public class DetectDepAggregation : ARule
    {

        List<Elem> typeInfo;

        public DetectDepAggregation(List<Elem> types)
        {
            typeInfo = types;
        }

        public bool detectType(String s)
        {
            foreach (Elem e in typeInfo)
            {
                if (s.Equals(e.name))
                {
                    return true;
                }
            }
            return false;
        }
        public override bool test(CSsemi.CSemiExp semi)
        {

            int index = semi.Contains("new");

            if (index != -1)
            {
                bool a = detectType(semi[index + 1]);
                bool b = false;
                if (semi[index + 2].Equals("."))
                    b = detectType(semi[index + 3]);
                if (a || b)
                {
                    CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                    // create local semiExp with tokens for type and name
                    local.displayNewLines = false;
                   
                    if (semi[index + 2].Equals("."))
                    {
                        String pkg = "";
                        foreach (Elem elem in typeInfo)
                        {
                            if (elem.name.Equals(semi[index + 3]))
                                pkg = elem.package;

                        }
                        local.Add("depends").Add(pkg);

                    }
                    else
                    {
                        String pkg = "";
                        foreach (Elem elem in typeInfo)
                        {
                            if (elem.name.Equals(semi[index +1]))
                                pkg = elem.package;

                        }
                        local.Add("depends").Add(pkg);
                    }
                        doActions(local);
                    return true;
                }
            }
            return false;
        }
    }
    /////////////////////////////////////////////////////////
    // rule to dectect Using

    public class DetectUsing : ARule
    {
        List<Elem> typeInfo;

        public DetectUsing(List<Elem> types)
        {
            typeInfo = types;
        }

        public static bool isSpecialToken(string token)
        {
            string[] SpecialToken = { "if", "for", "foreach", "while", "catch", "using" };
            foreach (string stoken in SpecialToken)
                if (stoken == token)
                    return true;
            return false;
        }

        public void store(CSsemi.CSemiExp args)
        {
            Repository rep = Repository.getInstance();

            for (int i = 0; i < args.count; i++)
            {
                Elem e = new Elem();
                e.type = "uses";
                e.name = args[i];
                rep.locations.Add(e);
            }
        }

        public override bool test(CSsemi.CSemiExp semi)
        {

            if (semi[semi.count - 1] != "{")
                return false;
            int count = 0;
            int index = semi.FindFirst("(");
            if (index > 0 && !isSpecialToken(semi[index - 1]))
            {
                int indexC = semi.FindFirst(")");
                CSsemi.CSemiExp args = new CSsemi.CSemiExp();

                for (int i = index; i <= indexC; i++)
                {

                    foreach (Elem e in typeInfo)
                    {
                        if (e.name == semi[i])
                        {
                            args.Add(e.name);
                            count++;
                        }
                    }
                }

                if (count == 0)
                    return false;

                store(args);

                return true;
            }
            return false;
        }
    }

    /////////////////////////////////////////////////////////
    // rule to dectect Dependency based on Using

    public class DetectDepUsing : ARule
    {
        List<Elem> typeInfo;

        public DetectDepUsing(List<Elem> types)
        {
            typeInfo = types;
        }

        public static bool isSpecialToken(string token)
        {
            string[] SpecialToken = { "if", "for", "foreach", "while", "catch", "using" };
            foreach (string stoken in SpecialToken)
                if (stoken == token)
                    return true;
            return false;
        }

        public void store(List<Elem> args)
        {
            Repository rep = Repository.getInstance();

            for (int i = 0; i < args.Count; i++)
            {
                Elem e = new Elem();
                e.type = "depends";
                e.name = args[i].package;
                rep.locations.Add(e);
            }
        }

        public override bool test(CSsemi.CSemiExp semi)
        {

            if (semi[semi.count - 1] != "{")
                return false;
            int count = 0;
            int index = semi.FindFirst("(");
            if (index > 0 && !isSpecialToken(semi[index - 1]))
            {
                int indexC = semi.FindFirst(")");
                List<Elem> args = new List<Elem>();

                for (int i = index; i <= indexC; i++)
                {

                    foreach (Elem e in typeInfo)
                    {
                        if (e.name == semi[i])
                        {
                            args.Add(e);
                            count++;
                        }
                    }
                }

                if (count == 0)
                    return false;

                store(args);

                return true;
            }
            return false;
        }
    }
    /////////////////////////////////////////////////////////
    // rule to dectect Composition

    public class DetectComposition : ARule
    {
        List<Elem> typeInfo;

        public DetectComposition(List<Elem> types)
        {
            typeInfo = types;
        }

        public override bool test(CSsemi.CSemiExp semi)
        {

            int index = -1;
            foreach (Elem e in typeInfo)
            {
                if (e.type != "class")
                {
                    index = semi.Contains(e.name);
                    if (index != -1)
                        break;
                }
            }

            if (index != -1)
            {
                CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                // create local semiExp with tokens for type and name
                local.displayNewLines = false;
                local.Add("composes").Add(semi[index]);
                doActions(local);
                return true;
            }
            return false;
        }
    }


    /////////////////////////////////////////////////////////
    // rule to dectect Dependency Composition

    public class DetectDepComposition : ARule
    {
        List<Elem> typeInfo;

        public DetectDepComposition(List<Elem> types)
        {
            typeInfo = types;
        }

        public override bool test(CSsemi.CSemiExp semi)
        {

            int index = -1;
            foreach (Elem e in typeInfo)
            {
                if (e.type != "class")
                {
                    index = semi.Contains(e.name);
                    if (index != -1)
                        break;
                }
            }

            if (index != -1)
            {
                CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                // create local semiExp with tokens for type and name
                local.displayNewLines = false;
                String pkg = "";
                foreach (Elem elem in typeInfo)
                {
                    if (elem.name.Equals(semi[index]))
                        pkg = elem.package;

                }
                local.Add("depends").Add(pkg);
                doActions(local);
                return true;
            }
            return false;
        }
    }
    /////////////////////////////////////////////////////////
    // rule to dectect function definitions

    public class DetectFunction : ARule
    {
        public static bool isSpecialToken(string token)
        {
            string[] SpecialToken = { "if", "for", "foreach", "while", "catch", "using" };
            foreach (string stoken in SpecialToken)
                if (stoken == token)
                    return true;
            return false;
        }
        public override bool test(CSsemi.CSemiExp semi)
        {
            if (semi[semi.count - 1] != "{")
                return false;

            int index = semi.FindFirst("(");
            if (index > 0 && !isSpecialToken(semi[index - 1]))
            {

                CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                local.Add("function").Add(semi[index - 1]);
                doActions(local);
                return true;
            }
            return false;
        }
    }
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
                Repository.complexityCount++;
                local.Add("control").Add("anonymous");
                doActions(local);
                return true;
            }
            return false;
        }
    }
    /////////////////////////////////////////////////////////
    // detect bracless scope

    public class DetectScope : ARule
    {
        public static bool isSpecialToken(CSsemi.CSemiExp token)
        {
            string[] SpecialToken = { "if", "for", "foreach", "while","else if","else","break", "continue"};
            foreach (string stoken in SpecialToken)
            {

                int i = token.Contains(stoken);

                if(i != -1)
                    return true;
            }
                return false;
        }
        public override bool test(CSsemi.CSemiExp semi)
        {


            if (isSpecialToken(semi))
            {
                CSsemi.CSemiExp local = new CSsemi.CSemiExp();
                // create local semiExp with tokens for type and name
                local.displayNewLines = false;
                local.Add("conditional").Add("scope");
                Repository.complexityCount++;
               //doActions(local);
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

    public class BuildCodeAnalyzer
    {
        Repository repo = new Repository();

        public BuildCodeAnalyzer(CSsemi.CSemiExp semi)
        {
            repo.semi = semi;
        }
        /////////////////////////////////////////////////////////
        // Parser for detecting types and function complexity

        public virtual Parser build()
        {
            Parser parser = new Parser();

            // decide what to show
            AAction.displaySemi = false;
            AAction.displayStack = false;  // this is default so redundant

            // action used for namespaces, classes, and functions
            PushStack push = new PushStack(repo);

            // capture namespace info
            DetectNamespace detectNS = new DetectNamespace();
            detectNS.add(push);
            parser.add(detectNS);

            // capture class info
            DetectClass detectCl = new DetectClass();
            detectCl.add(push);
            parser.add(detectCl);


            // capture function info
            DetectFunction detectFN = new DetectFunction();
            detectFN.add(push);
            parser.add(detectFN);

            //handle entering anonymous scopes, e.g., if, while, etc.
            DetectAnonymousScope anon = new DetectAnonymousScope();
            anon.add(push);
            parser.add(anon);

            // capture Scope info
            DetectScope detectSc = new DetectScope();
            detectSc.add(push);
            parser.add(detectSc);

            // handle leaving scopes
            DetectLeavingScope leave = new DetectLeavingScope();
            PopStack pop = new PopStack(repo);
            leave.add(pop);
            parser.add(leave);

            // parser configured
            return parser;
        }
        /////////////////////////////////////////////////////////
        // detect relationship between types

        public virtual Parser build(List<Elem> types)
        {
            Parser parser = new Parser();

            // decide what to show
            AAction.displaySemi = false;
            AAction.displayStack = false;  // this is default so redundant

            // action used for namespaces, classes, and functions
            PushStack push = new PushStack(repo);

            //capture inheritance info
            DetectInheritance detectInh = new DetectInheritance(types);
            detectInh.add(push);
            parser.add(detectInh);

            // capture class info
            DetectClass detectCl = new DetectClass();
            detectCl.add(push);
            parser.add(detectCl);

            //capture commposition info
            DetectComposition detectComp = new DetectComposition(types);
            detectComp.add(push);
            parser.add(detectComp);

            //capture aggregation info
            DetectAggregation detectAggr = new DetectAggregation(types);
            detectAggr.add(push);
            parser.add(detectAggr);

            //capture using info
            DetectUsing detectusing = new DetectUsing(types);
            detectusing.add(push);
            parser.add(detectusing);

            // parser configured
            return parser;
        }

        public virtual Parser buildDependency(List<Elem> types)
        {
            Parser parser = new Parser();

            // decide what to show
            AAction.displaySemi = false;
            AAction.displayStack = false;  // this is default so redundant

            // action used for namespaces, classes, and functions
            PushStack push = new PushStack(repo);

            //capture inheritance info
            DetectDepInheritance detectInh = new DetectDepInheritance(types);
            detectInh.add(push);
            parser.add(detectInh);

            // capture class info
            DetectDepClass detectCl = new DetectDepClass(types);
            detectCl.add(push);
            parser.add(detectCl);

            //capture commposition info
            DetectDepComposition detectComp = new DetectDepComposition(types);
            detectComp.add(push);
            parser.add(detectComp);

            //capture aggregation info
            DetectDepAggregation detectAggr = new DetectDepAggregation(types);
            detectAggr.add(push);
            parser.add(detectAggr);

            //capture using info
            DetectDepUsing detectusing = new DetectDepUsing(types);
            detectusing.add(push);
            parser.add(detectusing);

            // parser configured
            return parser;
        }
       
    }
}

