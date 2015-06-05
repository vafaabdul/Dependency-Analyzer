/**************************************************************************************************
 *FileName      : Analyzer.cs
 *Version       : 2.0
 *Langage       : C#, .Net Framework 4.5
 *Platform      : Dell Inspiron , Win 7, SP 3
 *Application   : Project Number 4 Demonstration, CSE681, Fall 2014
 *Author        : Dr.Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2014
 *Modified by   : Abdulvafa Choudhary, Syracuse University
 *                  (315) 289-3444, aachoudh@syr.edu
 ***************************************************************************************************/
/*
 *  Module Operations:
 *  -----------------
 * This package processes the file one after the other and calls the class
 * Parser to find relationship between types and package dependencies.
 * It also calls the store package to to store the results.
 *    
 *  Required Files:
 *  Parser.cs Storage.cs
 *
 *  Build Command:  devenv DependencyAnalyzer.sln /rebuild debug
 *  
 *  Maintenace History:
 *  ver 2.0 : Nov 19, 2014
 *  - added required functionalities wrt to Project #04
 *  ver 1.0 : Oct 29, 2014
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
    public class Analyzer
    {

        /////////////////////////////////////////////////////////
        // This function gets the list of files matching the 
        //pattern given by the user

        public string[] getFiles(string path, List<string> patterns, bool recur)
        {
            FileMgr fm = new FileMgr();
            fm.isRecurse(recur);
            foreach (string pattern in patterns)
                fm.addPattern(pattern);
            fm.findFiles(path);
            return fm.getFiles().ToArray();
        }

        /////////////////////////////////////////////////////////
        // This method detcet types defined in the files and
        // stores them for finding relationship between files

        public List<Elem> findTypes(string[] files)
        {

            List<Elem> typesTable = new List<Elem>();
            foreach (object file in files)
            {
                String[] pac = file.ToString().Split('\\');
                int len = pac.Length;
                String p = pac[len - 2] + "." + pac[len - 1];
                String package = Path.GetFileName(file.ToString());

                CSsemi.CSemiExp semi = new CSsemi.CSemiExp();
                semi.displayNewLines = false;
                if (!semi.open(file as string))
                {
                    Console.Write("\n  Can't open {0}\n\n", file);
                    return null;
                }

                BuildCodeAnalyzer builder = new BuildCodeAnalyzer(semi);
                Parser parser = builder.build();
                try
                {
                    while (semi.getSemi())
                        parser.parse(semi);
                }
                catch (Exception ex)
                {
                    Console.Write("\n\n  {0}\n", ex.Message);
                }
                Repository rep = Repository.getInstance();
                List<Elem> table = rep.locations;

                foreach (Elem e in table)
                {
                    if (e.type == "class" || e.type == "struct" || e.type == "enum" || e.type == "interface")
                    {
                        Elem typeElement = new Elem();
                        typeElement.type = e.type;
                        typeElement.name = e.name;
                        typeElement.package = p;
                        typesTable.Add(typeElement);

                    }
                }
                semi.close();
            }

            return typesTable;
        }

        /////////////////////////////////////////////////////////
        // This method find reltionship between types
        public void findRelationShip(string[] files, List<Elem> typesList,Dictionary<String,HashSet<String>> relAnal)
        {
            Storage disp = new Storage();
            List<Elem> typesTable = new List<Elem>();

            foreach (object file in files)
            {

                CSsemi.CSemiExp semi = new CSsemi.CSemiExp();
                semi.displayNewLines = false;
                if (!semi.open(file as string))
                {
                    Console.Write("\n  Can't open {0}\n\n", file);
                    return;
                }

                BuildCodeAnalyzer builder = new BuildCodeAnalyzer(semi);
                Parser parser = builder.build(typesList);
                try
                {
                    while (semi.getSemi())
                        parser.parse(semi);
                }
                catch (Exception ex)
                {
                    Console.Write("\n\n  {0}\n", ex.Message);
                }
                Repository rep = Repository.getInstance();
                List<Elem> tableRel = rep.locations;

                foreach (Elem e in tableRel)
                {
                    if (!(e.type == "struct" || e.type == "enum"))
                    {
                        Elem typeElement = new Elem();
                        typeElement.type = e.type;
                        typeElement.name = e.name;
                        typesTable.Add(typeElement);

                    }
                }
                disp.storeRel(tableRel, file,relAnal);


                semi.close();
            }

        }

        //This method is uesd to determine the dependencies betweenth packages
        public void findDependency(string[] files, List<Elem> typesList, Dictionary<String, HashSet<String>> depAnal)

        {

            Storage disp = new Storage();
            List<Elem> typesTable = new List<Elem>();

            foreach (object file in files)
            {


                CSsemi.CSemiExp semi = new CSsemi.CSemiExp();
                semi.displayNewLines = false;
                if (!semi.open(file as string))
                {
                    Console.Write("\n  Can't open {0}\n\n", file);
                    return;
                }

                BuildCodeAnalyzer builder = new BuildCodeAnalyzer(semi);
                Parser parser = builder.buildDependency(typesList);
                try
                {
                    while (semi.getSemi())
                        parser.parse(semi);
                }
                catch (Exception ex)
                {
                    Console.Write("\n\n  {0}\n", ex.Message);
                }
                Repository rep = Repository.getInstance();
                List<Elem> tableRel = rep.locations;

                disp.storeDep(tableRel, file, depAnal);


                semi.close();
            }
        }


#if(TEST_ANALYZER)
        static void Main(string[] args)
        {
            Console.Write("\n  Testing Analyzer Class");
            Console.Write("\n =======================\n");
            Analyzer analyzer = new Analyzer();
            analyzer.startProcessing(args);
            Console.Write("\n\n");
            
        }
#endif
    }



}
