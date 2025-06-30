using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Ev_ServiceBus_Samples_Sender>("Sender");
builder.AddProject<Ev_ServiceBus_Samples_Receiver>("Receiver");
builder.Build().Run();