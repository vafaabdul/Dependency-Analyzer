/**************************************************************************************************
 *FileName      : ServiceLibrary.cs - interface for Dependency Analyzer communication service
 *Version       : 1.0
 *Langage       : C#, .Net Framework 4.5
 *Platform      : Dell Inspiron , Win 7, SP 3
 *Application   : Project Number 4 Demonstration, CSE681, Fall 2014
 *Author        : Dr.Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2014
 ***************************************************************************************************/
/*
 *  Module Operations:
 *  -----------------
 *  This package defines the service contract, IMessageService, for messaging and 
 *  defines the class SvcMsg as a data contract.
 *    
 *  Required Files:
 *  None
 *
 *  Build Command:  devenv DependencyAnalyzer.sln /rebuild debug
 *  
 *  Maintenace History:
 *  ver 1.0 : Nov 15, 2014
 *  - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace DependencyAnalyzer
{
    [DataContract]
    public class SvcMsg
    {
        public enum Command { Projects,TypeDependency,PackageDependency,TypeTable,TypeResults,Quit}; 
        [DataMember]
        public Command cmd { get; set; }
        [DataMember]
        public Uri src { get; set; }
        [DataMember]
        public Uri dst { get; set; }
        [DataMember]
        public string body { get; set; }  // Used to send XML for structured data

        [DataMember]
        public List<Elem> types { get; set; }//Ued to send the list of types

        [DataMember]
        public List<PackageStruct> packageDep { get; set; } //used to send the package dependencies

        [DataMember]
        public List<RelStruct> typeDep { get; set; }//used to send the type dependencies

        [DataMember]
        public List<String> projectList { get; set; } //used to send the list of projects

    }
    [ServiceContract]
    public interface IMessageService
    {
        [OperationContract(IsOneWay = true)]
        void PostMessage(SvcMsg msg);
    }

}
