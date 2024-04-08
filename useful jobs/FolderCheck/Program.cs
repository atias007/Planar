// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
var directory = new DirectoryInfo("C:/Users/tsahi_a/source/repos");
var n = directory.EnumerateFiles("*.*", SearchOption.AllDirectories);

Console.WriteLine(n.Sum(f => f.Length));