using System.Net;
using System.Net.Mail;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPFApp
{

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void OpenEmailWindow_Click(object sender, RoutedEventArgs e)
        {
            EmailWindow emailWindow = new EmailWindow();
            emailWindow.Show();
        }

        private void OpenToDoWindow_Click(object sender, RoutedEventArgs e)
        {
            var todo = new ToDoWindow();
            todo.Show();
        }

        private void OpenChronoWindow_Click(object sender, RoutedEventArgs e)
        {
            var chrono = new ChronoWindow();
            chrono.Show();
        }

        private void OpenCommunicationWindow_CLick(object sender, RoutedEventArgs e)
        {
            var comm = new WPFApp.Views.CommunicationWindow();
            comm.Show();
        }
    }
}