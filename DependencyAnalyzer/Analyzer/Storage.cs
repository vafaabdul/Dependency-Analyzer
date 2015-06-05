/**************************************************************************************************
 *FileName      : Analyzer.cs
 *Version       : 1.0
 *Langage       : C#, .Net Framework 4.5
 *Platform      : Dell Inspiron , Win 7, SP 3
 *Application   : Project Number 4 Demonstration, CSE681, Fall 2014
 *Author        : Abdulvafa Choudhary, Syracuse University
 *                (315) 289-3444, aachoudh@syr.edu
 ***************************************************************************************************/
/*
 *  Module Operations:
 *  -----------------
 * This package processes the file one after the other and calls the class
 * Parser to find relationship between types and package dependencies.
 * It also calls the store package to to store the results.
 *    
 *  Required Files:
 *  None
 *
 *  Build Command:  devenv DependencyAnalyzer.sln /rebuild debug
 *  
 *  Maintenace History:
 *  ver 1.0 : Nov 19, 2014
 *  - first release
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DependencyAnalyzer
{
    class Storage
    {


        public void storeRel(List<Elem> table, Object file,Dictionary<String,HashSet<String>> reln)
        {

            String className = null;
            HashSet<String> relnSet = null;
            foreach (Elem e in table)
            {
                if (e.type == "class" || e.type == "interface" || e.type == "struct")
                {
                    if (className != null)
                    {
                        reln.Add(className, relnSet);
                    }
                    relnSet = new HashSet<string>();
                    className = e.type+ " " +e.name;
                }
                else if (!(e.type == "struct" || e.type == "enum" || e.type == "interface"))
                {
                    String relation = e.type + " " + e.name;
                    relnSet.Add(relation);
                }
            }

            reln.Add(className, relnSet);

        }

        /////////////////////////////////////////////////////////
        // This function displays the results of relationship 
        // between types analysis

        public void storeDep(List<Elem> table, Object file, Dictionary<String, HashSet<String>> depAnal)
        {

            String parent = Path.GetFileName(file.ToString());

            HashSet<String> dependency = new HashSet<string>();
            foreach (Elem e in table)
            {

                if ((e.type == "depends"))
                {
                    dependency.Add(e.name);
                }
            }
            depAnal.Add(parent, dependency);
        }

#if(TEST_STORAGE)
        static void Main(string[] args)
        {
            Console.Write("\n  Testing Stroage Class");
            Console.Write("\n =======================\n");
            Storage store = new Storage();
            List<Elem> table=new List<Elem>;
            Object file="D:/Analyze.cs";
            Dictionary<String, HashSet<String>> depAnal=new ictionary<String, HashSet<String>>();
            store.storeRel(table,file,depAnal);
            store.storeDep(table,file,depAnal);
            Console.Write("\n\n");
            
        }
#endif
    }
}
