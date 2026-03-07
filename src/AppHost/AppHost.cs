using Projects;

var builder = DistributedApplication.CreateBuilder(args);



var dbServer = builder.AddSqlServer("alpineBitsSqlServer").WithLifetime(ContainerLifetime.Persistent);
var dbDatabase = dbServer.AddDatabase("alpineBitsDatabase");

builder.AddProject<AlpineBits_GuestRequestProxy>("alpinebits")
    .WithExternalHttpEndpoints()
    .WithReference(dbDatabase, "DefaultConnection")
    .WaitFor(dbDatabase);


builder.Build().Run();
