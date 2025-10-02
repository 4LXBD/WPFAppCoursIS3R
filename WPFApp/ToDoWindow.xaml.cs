using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace WPFApp
{
    /// <summary>
    /// Logique d'interaction pour ToDoWindow.xaml
    /// </summary>
    public partial class ToDoWindow : Window
    {
        public ObservableCollection<TaskItem> Tasks { get; set; } = new ObservableCollection<TaskItem>();
        public ToDoWindow()
        {
            InitializeComponent();
            DataContext = this;

            Tasks.Add(new TaskItem { Title = "Exemple : faire les devoirs", IsDone = false });
            Tasks.Add(new TaskItem { Title = "Exemple : envoyer email", IsDone = true });
        }

        private void AddButton_Click(Object sender, RoutedEventArgs e)
        {
            var text = NewTaskTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("Entrez le titre de la tâche.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Tasks.Add(new TaskItem { Title = text, IsDone = false });
            NewTaskTextBox.Clear();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksListBox.SelectedItem is TaskItem selected)
            {
                var result = MessageBox.Show($"Supprimer la tâche : «{selected.Title}» ?", "Supprimer", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes) Tasks.Remove(selected);
            }
            else
            {
                MessageBox.Show("Sélectionnez une tâche à supprimer.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
