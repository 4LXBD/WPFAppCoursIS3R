using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Timers;

namespace WPFApp
{
    
    /// <summary>
    /// ViewModel du chronomètre — gère la logique de temps et le binding vers la vue.
    /// </summary>
    public class ChronoViewModel : INotifyPropertyChanged
    {
        // ---- Champs privés ----
        private System.Timers.Timer _timer;   // Le timer qui déclenche un "tick" chaque seconde
        private int _seconds;                 // Compte les secondes écoulées
        private int _minutes;                 // (Facultatif) Compte les minutes
        private double _secondAngle;          // Angle de l’aiguille des secondes
        private double _minuteAngle;          // Angle de l’aiguille des minutes
        private bool _isRunning;              // Indique si le chrono est en marche

        // ---- Événement de notification ----
        public event PropertyChangedEventHandler? PropertyChanged;

        // ---- Propriétés liées au cadran ----

        /// <summary>
        /// Angle de l’aiguille des secondes (lié au RotateTransform dans le XAML)
        /// </summary>
        public double SecondAngle
        {
            get => _secondAngle;
            set
            {
                if (_secondAngle != value)
                {
                    _secondAngle = value;
                    OnPropertyChanged(nameof(SecondAngle)); // Notifie la vue
                }
            }
        }

        /// <summary>
        /// Angle de l’aiguille des minutes (lié au RotateTransform dans le XAML)
        /// </summary>
        public double MinuteAngle
        {
            get => _minuteAngle;
            set
            {
                if (_minuteAngle != value)
                {
                    _minuteAngle = value;
                    OnPropertyChanged(nameof(MinuteAngle));
                }
            }
        }

        // ---- Commandes liées aux boutons ----
        public ICommand StartCommand { get; }   // Lancer le chrono
        public ICommand StopCommand { get; }    // Arrêter le chrono
        public ICommand ResetCommand { get; }   // Réinitialiser

        // ---- Constructeur ----
        public ChronoViewModel()
        {
            // Création du timer (1 tick par seconde)
            _timer = new System.Timers.Timer(1000);

            // On relie l'événement Elapsed à notre méthode Tick()
            _timer.Elapsed += (s, e) => Tick();

            // On crée les commandes en précisant la méthode à exécuter et la condition d'activation
            StartCommand = new RelayCommand(Start, () => !_isRunning);
            StopCommand = new RelayCommand(Stop, () => _isRunning);
            ResetCommand = new RelayCommand(Reset, () => !_isRunning);
        }

        // ---- Méthode appelée chaque seconde ----
        private void Tick()
        {
            // Incrémente les secondes
            _seconds++;

            // Calcule l'angle de l’aiguille des secondes (360° / 60 secondes = 6° par seconde)
            SecondAngle = (_seconds % 60) * 6;

            // Calcule l'angle de l’aiguille des minutes (360° / 60 minutes = 6° par minute)
            MinuteAngle = (_seconds / 60 % 60) * 6;
        }

        // ---- Démarrer le chronomètre ----
        private void Start()
        {
            _isRunning = true;
            _timer.Start();                // Lance le timer
            RaiseCanExecuteChanged();      // Met à jour l’état des boutons
        }

        // ---- Arrêter le chronomètre ----
        private void Stop()
        {
            _isRunning = false;
            _timer.Stop();                 // Arrête le timer
            RaiseCanExecuteChanged();
        }

        // ---- Réinitialiser le chronomètre ----
        private void Reset()
        {
            _seconds = 0;
            _minutes = 0;
            SecondAngle = 0;
            MinuteAngle = 0;
        }

        // ---- Met à jour les commandes (pour activer/désactiver les boutons) ----
        private void RaiseCanExecuteChanged()
        {
            (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ResetCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        // ---- Notifie la vue qu’une propriété a changé ----
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
