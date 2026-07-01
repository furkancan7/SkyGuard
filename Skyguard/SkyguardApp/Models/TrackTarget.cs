using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SkyguardApp.Models
{
    public class TrackTarget : INotifyPropertyChanged
    {
        private string _icao24;
        private string _callsign;
        private double _altitude;
        private double _speed;
        private double _heading;
        private DateTime _lastUpdate;
        private double _riskScore;

        public string Icao24
        {
            get => _icao24;
            set { _icao24 = value; OnPropertyChanged(); }
        }

        public string Callsign
        {
            get => _callsign;
            set { _callsign = value; OnPropertyChanged(); }
        }

        public string AltitudeString => $"{_altitude:F0} ft";
        public string SpeedString => $"{_speed:F0} kts";
        public string HeadingString => $"{_heading:F0}°";
        public string LastUpdateString => _lastUpdate.ToString("HH:mm:ss.fff");
        public string RiskScoreString => _riskScore < 50 ? $"{_riskScore:F1}% (LOW)" : $"{_riskScore:F1}% (CRITICAL)";

        public void UpdateTelemetry(double alt, double speed, double heading, double risk)
        {
            _altitude = alt;
            _speed = speed;
            _heading = heading;
            _riskScore = risk;
            _lastUpdate = DateTime.Now;

            OnPropertyChanged(nameof(AltitudeString));
            OnPropertyChanged(nameof(SpeedString));
            OnPropertyChanged(nameof(HeadingString));
            OnPropertyChanged(nameof(LastUpdateString));
            OnPropertyChanged(nameof(RiskScoreString));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}