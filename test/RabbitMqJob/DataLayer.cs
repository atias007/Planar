namespace RabbitMQJob
{
    internal class DataLayer
    {
        public IEnumerable<Currency> GetCurrency()
        {
            return
            [
                new() { Name = "USD", Rate = 1.0 },
                new() { Name = "EUR", Rate = 0.85 },
                new() { Name = "JPY", Rate = 110.0 }
            ];
        }
    }
}