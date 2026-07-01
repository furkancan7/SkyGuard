using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SkyGuardBackend.Services;
using System.Text;
using Skyguard.Extension;
using Skyguard.Services;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(8080, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});
builder.Services.AddGrpc();
builder.Services.AddHttpClient();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
var app = builder.Build();
app.UseCors("AllowAll");
app.MapGrpcService<LiveTelemetryService>();
app.MapGrpcService<FlightTrackerService>();
app.MapControllers();
app.UseAuthentication();
app.UseAuthorization();
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.MapPost("/", () => $"Service Aktif edildi port {port} kullanýn.");
app.Run($"http://127.0.0.1:{port}");
app.UseAuthentication();
app.UseAuthorization();