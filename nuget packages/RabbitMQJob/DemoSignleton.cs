namespace RabbitMQJob
{
    internal class DemoSignleton
    {
        public DemoSignleton()
        {
            CreateDate = DateTime.Now;
        }

        public DateTime CreateDate { get; private set; }
    }
}