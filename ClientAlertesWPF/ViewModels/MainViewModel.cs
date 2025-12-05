using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Drawing;
using System.IO;
using System.Media;
using System.Net.Http;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace ClientAlertesWPF.ViewModels
{
    public class MainViewModel
    {
        private HubConnection connection;
        private readonly TaskbarIcon trayIcon;

        public MainViewModel()
        {
            trayIcon = (TaskbarIcon)Application.Current.FindResource("NotifyIcon");

            // TON LOGO SWITCH DANS LA BARRE DES TÂCHES
            LoadLogoFromUrlAsync();

            ConnectToSignalR();
        }

        private async void LoadLogoFromUrlAsync()
        {
            try
            {
                using var client = new HttpClient();
                var bytes = await client.GetByteArrayAsync("https://switchcompagnie.eu/assets/img/logo.png");
                using var ms = new MemoryStream(bytes);
                using var bitmap = new Bitmap(ms);
                trayIcon.Icon = Icon.FromHandle(bitmap.GetHicon());
            }
            catch
            {
                trayIcon.Icon = System.Drawing.SystemIcons.Shield;
            }
        }

        private async void ConnectToSignalR()
        {
            connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7012/hubs/alertes", options =>
                {
                    options.HttpMessageHandlerFactory = (_) => new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };
                })
                .WithAutomaticReconnect()
                .Build();

            connection.On<Alerte>("ReceiveAlerte", alerte =>
            {
                Application.Current.Dispatcher.Invoke(() => ShowSwitchToast(alerte));
            });

            try
            {
                await connection.StartAsync();
                trayIcon.ToolTipText = "Switch Alertes · Connecté";
                ShowSwitchToast(new Alerte { Titre = "Connecté", Message = "Le poste est prêt à recevoir les alertes.", Niveau = "Info" });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connexion échouée : " + ex.Message);
            }
        }

        private void ShowSwitchToast(Alerte alerte)
        {
            string backgroundColor = alerte.Niveau switch
            {
                "Critique" => "#FF4B4B",
                "Avertissement" => "#FF8C42",
                _ => "#3777FF"
            };

            string sound = alerte.Niveau switch
            {
                "Critique" => "ms-winsoundevent:Notification.SMS",
                "Avertissement" => "ms-winsoundevent:Notification.Reminder",
                _ => "ms-winsoundevent:Notification.Mail"
            };

            string xml = $@"<?xml version='1.0'?>
<toast duration='long' scenario='{(alerte.Niveau == "Critique" ? "alarm" : "default")}'>
    <visual>
        <binding template='ToastGeneric'>
            <image placement='appLogoOverride' hint-crop='circle' src='https://switchcompagnie.eu/assets/img/logo.png'/>
            <text hint-style='title' color='#4A0E80'>{alerte.Titre}</text>
            <text hint-style='body' color='#FFFFFF'>{alerte.Message}</text>
            <text placement='attribution' color='#FF2B80'>
                Poste : {Environment.MachineName} • {DateTime.Now:HH:mm}
            </text>
        </binding>
    </visual>
    <audio src='{sound}' loop='{(alerte.Niveau == "Critique" ? "true" : "false")}'/>
    <actions>
        <action content='Marquer comme lue' arguments='read:{alerte.Id}' activationType='protocol'/>
        <action content='Fermer' arguments='close' activationType='system'/>
    </actions>
</toast>";

            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var toast = new ToastNotification(doc);
            toast.Data = new NotificationData();
            toast.Data.Values["backgroundColor"] = backgroundColor;

            ToastNotificationManager.CreateToastNotifier("Système d'alertes Switch Compagnie").Show(toast);
        }
    }

    public class Alerte
    {
        public int Id { get; set; }
        public string Titre { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Niveau { get; set; } = "Info";
        public DateTime DateCreation { get; set; }
        public bool EstLue { get; set; }
        public bool EstArchivee { get; set; }
        public int? PosteIdDestinataire { get; set; }
    }
}