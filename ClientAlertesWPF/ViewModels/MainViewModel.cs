using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Media;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using System.Drawing;

namespace ClientAlertesWPF.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public RelayCommand OpenCommand { get; }
        public RelayCommand ShowWindowCommand { get; }
        public RelayCommand ExitCommand { get; }

        private readonly TaskbarIcon notifyIcon;

        public MainViewModel()
        {
            OpenCommand = new RelayCommand(() => MessageBox.Show("Système d'alertes actif"));
            ShowWindowCommand = new RelayCommand(() => MessageBox.Show("Système d'alertes actif"));
            ExitCommand = new RelayCommand(() => Application.Current.Shutdown());

            notifyIcon = (TaskbarIcon)Application.Current.FindResource("NotifyIcon");
            notifyIcon.Icon = System.Drawing.SystemIcons.Shield; // icône bleue bouclier (toujours visible)

            ConnectToServer();
        }

        private async void ConnectToServer()
        {
            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5177/hubs/alertes")
                .WithAutomaticReconnect()
                .Build();

            connection.On<Alerte>("ReceiveAlerte", (alerte) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Son qui hurle à mort
                    for (int i = 0; i < 10; i++)
                    {
                        SystemSounds.Hand.Play();
                    }

                    // Toast rouge si critique
                    BalloonIcon icon = alerte.Niveau == "Critique" ? BalloonIcon.Error : BalloonIcon.Warning;

                    notifyIcon.ShowBalloonTip(
                        alerte.Titre,
                        alerte.Message,
                        icon);
                });
            });

            try
            {
                await connection.StartAsync();
                notifyIcon.ToolTipText = "Alertes · Connecté";
                notifyIcon.ShowBalloonTip("Système d'alertes", "Connecté avec succès !", BalloonIcon.Info);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERREUR CONNEXION :\n" + ex.Message);
            }
        }
    }

    public class Alerte
    {
        public int Id { get; set; }
        public string Titre { get; set; } = "";
        public string Message { get; set; } = "";
        public string Niveau { get; set; } = "Info";
        public DateTime DateCreation { get; set; }
        public bool EstLue { get; set; }
        public bool EstArchivee { get; set; }
        public int? PosteIdDestinataire { get; set; }
    }
}