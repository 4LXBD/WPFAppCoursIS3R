using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPFApp
{
    /// <summary>
    /// Logique d'interaction pour EmailWindow.xaml
    /// </summary>
    public partial class EmailWindow : Window
    {
        public EmailWindow()
        {
            InitializeComponent();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mail = new MailMessage(FromBox.Text, ToBox.Text, SubjectBox.Text, BodyBox.Text);


                using (var client = new SmtpClient("smtp.gmail.com", 587))
                {
                    client.Credentials = new NetworkCredential(FromBox.Text, PasswordBox.Password);
                    client.EnableSsl = true;

                    await client.SendMailAsync(mail);
                }

                StatusBlock.Text = "Email sent successfully!";
            }

            catch (System.Exception ex)
            {
                StatusBlock.Text = $"Error: {ex.Message}";
            }
        }
    }
}
