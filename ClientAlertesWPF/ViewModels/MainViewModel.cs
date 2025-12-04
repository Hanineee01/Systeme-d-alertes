using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Media;
using System.Net.Http;
using System.Windows;

namespace ClientAlertesWPF.ViewModels
{
    public class MainViewModel
    {
        private readonly TaskbarIcon tray;

        public MainViewModel()
        {
            tray = (TaskbarIcon)Application.Current.FindResource("MyNotifyIcon");

            MessageBox.Show("CLIENT LANCÉ – ICÔNE DEVRAIT ÊTRE VISIBLE", "TEST", MessageBoxButton.OK, MessageBoxImage.Information);

            Connect();
        }

        private async void Connect()
        {
            var conn = new HubConnectionBuilder()
                .WithUrl("https://localhost:7012/hubs/alertes", o =>
                {
                    o.HttpMessageHandlerFactory = (_) => new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };
                })
                .Build();

            conn.On<string, string, string>("ReceiveAlerte", (titre, message, niveau) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (niveau == "Critique")
                    {
                        for (int i = 0; i < 15; i++)
                            SystemSounds.Hand.Play();
                    }

                    tray.ShowBalloonTip(titre, message, BalloonIcon.Info);
                });
            });

            try
            {
                await conn.StartAsync();
                tray.ToolTipText = "Connecté";
                tray.ShowBalloonTip("CONNECTÉ", "Le poste est prêt", BalloonIcon.Info);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERREUR : " + ex.Message);
            }
        }
    }
}