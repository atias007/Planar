﻿namespace LongRunningProcess
{
    internal class Program
    {
        private static void Main()
        {
            var counter = 180;
            while (counter > 0)
            {
                Console.WriteLine($"Counter: {counter}");
                Thread.Sleep(10000);
                counter--;
            }
        }
    }
}