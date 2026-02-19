using Grants.ApplicantPortal.API.Web.Configurations;

var builder = WebApplication.CreateBuilder(args);

// Behind a reverse proxy (HAProxy → Node Express → Kestrel), the proxy buffers
// the full request before forwarding. Kestrel sees a delay between headers and
// body arrival and kills the connection with 408. Disable the minimum data rate
// check so proxy-forwarded POST requests aren't rejected.
builder.WebHost.ConfigureKestrel(options =>
{
  options.Limits.MinRequestBodyDataRate = null;
});

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
