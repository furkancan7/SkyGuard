using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using SkyguardApp.Models;

namespace SkyguardApp.ViewModels
{
    public class RadarViewModel : INotifyPropertyChanged
    {
        private TrackTarget _selectedTarget;

        public TrackTarget SelectedTarget
        {
            get => _selectedTarget;
            set
            {
                _selectedTarget = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<LogMessage> LiveLogs { get; set; }

        public RadarViewModel()
        {
            LiveLogs = new ObservableCollection<LogMessage>();

            SelectedTarget = new TrackTarget
            {
                Icao24 = "4BAA61",
                Callsign = "AERO01",
            };
            SelectedTarget.UpdateTelemetry(32500, 480, 115, 12.5);

            // Başlangıç logları
            AddLog("SYSTEM INITIATED. C2 NETWORK ONLINE.", (SolidColorBrush)new BrushConverter().ConvertFrom("#DEFF9A"));
            AddLog("AEROSAN RADAR SWEEP ACTIVE.", Brushes.LightGray);
        }

        public void AddLog(string msg, SolidColorBrush color)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LiveLogs.Add(new LogMessage { Message = $"[{DateTime.Now:HH:mm:ss}] {msg}", Color = color });

                if (LiveLogs.Count > 50)
                    LiveLogs.RemoveAt(0);
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}