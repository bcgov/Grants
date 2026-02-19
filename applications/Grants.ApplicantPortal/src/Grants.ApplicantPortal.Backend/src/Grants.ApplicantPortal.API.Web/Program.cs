using Grants.ApplicantPortal.API.Web.Configurations;

var builder = WebApplication.CreateBuilder(args);

// When running behind a reverse proxy (e.g. HAProxy → Node Express → Kestrel),
// the proxy buffers the full request before forwarding to Kestrel. This introduces
// a delay between Kestrel receiving the headers and the body arriving, which trips
// the default MinRequestBodyDataRate check and results in 408 Request Timeout.
// Disabling the rate check is a recommended practice behind reverse proxies — the
// proxy itself manages slow-client protection.
// The primary fix should be on the proxy side (e.g. streaming the body to Kestrel
// without buffering). This setting is belt-and-suspenders insurance.
if (builder.Configuration.GetValue("Kestrel:DisableMinRequestBodyDataRate", defaultValue: true))
{
  builder.WebHost.ConfigureKestrel(options =>
  {
    options.Limits.MinRequestBodyDataRate = null;
  });
}

var logger = Log.Logger = new LoggerConfiguration()
  .Enrich.FromLogContext()
  .WriteTo.Console()
  .CreateLogger();

logger.Information("Starting web host");

builder.AddLoggerConfigs();

var appLogger = new SerilogLoggerFactory(logger)
    .CreateLogger<Program>();

builder.Services.AddOptionConfigs(builder.Configuration, appLogger, builder);
builder.Services.AddServiceConfigs(appLogger, builder);

builder.Services.AddFastEndpoints()
                .SwaggerDocument(o =>
                {
                  o.ShortSchemaNames = true;
                });

builder.AddServiceDefaults();

var app = builder.Build();

await app.UseAppMiddlewareAndSeedDatabase();

app.Run();

// Make the implicit Program.cs class public, so integration tests can reference the correct assembly for host building
public partial class Program { }
