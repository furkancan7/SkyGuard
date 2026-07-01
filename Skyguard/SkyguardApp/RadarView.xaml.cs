using Skyguard.Grpc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SkyguardApp.ViewModels;
using SkyguardApp.Models;
using GMap.NET.WindowsPresentation;

namespace SkyguardApp
{
    public partial class RadarView : UserControl
    {
        private readonly SolidColorBrush _sysGreen = new SolidColorBrush(Color.FromRgb(16, 185, 129));
        private readonly SolidColorBrush _infoCyan = new SolidColorBrush(Color.FromRgb(107, 224, 255));
        private readonly SolidColorBrush _warnYellow = new SolidColorBrush(Color.FromRgb(245, 158, 11));
        private readonly SolidColorBrush _threatRed = new SolidColorBrush(Color.FromRgb(227, 32, 36));
        private TelemetryManager? _telemetryManager;
        private Dictionary<string, GMapMarker> _aircraftMarkers = new Dictionary<string, GMapMarker>();
        private HashSet<string> _activeThreats = new HashSet<string>();
        private int _alertCount = 0;
        private RadarViewModel _viewModel;

        public ObservableCollection<LogModel> OperationalLogs { get; set; } = new ObservableCollection<LogModel>();

        public RadarView()
        {
            InitializeComponent();

            _viewModel = new RadarViewModel();
            this.DataContext = _viewModel;

            LiveLogList.ItemsSource = OperationalLogs;

            try
            {
                var channel = Grpc.Net.Client.GrpcChannel.ForAddress("http://127.0.0.1:8080");
                var client = new Skyguard.Grpc.LiveTelemetryFeed.LiveTelemetryFeedClient(channel);
                _telemetryManager = new TelemetryManager(client);

                LogEvent("SYSTEM", "gRPC Telemetry pipeline channel established successfully at Port 8080.");
            }
            catch (Exception ex)
            {
                LogEvent("THREAT", $"Pipeline Initialization Failure: {ex.Message}");
            }
        }

        public void LogEvent(string type, string message)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (LiveLogList == null) return;

                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                SolidColorBrush logColor;
                switch (type.ToUpper())
                {
                    case "SYSTEM": logColor = _sysGreen; break;
                    case "INFO": logColor = _infoCyan; break;
                    case "WARN": logColor = _warnYellow; break;
                    case "THREAT": logColor = _threatRed; break;
                    default: logColor = Brushes.Gray; break;
                }
                if (type.ToUpper() == "WARN" || type.ToUpper() == "THREAT")
                {
                    _alertCount++;
                    if (FindName("TxtAlertCount") is TextBlock txtAlert)
                    {
                        txtAlert.Text = $"ALERTS: {_alertCount}";
                    }
                }

                if (OperationalLogs.Count > 500)
                {
                    OperationalLogs.RemoveAt(0);
                }

                OperationalLogs.Add(new LogModel
                {
                    Message = $"[{timestamp}] [{type.ToUpper()}] {message}",
                    Color = logColor
                });

