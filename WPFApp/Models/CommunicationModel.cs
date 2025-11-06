using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WPFApp.Models
{
    /// <summary>
    /// CommunicationModel : centralise vérification (DNS+Ping), UDP (send/listen) et TCP (listener + client).
    /// Les callbacks Action<string> sont invoquées quand un message ou info doit être transmis.
    /// </summary>
    public class CommunicationModel : IDisposable
    {
        // --- UDP ---
        private UdpClient? _udpListener;
        private CancellationTokenSource? _udpCts;

        // --- TCP serveur (listener) ---
        private TcpListener? _tcpListener;
        private CancellationTokenSource? _tcpListenerCts;
        private readonly object _tcpServerLock = new object();

        // --- TCP client ---
        private TcpClient? _tcpClient;
        private CancellationTokenSource? _tcpClientCts;
        private StreamReader? _tcpClientReader;
        private StreamWriter? _tcpClientWriter;
        private readonly object _tcpClientLock = new object();

        public bool TcpClientConnected => _tcpClient != null && _tcpClient.Connected;

        public CommunicationModel()
        {
        }

        #region Vérifier (DNS + IPv4 + Ping)

        /// <summary>
        /// Vérifie que le nom résout en IPv4 et ping la première adresse IPv4 trouvée.
        /// Retour: (success, ipv4StringOrNull, message)
        /// </summary>
        public async Task<(bool success, string? ipv4, string message)> VerifyServerAsync(string hostOrName, int timeoutMs = 2000)
        {
            if (string.IsNullOrWhiteSpace(hostOrName))
                return (false, null, "Nom de serveur vide");

            IPAddress[] addrs;
            try
            {
                addrs = await Dns.GetHostAddressesAsync(hostOrName);
            }
            catch (Exception ex)
            {
                return (false, null, $"Résolution DNS échouée: {ex.Message}");
            }

            IPAddress? ipv4 = null;
            foreach (var a in addrs)
            {
                if (a.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipv4 = a;
                    break;
                }
            }

            if (ipv4 == null)
                return (false, null, "Aucune adresse IPv4 trouvée");

            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ipv4, timeoutMs);
                if (reply.Status == IPStatus.Success)
                    return (true, ipv4.ToString(), "Ping réussi");
                else
                    return (false, ipv4.ToString(), $"Ping échoué: {reply.Status}");
            }
            catch (Exception ex)
            {
                return (false, ipv4.ToString(), $"Erreur Ping: {ex.Message}");
            }
        }

        #endregion

        #region UDP (send / listen)

        /// <summary>
        /// Envoie un message UDP vers host:port (asynchrone).
        /// </summary>
        public async Task UdpSendAsync(string host, int port, string message)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("Host vide", nameof(host));

            using var client = new UdpClient();
            var bytes = Encoding.UTF8.GetBytes(message ?? string.Empty);
            await client.SendAsync(bytes, bytes.Length, host, port);
        }

        /// <summary>
        /// Démarre l'écoute UDP sur le port indiqué. Appelle onMessageReceived(msg) quand message reçu.
        /// </summary>
        public void UdpStartListening(int port, Action<string> onMessageReceived)
        {
            if (onMessageReceived == null) throw new ArgumentNullException(nameof(onMessageReceived));

            if (_udpListener != null)
                throw new InvalidOperationException("UDP listener déjà démarré.");

            _udpCts = new CancellationTokenSource();
            _udpListener = new UdpClient(port);

            Task.Run(async () =>
            {
                try
                {
                    while (!_udpCts.Token.IsCancellationRequested)
                    {
                        var result = await _udpListener.ReceiveAsync();
                        var msg = Encoding.UTF8.GetString(result.Buffer);
                        try { onMessageReceived.Invoke(msg); } catch { /* ignorer exceptions callback */ }
                    }
                }
                catch (ObjectDisposedException) { /* fermé volontairement */ }
                catch (Exception ex)
                {
                    try { onMessageReceived.Invoke($"Erreur écoute UDP: {ex.Message}"); } catch { }
                }
            }, _udpCts.Token);
        }

        /// <summary>
        /// Arrête l'écoute UDP si elle était démarrée.
        /// </summary>
        public void UdpStopListening()
        {
            try
            {
                _udpCts?.Cancel();
                _udpListener?.Close();
                _udpListener?.Dispose();
            }
            finally
            {
                _udpListener = null;
                _udpCts = null;
            }
        }

        #endregion

        #region TCP Listener (serveur)

        /// <summary>
        /// Démarre un TCP listener sur le port donné. onServerReceived(callback) est appelé pour chaque ligne reçue du client.
        /// onInfo est appelé pour des messages d'état / erreur.
        /// </summary>
        public void TcpStartListener(int port, Action<string> onServerReceived, Action<string>? onInfo = null)
        {
            if (onServerReceived == null) throw new ArgumentNullException(nameof(onServerReceived));

            lock (_tcpServerLock)
            {
                if (_tcpListener != null)
                    throw new InvalidOperationException("TCP listener déjà démarré.");

                _tcpListenerCts = new CancellationTokenSource();
                _tcpListener = new TcpListener(IPAddress.Any, port);
                _tcpListener.Start();
                onInfo?.Invoke($"Listener TCP démarré sur le port {port}.");

                Task.Run(async () =>
                {
                    try
                    {
                        while (!_tcpListenerCts.Token.IsCancellationRequested)
                        {
                            var client = await _tcpListener.AcceptTcpClientAsync();
                            onInfo?.Invoke("Client connecté au serveur TCP.");
                            _ = HandleServerSideClientAsync(client, onServerReceived, onInfo, _tcpListenerCts.Token);
                        }
                    }
                    catch (ObjectDisposedException) { /* arrêté */ }
                    catch (Exception ex)
                    {
                        onInfo?.Invoke($"Erreur TcpStartListener: {ex.Message}");
                    }
                }, _tcpListenerCts.Token);
            }
        }

        private async Task HandleServerSideClientAsync(TcpClient client, Action<string> onServerReceived, Action<string>? onInfo, CancellationToken ct)
        {
            try
            {
                using var ns = client.GetStream();
                using var reader = new StreamReader(ns, Encoding.UTF8);
                using var writer = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true };

                // Envoyer message initial de confirmation au client
                await writer.WriteLineAsync("Connexion réussie");

                while (!ct.IsCancellationRequested && client.Connected)
                {
                    string? line = await reader.ReadLineAsync();
                    if (line == null) break;
                    try { onServerReceived.Invoke(line); } catch { }
                }
            }
            catch (Exception ex)
            {
                try { onInfo?.Invoke($"Erreur lecture côté serveur: {ex.Message}"); } catch { }
            }
            finally
            {
                try { client.Close(); } catch { }
            }
        }

        /// <summary>
        /// Arrête le listener TCP (serveur).
        /// </summary>
        public void TcpStopListener()
        {
            lock (_tcpServerLock)
            {
                try
                {
                    _tcpListenerCts?.Cancel();
                    _tcpListener?.Stop();
                }
                finally
                {
                    _tcpListener = null;
                    _tcpListenerCts = null;
                }
            }
        }

        #endregion

        #region TCP Client

        /// <summary>
        /// Se connecte au serveur (host:port). onClientReceived(msg) est appelé quand le client reçoit une ligne.
        /// onInfo pour infos/erreurs.
        /// </summary>
        public async Task TcpConnectAsync(string host, int port, Action<string> onClientReceived, Action<string>? onInfo = null)
        {
            if (onClientReceived == null) throw new ArgumentNullException(nameof(onClientReceived));
            if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("Host vide", nameof(host));

            await TcpDisconnectAsync().ConfigureAwait(false);

            lock (_tcpClientLock)
            {
                _tcpClient = new TcpClient();
                _tcpClientCts = new CancellationTokenSource();
            }

            try
            {
                await _tcpClient.ConnectAsync(host, port).ConfigureAwait(false);
                onInfo?.Invoke($"Connecté au serveur {host}:{port}");

                var ns = _tcpClient.GetStream();
                _tcpClientReader = new StreamReader(ns, Encoding.UTF8);
                _tcpClientWriter = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true };

                // Boucle de lecture sur le client (en tâche à part)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (!_tcpClientCts.Token.IsCancellationRequested && _tcpClient.Connected)
                        {
                            string? line = await _tcpClientReader.ReadLineAsync();
                            if (line == null) break;
                            try { onClientReceived.Invoke(line); } catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        try { onClientReceived.Invoke($"Erreur lecture client: {ex.Message}"); } catch { }
                    }
                }, _tcpClientCts.Token);
            }
            catch (Exception ex)
            {
                await TcpDisconnectAsync().ConfigureAwait(false);
                onInfo?.Invoke($"Erreur connexion TCP: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Envoie une ligne via la connexion TCP client (ajoute newline côté serveur).
        /// </summary>
        public async Task TcpClientSendAsync(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            lock (_tcpClientLock)
            {
                if (_tcpClientWriter == null)
                    throw new InvalidOperationException("Client TCP non connecté.");
            }

            try
            {
                await _tcpClientWriter!.WriteLineAsync(message).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // rethrow ou logger selon besoin
                throw;
            }
        }

        /// <summary>
        /// Déconnecte proprement le client TCP.
        /// </summary>
        public async Task TcpDisconnectAsync()
        {
            lock (_tcpClientLock)
            {
                try
                {
                    _tcpClientCts?.Cancel();
                    _tcpClientReader?.Dispose();
                    _tcpClientWriter?.Dispose();
                    _tcpClient?.Close();
                }
                finally
                {
                    _tcpClient = null;
                    _tcpClientReader = null;
                    _tcpClientWriter = null;
                    _tcpClientCts = null;
                }
            }
            await Task.CompletedTask;
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            try
            {
                UdpStopListening();
            }
            catch { }

            try
            {
                TcpStopListener();
            }
            catch { }

            try
            {
                TcpDisconnectAsync().GetAwaiter().GetResult();
            }
            catch { }
        }

        #endregion
    }
}
