Console.WriteLine("Hello, Process Job!");
for (int i = 0; i < 20; i++)
{
    Console.WriteLine($"Sleep {i + 1}");
    Console.WriteLine($"<<planar.effectedrows:{i}>>");
    Thread.Sleep(1000);
}

var rows = new Random().Next(10, 100);
Console.WriteLine($"<<planar.effectedrows:{rows}>>");
Console.WriteLine("Bye Bye, Process Job!");