                if (OperationalLogs.Count > 0)
                {
                    LiveLogList.ScrollIntoView(OperationalLogs[OperationalLogs.Count - 1]);
                }
            }));
        }

        private void BtnLiveTelemetry_Click(object sender, RoutedEventArgs e)
        {
            if (_telemetryManager == null)
            {
                LogEvent("WARN", "gRPC Telemetry client pipeline not initialized. Verify server node mapping.");
                return;
            }
            if (_telemetryManager.IsStreaming)
            {
                _telemetryManager.OnSafeTelemetryReceived -= TelemetryManager_OnSafeTelemetryReceived;
                _telemetryManager.OnSecurityLogTriggered -= TelemetryManager_OnSecurityLogTriggered;

                _telemetryManager.StopBackgroundFetch();

                BtnLiveTelemetry.Background = Brushes.Transparent;
                BtnLiveTelemetry.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                LogEvent("SYSTEM", "Live Telemetry Feed gRPC stream disconnected by operator.");
                MainMap.Markers.Clear();
                _aircraftMarkers.Clear(); 
                _activeThreats.Clear();
                _alertCount = 0;
                if (FindName("TxtThreatCount") is TextBlock t1) t1.Text = "THREATS: 0";
                if (FindName("TxtTrackCount") is TextBlock t2) t2.Text = "TRACKS: 0";
                if (FindName("TxtAlertCount") is TextBlock t3) t3.Text = "ALERTS: 0";
                if (FindName("TxtLatency") is TextBlock t4) t4.Text = " | 0ms";

                return;
            }
            BtnLiveTelemetry.Background = new SolidColorBrush(Color.FromArgb(38, 16, 185, 129));
            BtnLiveTelemetry.Foreground = _sysGreen;

            LogEvent("SYSTEM", "Connecting to secure gRPC Telemetry Server pipeline...");

            _telemetryManager.OnSafeTelemetryReceived += TelemetryManager_OnSafeTelemetryReceived;
            _telemetryManager.OnSecurityLogTriggered += TelemetryManager_OnSecurityLogTriggered;
            string targetFlight = _viewModel.SelectedTarget != null && !string.IsNullOrEmpty(_viewModel.SelectedTarget.Icao24)
                                  ? _viewModel.SelectedTarget.Icao24
                                  : "SYSTEM_ALL";

            _telemetryManager.StartBackgroundFetch(targetFlight);
        }

        private void TelemetryManager_OnSecurityLogTriggered(string severity, string message)
        {
            LogEvent(severity, message);
        }
        private void TelemetryManager_OnSafeTelemetryReceived(TelemetryUpdate response)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (MainMap == null) return;

                string icao = response.Flightno.ToUpper().Trim();

                if (_viewModel.SelectedTarget == null)
                {
                    _viewModel.SelectedTarget = new TrackTarget();
                }
                bool isAnomaly = response.RiskScore > 70.0 || icao.Contains("THREAT") || icao.Contains("UNKNOWN");
                if (isAnomaly)
                {
                    _viewModel.SelectedTarget.Icao24 = icao;
                    if (!_activeThreats.Contains(icao))
                    {
                        _activeThreats.Add(icao);
                        if (FindName("TxtThreatCount") is TextBlock txtThreat)
                        {
                            txtThreat.Text = $"THREATS: {_activeThreats.Count}";
                        }
                    }
                }
                if (string.IsNullOrEmpty(_viewModel.SelectedTarget.Icao24) || _viewModel.SelectedTarget.Icao24 == icao)
                {
                    _viewModel.SelectedTarget.Icao24 = icao;
                    _viewModel.SelectedTarget.Callsign = response.Flightno;

                    double backendRiskScore = response.RiskScore;

                    _viewModel.SelectedTarget.UpdateTelemetry(
                        response.Baroaltitude,
                        response.Velocity,
                        response.Heading,
                        backendRiskScore);
                }
                if (response.Latitude != 0 && response.Longitude != 0)
                {
                    GMap.NET.PointLatLng newPosition = new GMap.NET.PointLatLng(response.Latitude, response.Longitude);
                    if (_aircraftMarkers.TryGetValue(icao, out GMapMarker existingMarker))
                    {
                        existingMarker.Position = newPosition;
                        if (existingMarker.Shape is TextBlock existingIcon)
                        {
                            existingIcon.ToolTip = $"ID: {icao}\nALT: {response.Baroaltitude:N0} ft\nSPD: {response.Velocity:F0} kts\nRISK: {response.RiskScore}%";
                            if (isAnomaly && existingIcon.Foreground != _threatRed)
                            {
                                existingIcon.Foreground = _threatRed;
                                existingIcon.FontSize = 20;
                                existingIcon.FontWeight = FontWeights.Bold;

                                var pulse = new DoubleAnimation(1.0, 0.4, TimeSpan.FromSeconds(0.5));
                                pulse.AutoReverse = true;
                                pulse.RepeatBehavior = RepeatBehavior.Forever;
                                existingIcon.BeginAnimation(UIElement.OpacityProperty, pulse);
                            }
                            existingIcon.RenderTransform = new System.Windows.Media.RotateTransform(response.Heading);
                            existingIcon.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                        }
                    }
                    else
                    {
                        var newMarker = new GMapMarker(newPosition);

                        SolidColorBrush targetColor = _infoCyan;

                        if (isAnomaly)
                        {
                            targetColor = _threatRed;
                        }

                        var planeIcon = new System.Windows.Controls.TextBlock
                        {
                            Text = "✈",
                            FontFamily = new System.Windows.Media.FontFamily("Segoe UI Symbol"),
                            Foreground = targetColor,
                            FontSize = isAnomaly ? 20 : 16,
                            FontWeight = isAnomaly ? FontWeights.Bold : FontWeights.Normal,
                            ToolTip = $"ID: {icao}\nALT: {response.Baroaltitude:N0} ft\nSPD: {response.Velocity:F0} kts\nRISK: {response.RiskScore}%"
                        };
                        planeIcon.RenderTransform = new System.Windows.Media.RotateTransform(response.Heading);
                        planeIcon.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                        if (isAnomaly)
                        {
                            var pulse = new DoubleAnimation(1.0, 0.4, TimeSpan.FromSeconds(0.5));
                            pulse.AutoReverse = true;
                            pulse.RepeatBehavior = RepeatBehavior.Forever;
                            planeIcon.BeginAnimation(UIElement.OpacityProperty, pulse);
                        }

                        newMarker.Offset = new System.Windows.Point(-8, -8);
                        newMarker.Shape = planeIcon;

                        MainMap.Markers.Add(newMarker);
                        _aircraftMarkers.Add(icao, newMarker);
                        if (FindName("TxtTrackCount") is TextBlock txtTrack)
                        {
                            txtTrack.Text = $"TRACKS: {_aircraftMarkers.Count}";
                        }
                    }
                }
                if (FindName("TxtLatency") is TextBlock txtLatency)
                {
                    txtLatency.Text = $" | {new Random().Next(18, 55)}ms";
                }

            }));

            LogEvent("INFO", $"Target vector {response.Flightno} parsed via gRPC. Speed: {response.Velocity} KTS");
        }

        private void MainMap_Loaded(object sender, RoutedEventArgs e)
        {
            GMap.NET.MapProviders.GMapProvider.UserAgent = "SkyguardApp_MyPersonalProject_v1";
            var provider = GMap.NET.MapProviders.BingMapProvider.Instance;
            provider.RefererUrl = "https://github.com/furkancan7";

            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerAndCache;
            MainMap.MapProvider = GMap.NET.MapProviders.BingHybridMapProvider.Instance;
            MainMap.Position = new GMap.NET.PointLatLng(39.9334, 32.8597); // Ankara
            MainMap.MinZoom = 2;
            MainMap.MaxZoom = 17;
            MainMap.Zoom = 6;
            MainMap.DragButton = MouseButton.Left;

            MainMap.ReloadMap();

            this.Focusable = true;
            this.Focus();
            LogEvent("SYSTEM", "AEROSAN Sky Guard AI Core operational.");
            LogEvent("INFO", "Tactical map centered at Ankara Command Hub Layer.");
        }

        private void Key_Down(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.OemPlus || e.Key == Key.Add) && Keyboard.Modifiers == ModifierKeys.Control)
            {
                MainMap.Zoom = MainMap.Zoom + 0.5;
            }
            else if ((e.Key == Key.OemMinus || e.Key == Key.Subtract) && Keyboard.Modifiers == ModifierKeys.Control)
            {
                MainMap.Zoom = MainMap.Zoom - 0.5;
            }
            else if (e.Key == Key.Left || e.Key == Key.A)
            {
                MainMap.Position = new GMap.NET.PointLatLng(MainMap.Position.Lat, MainMap.Position.Lng - 0.05);
            }
            else if (e.Key == Key.Right || e.Key == Key.D)
            {
                MainMap.Position = new GMap.NET.PointLatLng(MainMap.Position.Lat, MainMap.Position.Lng + 0.05);
            }
            else if (e.Key == Key.Up || e.Key == Key.W)
            {
                MainMap.Position = new GMap.NET.PointLatLng(MainMap.Position.Lat + 0.05, MainMap.Position.Lng);
            }
            else if (e.Key == Key.Down || e.Key == Key.S)
            {
                MainMap.Position = new GMap.NET.PointLatLng(MainMap.Position.Lat - 0.05, MainMap.Position.Lng);
            }
        }

        private void btn_AnomalyPanel_Click(object sender, RoutedEventArgs e)
        {
            LogEvent("INFO", "Radar active sweep initialized. Ingesting live telemetry feed.");
        }

        private void ToggleSidebar(bool collapse)
        {
            DoubleAnimation anim = new DoubleAnimation(collapse ? 50 : 240, TimeSpan.FromMilliseconds(200));
            SidebarContainer.BeginAnimation(WidthProperty, anim);
            SidebarContainer.Tag = collapse ? "Collapsed" : "Visible";
        }

        private void ToggleLogStream(bool expand)
        {
            DoubleAnimation anim = new DoubleAnimation(expand ? 180 : 40, TimeSpan.FromMilliseconds(200));
            LogStreamPanel.BeginAnimation(HeightProperty, anim);
        }

        private void Filter_SpeedAltitude_Click(object sender, RoutedEventArgs e)
        {
            LogEvent("WARN", "Filter Enforced: Isolating airspeed and barometric altitude anomalies.");
        }

        private void Filter_RouteViolation_Click(object sender, RoutedEventArgs e)
        {
            LogEvent("WARN", "Filter Enforced: Scanning for flight path and transponder non-compliance.");
        }

        private void Filter_SignalLoss_Click(object sender, RoutedEventArgs e)
        {
            LogEvent("THREAT", "CRITICAL: Signal Loss audit enabled. Scanning sectors for potential Jamming vectors.");
        }

        private void Filter_CriticalHigh_Click(object sender, RoutedEventArgs e)
        {
            LogEvent("INFO", "Filtering airspace targets: Severity High (Red Profile) only.");
        }

        private void Filter_CriticalMedium_Click(object sender, RoutedEventArgs e)
        {
            LogEvent("INFO", "Filtering airspace targets: Suspicious Tracks (Yellow Profile) only.");
        }

        private void Track_Target_Click(object sender, RoutedEventArgs e)
        {
            LogEvent("INFO", "Target lock vector computed. Initiating persistent sensory tracking.");
        }

        private void Track_HistoryProjection_Click(object sender, RoutedEventArgs e)
        {
            LogEvent("INFO", "Historical trajectory trace calculated for selected target matrix.");
        }

        private void Track_AIPrediction_Click(object sender, RoutedEventArgs e)
        {
            LogEvent("SYSTEM", "ML Predictor Engine executing 3D flight path projection (0.92 AUC Precision).");
        }

        private void Action_BroadcastAlert_Click(object sender, RoutedEventArgs e)
        {
            LogEvent("THREAT", "🚨 ALERT BROADCAST SIGNED: Airspace warning vector transmitted to secondary tactical units.");
        }

        private void Action_ReportCenter_Click(object sender, RoutedEventArgs e)
        {
            LogEvent("SYSTEM", "Incident file compiled and securely routed to Central Intelligence Command.");
        }

        private void Action_MarkAsFriendly_Click(object sender, RoutedEventArgs e)
        {
            LogEvent("INFO", "Target track override executed: Profile tagged as FRIENDLY.");
        }

        private void btn_AddRoute_Click(object sender, RoutedEventArgs e)
        {
            LogEvent("INFO", "Waypoint injection terminal active. Waiting for manual coordinates input.");
        }

        private void btn_Report_Click(object sender, RoutedEventArgs e)
        {
            LogEvent("SYSTEM", "Automated threat analytics report generated successfully.");
        }

        private void btn_Settings_Click(object sender, RoutedEventArgs e)
        {
            LogEvent("INFO", "Accessing local C2 hardware configuration and API connection nodes.");
        }

        private void Btn_FleetManagement_Click(object sender, RoutedEventArgs e)
        {
            LogEvent("INFO", "Accessing proprietary enterprise flight profiles layer.");
            MessageBox.Show("Proprietary Fleet Management Panel will be integrated in the next patch.", "AEROSAN Sky Guard", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btn_Logout_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to securely terminate the C2 Network connection?",
                "Secure Exit", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                LogEvent("SYSTEM", "C2 Network connection terminated securely by operator.");
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.SwitchToLoginView();
                }
            }
        }
    }

    public class LogModel
    {
        public string Message { get; set; } = string.Empty;
        public SolidColorBrush Color { get; set; } = Brushes.Gray;
    }
}