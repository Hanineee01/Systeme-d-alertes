using Hardcodet.Wpf.TaskbarNotification;
using ClientAlertesWPF.ViewModels;
using Microsoft.AspNetCore.SignalR.Client;
using CommunityToolkit.WinUI.Notifications;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ClientAlertesWPF.Services
{
    public class AlertService
    {
        private readonly TaskbarIcon _tb;
        private HubConnection? _connection;
        private readonly HttpClient _http = new HttpClient();
        private readonly int _machineId = Environment.MachineName.GetHashCode();
        private readonly string _logoPath;

        public AlertService(TaskbarIcon tb)
        {
            _tb = tb;
            _logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Icons", "logo.png");

            // Affiche l'ID dans le tooltip pour debug
            _tb.ToolTipText = $"Système d'alertes (ID: {_machineId})";
        }

        public async void Start()
        {
            // 1. Enregistrement de la machine sur le serveur (obligatoire)
            await RegisterMachineAsync();

            // 2. SignalR (temps réel)
            _connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/alerthub") // Change le port si ton API est sur un autre
                .Build();

            _connection.On<string>("ReceiveAlert", (json) =>
            {
                var vm = JsonConvert.DeserializeObject<AlertViewModel>(json);
                if (vm != null) ShowToast(vm);
            });

            try
            {
                await _connection.StartAsync();
                _tb.ToolTipText += " - Connecté en temps réel";
            }
            catch (Exception)
            {
                _tb.ToolTipText += " - Mode polling activé";
                _ = PollingLoopAsync();
            }
        }

        private async Task RegisterMachineAsync()
        {
            try
            {
                var payload = new
                {
                    UniqueId = _machineId.ToString(),
                    Name = Environment.MachineName
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                await _http.PostAsync("http://localhost:5000/api/machines/register", content);
            }
            catch { }
        }

        private async Task PollingLoopAsync()
        {
            while (true)
            {
                try
                {
                    var response = await _http.GetAsync($"http://localhost:5000/api/alerts/pending/{_machineId}");

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var alerts = JsonConvert.DeserializeObject<List<AlertViewModel>>(json);

                        if (alerts != null)
                        {
                            foreach (var vm in alerts)
                            {
                                ShowToast(vm);
                            }
                        }
                    }
                }
                catch { }

                await Task.Delay(4000); // 4 secondes
            }
        }

        private void ShowToast(AlertViewModel vm)
        {
            var toast = new ToastContentBuilder()
                .AddAppLogoOverride(new Uri(_logoPath), ToastGenericAppLogoCrop.Circle)
                .AddHeader(vm.Level ?? "Info", vm.Title ?? "Alerte", "")
                .AddText(vm.Message ?? "")
                .AddButton(new ToastButton("OK", "dismiss").SetBackgroundActivation());

            if (vm.Level == "Critical")
            {
                toast.AddAudio(new ToastAudio { Src = new Uri("ms-winsoundevent:Notification.Looping.Alarm"), Loop = true });
                toast.SetToastScenario(ToastScenario.Alarm);
            }
            else if (vm.Level == "Warning")
            {
                toast.AddAudio(new ToastAudio { Src = new Uri("ms-winsoundevent:Notification.IM") });
            }
            else
            {
                toast.AddAudio(new ToastAudio { Src = new Uri("ms-winsoundevent:Notification.Default") });
            }

            toast.Show();
        }

        public void Stop()
        {
            _connection?.StopAsync().Wait();
        }
    }
}