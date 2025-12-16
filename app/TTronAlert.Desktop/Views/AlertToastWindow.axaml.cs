using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using NAudio.Wave;
using System;
using TTronAlert.Desktop.ViewModels;

namespace TTronAlert.Desktop.Views
{
    public partial class AlertToastWindow : Window
    {
        private IWavePlayer? _outputDevice;
        private WaveStream? _waveStream;

        public AlertToastWindow()
        {
            InitializeComponent();
            // Fermeture automatique après 15 secondes
            DispatcherTimer.RunOnce(CloseWindow, TimeSpan.FromSeconds(15));

            // Ajout pour démarrer l'animation à l'ouverture
            Opened += OnOpened;
            Closed += OnClosed; // Pour nettoyer l'audio
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            ToastBorder.Opacity = 1;
            if (ToastBorder.RenderTransform is TranslateTransform transform)
            {
                transform.X = 0;
            }

            // Jouer le son de notification
            PlayNotificationSound();
        }

        private void PlayNotificationSound()
        {
            try
            {
                var assetUri = new Uri("avares://TTronAlert.Desktop/Assets/system-notification-02-352442.mp3");
                var audioStream = AssetLoader.Open(assetUri);

                _waveStream = new Mp3FileReader(audioStream); // Pour MP3
                _outputDevice = new WaveOutEvent();
                _outputDevice.Init(_waveStream);
                _outputDevice.Play();
            }
            catch (Exception ex)
            {
                // Gère l'erreur silencieusement ou logue-la (ex. Console.WriteLine(ex.Message));
            }
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            _outputDevice?.Stop();
            _outputDevice?.Dispose();
            _waveStream?.Dispose();
        }

        public void SetAlert(AlertItemViewModel viewModel)
        {
            DataContext = viewModel;
        }

        private void CloseWindow()
        {
            Dispatcher.UIThread.Post(() => this.Close());
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void PositionToast(int index)
        {
            var screen = Screens.ScreenFromVisual(this);
            if (screen != null)
            {
                var workArea = screen.WorkingArea;
                Position = new PixelPoint(workArea.Right - (int)Width - 20, workArea.Bottom - (int)Height - 20 - (index * ((int)Height + 10)));
            }
        }
    }
}