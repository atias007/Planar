// See https://aka.ms/new-console-template for more information
using Seq.Api;

Console.WriteLine("Hello, World!");
var connection = new SeqConnection("http://localhost:5341");

var g = await connection.AlertState.ListAsync();

Console.WriteLine();