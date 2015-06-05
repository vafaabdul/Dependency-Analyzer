/**************************************************************************************************
 *FileName      : Server.cs - Dependency Analyzer Server 1
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
 *    Provides prototype behavior for the Dependency Analyzer server.
 *    Defines prototype behavior for processing received messages
 *    
 *  Required Files:
 *  BlockingQueue.cs  ServiceLibrary.cs Analyzer.cs
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
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.ServiceModel;
using System.Xml.Serialization;

namespace DependencyAnalyzer
{
    ///////////////////////////////////////////////////
    // client of another Peer's Communication service

    public class Sender
    {
        IMessageService channel;
        string lastError = "";
        BlockingQueue<SvcMsg> sndBlockingQ = null;
        Thread sndThrd = null;
        int tryCount = 0, MaxCount = 10;

        // Processing for sndThrd to pull msgs out of sndBlockingQ
        // and post them to another Peer's Communication service

        void ThreadProc()
        {
            while (true)
            {
                SvcMsg msg = sndBlockingQ.deQ();
                channel.PostMessage(msg);
                if (msg.cmd == SvcMsg.Command.Quit)
                    break;
            }
        }

        // Create Communication channel proxy, sndBlockingQ, and
        // start sndThrd to send messages that client enqueues

        public Sender(string url)
        {
            sndBlockingQ = new BlockingQueue<SvcMsg>();
            while (true)
            {
                try
                {
                    CreateSendChannel(url);
                    tryCount = 0;
                    break;
                }
                catch (Exception ex)
                {
                    if (++tryCount < MaxCount)
                        Thread.Sleep(100);
                    else
                    {
                        lastError = ex.Message;
                        break;
                    }
                }
            }
            sndThrd = new Thread(ThreadProc);
            sndThrd.IsBackground = true;
            sndThrd.Start();
        }

        // Create proxy to another Peer's Communicator
        public void CreateSendChannel(string address)
        {
            EndpointAddress baseAddress = new EndpointAddress(address);
            BasicHttpBinding binding = new BasicHttpBinding();
            ChannelFactory<IMessageService> factory
              = new ChannelFactory<IMessageService>(binding, address);
            channel = factory.CreateChannel();
        }

        // Sender posts message to another Peer's queue using
        // Communication service hosted by receipient via sndThrd

        public void PostMessage(SvcMsg msg)
        {
            sndBlockingQ.enQ(msg);
        }

        public string GetLastError()
        {
            string temp = lastError;
            lastError = "";
            return temp;
        }

        public void Close()
        {
            ChannelFactory<IMessageService> temp = (ChannelFactory<IMessageService>)channel;
            temp.Close();
        }

    }


    // PerSession activation creates an instance of the service for each
    // client.  That instance lives for a pre-determined lease time.  
    // - If the creating client calls back within the lease time, then
    //   the lease is renewed and the object stays alive.  Otherwise it
    //   is invalidated for garbage collection.
    // - This behavior is a reasonable compromise between the resources
    //   spent to create new objects and the memory allocated to persistant
    //   objects.
    // 
    public class Receiver : IMessageService
    {
        static BlockingQueue<SvcMsg> rcvBlockingQ = null;
        ServiceHost service = null;

        public void Close()
        {
            service.Close();
        }


        //  Create Host for Communication service
        public void CreateRecvChannel(string address)
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            Uri baseAddress = new Uri(address);
            service = new ServiceHost(typeof(Receiver), baseAddress);
            service.AddServiceEndpoint(typeof(IMessageService), binding, baseAddress);
            service.Open();
        }

        // Implement service method to receive messages from other Peers
        public Receiver()
        {
            if (null == rcvBlockingQ)
            {
                rcvBlockingQ = new BlockingQueue<SvcMsg>();
            }
        }


        //sotres the incoming message in the blocking queue
        public void PostMessage(SvcMsg msg)
        {
            rcvBlockingQ.enQ(msg);
        }

        public SvcMsg GetMessage()
        {
            return rcvBlockingQ.deQ();
        }

        //changes the encoding of the string writer class 
        public class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }
        }


        //serialize the the object to be send

        public static string ConvertToXml(object toSerialize)
        {
            string temp;
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add(string.Empty, string.Empty);
            var serializer = new XmlSerializer(toSerialize.GetType());
            using (StringWriter writer = new Utf8StringWriter())
            {
                serializer.Serialize(writer, toSerialize, ns);
                temp = writer.ToString();
            }
            return temp;
        }

        //create the list of package dependencies
        public List<PackageStruct> buildPkgResp( Dictionary<String,HashSet<String>> depAnal,String projectName,List<PackageStruct> packList)
        {
            foreach (var dep in depAnal)
            {
                foreach(var val in dep.Value)
                  {
                    PackageStruct p=new PackageStruct();
                    p.projectName=projectName;
                    p.packageFrom=dep.Key;
                    p.packageTo=val;
                    packList.Add(p);

                }
            }

            return packList;
        }


        //create the list of types 
        public List<RelStruct> buildTypeResp(Dictionary<String, HashSet<String>> depAnal, String projectName, List<RelStruct> relList)
        {

            foreach (var dep in depAnal)
            {
                foreach(var val in dep.Value)
                  {
                      RelStruct r = new RelStruct();
                       r.projectName=projectName;
                       r.typeName = dep.Key;
                       r.reltnName = val;
                       relList.Add(r);

                }
            }

            return relList;
        }

        //build the table of types present on the server
        public List<Elem> buildTypeTable()
        {

            List<Elem> myTypeTable = new List<Elem>();
            Analyzer myAnal = new Analyzer();
            List<string> myPatterns = new List<string>();

            myPatterns.Add("*.cs");

            string[] myFiles = myAnal.getFiles("../../../../TestProjectsServer0", myPatterns, true);
            myTypeTable = myAnal.findTypes(myFiles);
         
            return myTypeTable;
        }


        //send the message to server requesting to upate the type table
        public void sendMsgForUpdate()
        {
            Sender server = new Sender("http://localhost:8081/MyDependencyAnalyzer");
            SvcMsg msg1 = new SvcMsg();
            msg1.src = new Uri("http://localhost:8080/MyDependencyAnalyzer");
            msg1.dst = new Uri("http://localhost:8081/MyDependencyAnalyzer");
            msg1.cmd = SvcMsg.Command.TypeTable;
            server.PostMessage(msg1);
        }


        //send the list of projects to the requesting client
        public void sendProjectList(SvcMsg msg)
        {
            string[] myProjects = Directory.GetDirectories("../../../../TestProjectsServer0", "*");

            List<String> myProjectList = new List<string>();

            String project = "";
            for (int i = 0; i < myProjects.Length; i++)
            {
                project = Path.GetFileName(myProjects[i].ToString());
                myProjectList.Add(project);
            }

            Sender client = new Sender(msg.src.ToString());
            SvcMsg msg1 = new SvcMsg();
            msg1.src = new Uri("http://localhost:8080/MyDependencyAnalyzer");
            msg1.dst = new Uri(msg.src.ToString());
            msg1.cmd = SvcMsg.Command.Projects;
            msg1.body = ConvertToXml(myProjectList);
            msg1.projectList = myProjectList;
            client.PostMessage(msg1);
        }

        //send the package dependencies to the requesting client
        public void sendPkgDependency(SvcMsg msg,List<Elem> myTypeTable)
        {
            string[] myProjects = Directory.GetDirectories("../../../../TestProjectsServer0", "*");

            List<String> myProjectList = new List<string>();

            String project = "";
            for (int i = 0; i < myProjects.Length; i++)
            {
                project = Path.GetFileName(myProjects[i].ToString());
                myProjectList.Add(project);
            }

            List<String> projectList = new List<String>();
            foreach (var p in msg.projectList)
            {
                if (myProjectList.Contains(p))
                {
                    projectList.Add(p);
                    Console.WriteLine(p);
                }
            }
            Console.WriteLine("in Type Dependency  {0}", myTypeTable.Count);
            Analyzer anal = new Analyzer();
            List<string> patterns = new List<string>();
            patterns.Add("*.cs");
            Dictionary<String, HashSet<String>> depAnal = new Dictionary<string, HashSet<string>>();
            List<PackageStruct> pkgList = new List<PackageStruct>();

            for (int i = 0; i < projectList.Count; i++)
            {
                string[] files = anal.getFiles("../../../../TestProjectsServer0/" + projectList[i], patterns, false);
                anal.findDependency(files, myTypeTable, depAnal);
                buildPkgResp(depAnal, projectList[i], pkgList);

            }

            Sender server = new Sender(msg.src.ToString());
            SvcMsg msg1 = new SvcMsg();
            msg1.src = new Uri("http://localhost:8080/MyDependencyAnalyzer");
            msg1.dst = new Uri(msg.src.ToString());
            msg1.cmd = SvcMsg.Command.PackageDependency;
            msg1.body = ConvertToXml(pkgList);
            msg1.packageDep = pkgList;
            server.PostMessage(msg1);
        }

        //send the type dependencies to the requesting client
        public void sendTypeDependency(SvcMsg msg,List<Elem> myTypeTable)
        {
            string[] myProjects = Directory.GetDirectories("../../../../TestProjectsServer0", "*");

            List<String> myProjectList = new List<string>();

            String project = "";
            for (int i = 0; i < myProjects.Length; i++)
            {
                project = Path.GetFileName(myProjects[i].ToString());
                myProjectList.Add(project);
            }

            List<String> projectList = new List<String>();
            foreach (var p in msg.projectList)
            {
                if (myProjectList.Contains(p))
                {
                    projectList.Add(p);
                    Console.WriteLine(p);
                }
            }
            Analyzer anal = new Analyzer();
            List<string> patterns = new List<string>();
            List<RelStruct> relList = new List<RelStruct>();
            patterns.Add("*.cs");
            Dictionary<String, HashSet<String>> depAnal = new Dictionary<string, HashSet<string>>();
            for (int i = 0; i < projectList.Count; i++)
            {
                string[] files = anal.getFiles("../../../../TestProjectsServer0/" + projectList[i], patterns, false);
                anal.findRelationShip(files, myTypeTable, depAnal);
                buildTypeResp(depAnal, projectList[i], relList);

            }

            Sender server = new Sender(msg.src.ToString());
            SvcMsg msg1 = new SvcMsg();
            msg1.src = new Uri("http://localhost:8080/MyDependencyAnalyzer");
            msg1.dst = new Uri(msg.src.ToString());
            msg1.cmd = SvcMsg.Command.TypeDependency;
            msg1.body = ConvertToXml(relList);
            msg1.typeDep = relList;
            server.PostMessage(msg1);
        }


        //send the type table to the requesting server
        public void sendTypeTable(SvcMsg msg)
        {
            Analyzer anal = new Analyzer();
            List<string> patterns = new List<string>();
            List<Elem> typesTable = new List<Elem>();

            patterns.Add("*.cs");

            string[] files = anal.getFiles("../../../../TestProjectsServer0", patterns, true);
            typesTable = anal.findTypes(files);

            Sender server = new Sender(msg.src.ToString());
            SvcMsg msg1 = new SvcMsg();
            msg1.src = new Uri("http://localhost:8081/MyDependencyAnalyzer");
            msg1.dst = new Uri(msg.src.ToString());
            msg1.cmd = SvcMsg.Command.TypeResults;
            msg1.types = typesTable;
            server.PostMessage(msg1);
        }


        //process the messgaes from the clients and serveres
        public void HandleRequest()
        {
           List<Elem> myTypeTable=buildTypeTable() ;
        
            while (true)
            {
                SvcMsg msg=GetMessage();
          
                if(msg.cmd==SvcMsg.Command.Projects)
                {
                    sendMsgForUpdate();
                    sendProjectList(msg);
                }

                if (msg.cmd == SvcMsg.Command.TypeTable)
                {
                    sendTypeTable(msg);
                }
                if (msg.cmd == SvcMsg.Command.TypeResults)
                {
                    for (int i = 0; i < msg.types.Count; i++)
                        myTypeTable.Add(msg.types[i]);
                }
                if (msg.cmd == SvcMsg.Command.TypeDependency)
                {
                    sendTypeDependency(msg, myTypeTable);
                }

                if (msg.cmd == SvcMsg.Command.PackageDependency)
                {
                    sendPkgDependency(msg, myTypeTable);
                }

                if (msg.cmd == SvcMsg.Command.Quit)
                {
                    break;
                }

                }

        }

    }


    //starts the server 
    public class Server0
    {
        public static string ConvertToXml(object toSerialize)
        {
            string temp;
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add(string.Empty, string.Empty);
            var serializer = new XmlSerializer(toSerialize.GetType());
            using (StringWriter writer = new StringWriter())
            {
                serializer.Serialize(writer, toSerialize, ns);
                temp = writer.ToString();
            }
            return temp;
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("Starting Message Service on Server0");
            Receiver receive = null;
            try
            {
                receive = new Receiver();
                receive.CreateRecvChannel("http://localhost:8080/MyDependencyAnalyzer");

                receive.HandleRequest();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.GetType());
            }

            Console.WriteLine("\n  press key to terminate service");
            Console.ReadKey();
            Console.Write("\n");
            receive.Close();
        }


#if(TEST_SERVER0)
      static void Main (string[] args)
      {
               Receiver receive = new Receiver();
               receive.CreateRecvChannel("http://localhost:8081/MyDependencyAnalyzer");
                receive.HandleRequest();
      }
#endif
    }

}
