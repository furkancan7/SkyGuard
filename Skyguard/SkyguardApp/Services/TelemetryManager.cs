/*using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Skyguard.Grpc;

namespace SkyguardApp;

public class TelemetryManager
{
    private CancellationTokenSource? _cts;

    private readonly LiveTelemetryFeed.LiveTelemetryFeedClient _grpcClient;

    private DateTime _lastPacketTime = DateTime.MinValue;
    private const int MinPacketIntervalMs = 5;

    public bool IsStreaming => _cts != null;
    public event Action<TelemetryUpdate>? OnSafeTelemetryReceived;
    public event Action<string, string>? OnSecurityLogTriggered;


    public TelemetryManager(LiveTelemetryFeed.LiveTelemetryFeedClient grpcClient)
    {
        _grpcClient = grpcClient;
    }

    public void StartBackgroundFetch(string targetFlightNo)
    {
        if (IsStreaming) return;
        _cts = new CancellationTokenSource();

        Task.Run(async () =>
        {
            OnSecurityLogTriggered?.Invoke("SYSTEM", "Secure dedicated telemetry stream link deployed.");

            try
            {
                var request = new TelemetryRequest { Flightno = targetFlightNo };

                using var call = _grpcClient.TelemetryData(request, cancellationToken: _cts.Token);

                while (await call.ResponseStream.MoveNext(_cts.Token))
                {
                    var packet = call.ResponseStream.Current;

                    if (!ValidateIncomingPacket(packet))
                    {
                        OnSecurityLogTriggered?.Invoke("WARN", $"Anti-Spoofing: Discarded suspicious payload frame from {packet.Flightno}");
                        continue;
                    }

                    OnSafeTelemetryReceived?.Invoke(packet);
                }
            }
            catch (OperationCanceledException)
            {
                OnSecurityLogTriggered?.Invoke("SYSTEM", "Dedicated telemetry background task stopped securely.");
            }
            catch (Exception ex)
            {
                OnSecurityLogTriggered?.Invoke("THREAT", $"Telemetry Link Failure: {ex.Message}");
                StopBackgroundFetch();
            }
        }, _cts.Token);
    }

    public void StopBackgroundFetch()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private bool ValidateIncomingPacket(TelemetryUpdate packet)
    {
        var now = DateTime.Now;
        if ((now - _lastPacketTime).TotalMilliseconds < MinPacketIntervalMs) return false;
        _lastPacketTime = now;

        if (packet.Baroaltitude > 80000 || packet.Velocity > 2000) return false;
        if (packet.Flightno.Length > 10) return false;

        return true;
    }
}*/
using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Skyguard.Grpc;

namespace SkyguardApp;

public class TelemetryManager
{
    private CancellationTokenSource? _cts;
    private readonly LiveTelemetryFeed.LiveTelemetryFeedClient _grpcClient;
    private DateTime _lastPacketTime = DateTime.MinValue;
    private const int MinPacketIntervalMs = 5;

    // VİDEO DEMOSU İÇİN GİZLİ BAYRAK
    private bool _injectAnomalyOnNextPacket = false;

    public bool IsStreaming => _cts != null;
    public event Action<TelemetryUpdate>? OnSafeTelemetryReceived;
    public event Action<string, string>? OnSecurityLogTriggered;

    public TelemetryManager(LiveTelemetryFeed.LiveTelemetryFeedClient grpcClient)
    {
        _grpcClient = grpcClient;
    }
    public void TriggerMockAnomaly()
    {
        _injectAnomalyOnNextPacket = true;
        OnSecurityLogTriggered?.Invoke("WARN", "MANUAL OVERRIDE: Preparing to inject synthetic threat into telemetry stream...");
    }

    public void StartBackgroundFetch(string targetFlightNo)
    {
        if (IsStreaming) return;
        _cts = new CancellationTokenSource();

        Task.Run(async () =>
        {
            OnSecurityLogTriggered?.Invoke("SYSTEM", "Secure dedicated telemetry stream link deployed.");

            try
            {
                var request = new TelemetryRequest { Flightno = targetFlightNo };
                using var call = _grpcClient.TelemetryData(request, cancellationToken: _cts.Token);

                while (await call.ResponseStream.MoveNext(_cts.Token))
                {
                    var packet = call.ResponseStream.Current;

                    if (!ValidateIncomingPacket(packet))
                    {
                        OnSecurityLogTriggered?.Invoke("WARN", $"Anti-Spoofing: Discarded suspicious payload frame from {packet.Flightno}");
                        continue;
                    }
                    if (_injectAnomalyOnNextPacket)
                    {
                        packet.Velocity = 1150f;       
                        packet.Baroaltitude = 12000f;    
                        packet.RiskScore = 98.5f; 

                        _injectAnomalyOnNextPacket = false;

                        OnSecurityLogTriggered?.Invoke("THREAT", $"[CRITICAL ALERT] SQUAWK 7500 (HIJACK/UNLAWFUL INTERFERENCE) DETECTED! TARGET: {packet.Flightno} | SPD: {packet.Velocity} KTS");
                    }

                    OnSafeTelemetryReceived?.Invoke(packet);
                }
            }
            catch (OperationCanceledException)
            {
                OnSecurityLogTriggered?.Invoke("SYSTEM", "Dedicated telemetry background task stopped securely.");
            }
            catch (Exception ex)
            {
                OnSecurityLogTriggered?.Invoke("THREAT", $"Telemetry Link Failure: {ex.Message}");
                StopBackgroundFetch();
            }
        }, _cts.Token);
    }

    public void StopBackgroundFetch()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private bool ValidateIncomingPacket(TelemetryUpdate packet)
    {
        var now = DateTime.Now;
        if ((now - _lastPacketTime).TotalMilliseconds < MinPacketIntervalMs) return false;
        _lastPacketTime = now;

        if (packet.Baroaltitude > 80000 || packet.Velocity > 2000) return false;
        if (packet.Flightno.Length > 10) return false;

        return true;
    }
}