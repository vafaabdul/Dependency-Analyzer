using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using DependencyAnalyzer;


namespace Client0UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal static MainWindow windwObj;
        public MainWindow()
        {

            try
            {
                this.WindowState = WindowState.Maximized;
                InitializeComponent();
                result.Items.Add("");
                windwObj = this;
                Client0 client = new Client0();
                client.startClient();
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.ToString());
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

     

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            AnalyzeProjects.Items.Remove(AnalyzeProjects.SelectedItem);
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            App.Current.Shutdown();
        }
        private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Analyze_Click(object sender, RoutedEventArgs e)
        {
            RBXML.SelectAll();
            RBXML.Selection.Text = "";
            result.Items.Clear();
            Client0 clnt = Client0.getInstance();
            clnt.analyze();
        }

        private void GetProjects_Click(object sender, RoutedEventArgs e)
        {

           

        }

        private void Type_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Package_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!AnalyzeProjects.Items.Contains(ResultantProjects.SelectedItem.ToString()))
                {
                    AnalyzeProjects.Items.Add(ResultantProjects.SelectedItem.ToString());
                }
            }

            catch (Exception exp)
            {
                System.Windows.Forms.MessageBox.Show("Please Select Project");

            }
        }


  

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                if (Server1.IsChecked==true)
                {
                    Sender server0 = new Sender("http://localhost:8080/MyDependencyAnalyzer");
                    SvcMsg msg0 = new SvcMsg();
                    msg0.cmd = SvcMsg.Command.Projects;
                    msg0.src = new Uri("http://localhost:8082/MyDependencyAnalyzer");
                    msg0.dst = new Uri("http://localhost:8080/MyDependencyAnalyzer");
                    msg0.body = "body";
                    server0.PostMessage(msg0);
                }

                else if (Server2.IsChecked==true)
                {
                    Sender server1 = new Sender("http://localhost:8081/MyDependencyAnalyzer");
                    SvcMsg msg1 = new SvcMsg();
                    msg1.cmd = SvcMsg.Command.Projects;
                    msg1.src = new Uri("http://localhost:8082/MyDependencyAnalyzer");
                    msg1.dst = new Uri("http://localhost:8081/MyDependencyAnalyzer");
                    msg1.body = "body";
                    server1.PostMessage(msg1);
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Select one of the server");
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }

        private void Server1_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Server2_Checked(object sender, RoutedEventArgs e)
        {

        }

        internal string projectList
        {
            get { return ""; }
            set
            {
                Dispatcher.Invoke((Action)(() => { ResultantProjects.Items.Add(value); }));
            }
        }

        internal string XMLView
        {
            get { return ""; }
            set
            {
                Dispatcher.Invoke((Action)(() => { RBXML.AppendText(value); }));
            }
        }
        internal String results
        {
            get { return null; }
            set
            {
                Dispatcher.Invoke((Action)(() => { result.Items.Add(value); }));
            }
        }

    



    }

}
