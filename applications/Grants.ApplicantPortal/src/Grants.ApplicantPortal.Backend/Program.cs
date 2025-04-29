using Microsoft.EntityFrameworkCore;
using MediatR;
using Grants.ApplicantPortal.Backend;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Entity Framework Core with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add MediatR
builder.Services.AddMediatR(typeof(Program).Assembly);

// Add CORS policy - restrict to only allow the frontend service
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // In Kubernetes, only allow requests from the frontend-service
        policy.WithOrigins(
                // Allow the frontend service by name in Kubernetes
                "http://frontend-service",
                "https://frontend-service",
                // For local development
                "http://localhost:4000", 
                "http://localhost:4200"
            )
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Load backend URL from configuration
builder.WebHost.UseUrls("http://0.0.0.0:5100");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Use CORS middleware
app.UseCors();

app.MapControllers();
app.MapGet("/healthz", () => Results.Text("Service is operational", "text/plain"));
app.MapGet("/healthz/ready", (HttpContext context) => {
    context.Response.Headers.Append("content-type", "text/plain");
    context.Response.Headers.Append("readiness", "healthy");
    return Results.Text("Service is ready", "text/plain");
});

await app.RunAsync();

namespace Grants.ApplicantPortal.Backend
{
    // Define ApplicationDbContext
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Define DbSets for your entities here
    }
}
