using MVFC.Aspire.Helpers.Mongo;

var builder = DistributedApplication.CreateBuilder(args);

var mongo = builder.AddMongoReplicaSet("mongo")
                   .WithDataVolume("mongo-data");

var worker = builder.AddProject<Projects.MVFC_Veragi_Simulator_WebhookWorker>("webhook-worker", launchProfileName: "http");

builder.AddProject<Projects.MVFC_Veragi_Simulator_Api>("provider", launchProfileName: "http")
       .WithReference(mongo, "ConnectionStrings:simulator")
       .WaitFor(mongo)
       .WaitFor(worker);

await builder.Build().RunAsync();
