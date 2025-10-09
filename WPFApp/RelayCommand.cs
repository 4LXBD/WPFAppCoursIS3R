using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WPFApp
{
    /// <summary>
    /// Classe utilitaire pour implémenter facilement ICommand dans le pattern MVVM.
    /// Permet de relier une action (méthode à exécuter) et une condition d’activation à un bouton.
    /// </summary>
    public class RelayCommand : ICommand
    {
        // ---- Champs privés ----
        private readonly Action _execute;           // L'action à exécuter quand la commande est appelée (ex: Start, Stop…)
        private readonly Func<bool>? _canExecute;   // Fonction qui détermine si la commande peut être exécutée (active/désactivée)

        // ---- Événement requis par ICommand ----
        /// <summary>
        /// Événement déclenché quand l’état d’exécution de la commande change.
        /// (WPF s’en sert pour actualiser automatiquement les boutons dans la vue)
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        // ---- Constructeur ----
        /// <summary>
        /// Crée une nouvelle commande.
        /// </summary>
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;             // Méthode à exécuter lorsque la commande est déclenchée.
            _canExecute = canExecute;       //Méthode optionnelle qui renvoie true/false selon si la commande peut être exécutée.
        }

        // ---- Méthodes de ICommand ----

        /// <summary>
        /// Détermine si la commande peut être exécutée (appelée par WPF automatiquement).
        /// </summary>
        /// <param name="parameter">Paramètre optionnel (non utilisé ici).</param>
        /// <returns>True si la commande est active, sinon False.</returns>
        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();

        /// <summary>
        /// Exécute l’action associée à la commande.
        /// </summary>
        /// <param name="parameter">Paramètre optionnel (non utilisé ici).</param>
        public void Execute(object? parameter) => _execute();

        // ---- Méthode utilitaire ----
        /// <summary>
        /// Permet de forcer WPF à re-vérifier la condition CanExecute().
        /// (Très utile pour actualiser l’état des boutons quand une variable change)
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            // Déclenche l’événement pour avertir WPF qu’il doit réévaluer la commande
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
