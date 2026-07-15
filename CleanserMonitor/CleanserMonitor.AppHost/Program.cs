var builder = DistributedApplication.CreateBuilder(args);
var web = builder.AddProject<Projects.CleanserBlazorUI>("CleanserBlazorUI");

builder.Build().Run();
