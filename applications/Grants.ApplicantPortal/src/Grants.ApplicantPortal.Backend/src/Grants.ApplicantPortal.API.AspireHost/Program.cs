var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Grants_ApplicantPortal_API_Web>("web");

builder.Build().Run();
