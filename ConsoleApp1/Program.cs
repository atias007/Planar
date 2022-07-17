using Grpc.Net.Client;
using Planar;

var channel = GrpcChannel.ForAddress("http://localhost:9999");
var client = new PlanarCluster.PlanarClusterClient(channel);
var reply = await client.HealthCheckAsync(
       new Google.Protobuf.WellKnownTypes.Empty());
Console.WriteLine("from server: " + reply);