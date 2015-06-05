/**************************************************************************************************
 *FileName      : PackageStruct.cs
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
 *  This package defines the structure to store the type and 
 *  package dependencies.
 *    
 *  Required Files:
 *  None
 *
 *  Build Command:  devenv DependencyAnalyzer.sln /rebuild debug
 *  
 *  Maintenace History:
 *  ver 1.0 : Nov 18, 2014
 *  - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependencyAnalyzer
{
    //structure to store the the package dependency info
    public class PackageStruct
    {
        public String projectName { get; set; }
        public String packageFrom { get; set; }
        public String packageTo  {get; set;}
    }

    //structure to store the the type dependency info
    public class RelStruct
    {
        public String projectName { get; set; }
        public String typeName { get; set; }
        public String reltnName { get; set; }
    }
}
