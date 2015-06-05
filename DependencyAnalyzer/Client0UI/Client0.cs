/**************************************************************************************************
 *FileName      : Client.cs - Dependency Analyzer Client 1
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
 *    Defines the behavior of a Dependency Analyzer client
 *    Defines prototype behavior for processing client received messages
 * Required Files:
 *  BlockingQueue.cs  ServiceLibrary.cs
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
using System.ServiceModel;
using System.Threading;
using System.Xml.Linq;
using DependencyAnalyzer;

namespace Client0UI
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
   //close the connection
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

        //stores the incoming message into the queue
        public void PostMessage(SvcMsg msg)
        {
            rcvBlockingQ.enQ(msg);
        }

        //process the received messages
        public void processMessages()
        {
            Client0 c = Client0.getInstance();
            while (true)
            {
                SvcMsg msg = GetMessage();

                    if(msg.cmd==SvcMsg.Command.Projects)
                    {
                        c.displayProjects(msg.body);
                        
                    }
                    if(msg.cmd==SvcMsg.Command.TypeDependency)
                    {
                         c.displayTypeRel(msg.typeDep,msg.body);

                    }
                   if(msg.cmd==SvcMsg.Command.PackageDependency)
                   {
                       c.displayPkgDep(msg.packageDep,msg.body);

                   }
                
              
            }
        }


        // Implement service method to extract messages from other Peers.
        // This will often block on empty queue, so user should provide
        // read thread.
        public SvcMsg GetMessage()
        {
            return rcvBlockingQ.deQ();
        }
    }

    public class Client0
    {
        static Client0 instance;
        Sender server0 = null;
        Sender server1 = null;
        Receiver receive;

        public Client0()
        {
            instance = this;
            server0 = new Sender("http://localhost:8080/MyDependencyAnalyzer");
            server1 = new Sender("http://localhost:8081/MyDependencyAnalyzer");
        }

        //creating a static instance of client
        public static Client0 getInstance()
        {
            return instance;
        }


        //starts the client at the given port
        public void startClient()
        {
          
            try
            {
                receive = new Receiver();
                receive.CreateRecvChannel("http://localhost:8082/MyDependencyAnalyzer");
                Thread t = new Thread(receive.processMessages);
                t.Start();
                Thread.Sleep(5000);
            }
            catch (Exception ex)
            {
                Console.Write("\n\n  {0}\n\n", ex.Message);
                return;
            }
        }


        //displays the project names on the UI
        internal void displayProjects(String projectXML)
        {
            try
            {
                List<String> projectsList = new List<string>();

                //writing output to the xml file and reading it from the xml using linq
               
               
                System.IO.File.WriteAllText(@"..\..\WriteText.xml", projectXML);
                XDocument doc = XDocument.Load(@"..\..\WriteText.xml");
                var projects = from e in
                                   doc.Elements("ArrayOfString").Elements("string")
                               select e;
                foreach (var prj in projects)
                {
                    projectsList.Add(prj.Value);
                }
                foreach (var proj in projectsList)
                {
                    MainWindow.windwObj.projectList = proj;
                }

            }
            catch(Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.ToString());
            }
        }


        //displays type dependencies
        internal void displayTypeRel(List<RelStruct> relList,string xml)
        {
            MainWindow.windwObj.XMLView = xml;
            MainWindow.windwObj.results = "Project         Type                 RelatesTo";
            MainWindow.windwObj.results = "-----------------------------------------------------";
            foreach (var r in relList)
            {
                MainWindow.windwObj.results = r.projectName+" ,  "+r.typeName+" ,  "+r.reltnName;
            }
        }

        //displays package dependencies
        internal void displayPkgDep(List<PackageStruct> pkgList,string xml)
        {
            MainWindow.windwObj.XMLView = xml;
            MainWindow.windwObj.results = "Project  FromPackage  ToPackage(Project.Package)";
            MainWindow.windwObj.results = "-----------------------------------------------------";
            foreach (var p in pkgList)
            {
                MainWindow.windwObj.results = p.projectName +",  "+ p.packageFrom + " depends on,  "+p.packageTo;
            }
        }

        //sends the message to servers for analysis
        internal void analyze()
        {
            List<string> projectsList = new List<string>();
            foreach (var proj in MainWindow.windwObj.AnalyzeProjects.Items)
            {
                //test
                projectsList.Add(proj.ToString().Trim());
            }

            if (MainWindow.windwObj.Type.IsChecked == true)
            {
                if (MainWindow.windwObj.Server1.IsChecked == true)
                {
                    SvcMsg msg = new SvcMsg();
                    msg.cmd = SvcMsg.Command.TypeDependency;
                    msg.src = new Uri("http://localhost:8082/MyDependencyAnalyzer");
                    msg.dst = new Uri("http://localhost:8080/MyDependencyAnalyzer");
                    msg.body = "Project List";
                    msg.projectList = projectsList;
                    server0.PostMessage(msg);
                }
                else if (MainWindow.windwObj.Server2.IsChecked == true)
                {
                    SvcMsg msg = new SvcMsg();
                    msg.cmd = SvcMsg.Command.TypeDependency;
                    msg.src = new Uri("http://localhost:8082/MyDependencyAnalyzer");
                    msg.dst = new Uri("http://localhost:8080/MyDependencyAnalyzer");
                    msg.body = "Project List";
                    msg.projectList = projectsList;
                    server1.PostMessage(msg);
                }

                else
                {
                    System.Windows.Forms.MessageBox.Show("Select one of the server");
                }
            }
            else
            {
                if (MainWindow.windwObj.Server1.IsChecked == true)
                {
                    SvcMsg msg = new SvcMsg();
                    msg.cmd = SvcMsg.Command.PackageDependency;
                    msg.src = new Uri("http://localhost:8082/MyDependencyAnalyzer");
                    msg.dst = new Uri("http://localhost:8080/MyDependencyAnalyzer");
                    msg.body = "Project List";
                    msg.projectList = projectsList;
                    server0.PostMessage(msg);
                }
                if (MainWindow.windwObj.Server2.IsChecked == true)
                {
                    Sender send = new Sender("");
                    SvcMsg msg = new SvcMsg();
                    msg.cmd = SvcMsg.Command.PackageDependency;
                    msg.src = new Uri("http://localhost:8082/MyDependencyAnalyzer");
                    msg.dst = new Uri("http://localhost:8080/MyDependencyAnalyzer");
                    msg.projectList = projectsList;
                    msg.body = "Project List";
                    server1.PostMessage(msg);
                }
            }
        }

#if(TEST_CLIENT0)
      static void Main (string[] args)
      {
             Client0 client = new Client0();
             client.startClient();
             Client0 clnt = Client0.getInstance();
             clnt.analyze();
      }
#endif


    }


}
