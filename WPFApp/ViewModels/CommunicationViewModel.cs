using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WPFApp.Helpers;
using WPFApp.Models;

namespace WPFApp.ViewModels
{
    public class CommunicationViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly CommunicationModel _model = new();
        private readonly Dispatcher _ui = Application.Current.Dispatcher;

        // === Propriétés liées au binding ===
        private string _serveur = "";
        public string Serveur
        {
            get => _serveur;
            set { _serveur = value; OnPropertyChanged(); }
        }

        private string _ip = "";
        public string IP
        {
            get => _ip;
            set { _ip = value; OnPropertyChanged(); }
        }

        private string _message = "";
        public string Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(); }
        }

        private string _echanges = "";
        public string Echanges
        {
            get => _echanges;
            set { _echanges = value; OnPropertyChanged(); }
        }

        // === Commandes ===
        public ICommand VerifierCommand { get; }
        public ICommand UdpConnecterCommand { get; }
        public ICommand UdpEcouterCommand { get; }
        public ICommand ListenerEcouterCommand { get; }
        public ICommand ListenerConnecterCommand { get; }
        public ICommand DeconnecterCommand { get; }
        public ICommand EnvoyerCommand { get; }

        private bool _udpListening = false;
        private bool _tcpListening = false;

        public CommunicationViewModel()
        {
            VerifierCommand = new RelayCommand(async _ => await VerifierAsync());
            UdpConnecterCommand = new RelayCommand(async _ => await UdpEnvoyerAsync());
            UdpEcouterCommand = new RelayCommand(_ => ToggleUdpEcoute());
            ListenerEcouterCommand = new RelayCommand(_ => ToggleTcpListener());
            ListenerConnecterCommand = new RelayCommand(async _ => await TcpConnecterAsync());
            DeconnecterCommand = new RelayCommand(async _ => await DeconnecterAsync());
            EnvoyerCommand = new RelayCommand(async _ => await EnvoyerViaTcpClientAsync());
        }

        private async Task VerifierAsync()
        {
            var (success, ipv4, message) = await _model.VerifyServerAsync(Serveur);
            IP = ipv4 ?? "";
            AppendEchange($"Vérification: {message}");
            MessageBox.Show(success ? "Succès" : "Erreur", message);
        }

        private async Task UdpEnvoyerAsync()
        {
            var msg = string.IsNullOrWhiteSpace(Message) ? "Message test" : Message;
            await _model.UdpSendAsync(Serveur, 8080, msg);
            AppendEchange("Message envoyé + " + msg);
        }

        private void ToggleUdpEcoute()
        {
            if (!_udpListening)
            {
                _model.UdpStartListening(8080, msg => _ui.Invoke(() => AppendEchange("Message reçu + " + msg)));
                _udpListening = true;
                AppendEchange("UDP écoute démarrée");
            }
            else
            {
                _model.UdpStopListening();
                _udpListening = false;
                AppendEchange("UDP écoute arrêtée");
            }
        }

        private void ToggleTcpListener()
        {
            if (!_tcpListening)
            {
                _model.TcpStartListener(8000,
                    msg => _ui.Invoke(() => AppendEchange("Serveur a reçu: " + msg)),
                    info => _ui.Invoke(() => AppendEchange(info))
                );
                _tcpListening = true;
            }
            else
            {
                _model.TcpStopListener();
                _tcpListening = false;
                AppendEchange("Listener TCP arrêté");
            }
        }

        private async Task TcpConnecterAsync()
        {
            await _model.TcpConnectAsync(Serveur, 8000,
                msg => _ui.Invoke(() => AppendEchange("Client a reçu: " + msg)),
                info => _ui.Invoke(() => AppendEchange(info))
            );
            await _model.TcpClientSendAsync($"Machine {Environment.MachineName} connectée");
        }

        private async Task DeconnecterAsync()
        {
            try
            {
                if (_model.TcpClientConnected)
                {
                    await _model.TcpDisconnectAsync();
                    AppendEchange("Client TCP déconnecté.");
                }
                else
                {
                    AppendEchange("Aucune connexion TCP à fermer.");
                }
            }
            catch (Exception ex)
            {
                AppendEchange("Erreur déconnexion TCP: " + ex.Message);
            }
        }

        private async Task EnvoyerViaTcpClientAsync()
        {
            if (string.IsNullOrWhiteSpace(Message))
            {
                AppendEchange("Aucun message à envoyer.");
                return;
            }

            bool sent = false;

            try
            {
                // === Tentative d'envoi via TCP ===
                if (_model.TcpClientConnected)
                {
                    await _model.TcpClientSendAsync(Message);
                    AppendEchange("Message envoyé (TCP): " + Message);
                    sent = true;
                }

                // === Envoi aussi via UDP (par défaut ou si TCP non dispo) ===
                if (!sent || _udpListening)
                {
                    await _model.UdpSendAsync(Serveur, 8080, Message);
                    AppendEchange("Message envoyé (UDP): " + Message);
                }
            }
            catch (Exception ex)
            {
                AppendEchange($"Erreur lors de l’envoi: {ex.Message}");
            }
        }

        private void AppendEchange(string text)
        {
            Echanges += $"{DateTime.Now:HH:mm:ss} - {text}{Environment.NewLine}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Dispose() => _model.Dispose();
    }
}
