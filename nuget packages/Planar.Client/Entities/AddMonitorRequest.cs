namespace Planar.Client.Entities
{
    public class AddMonitorRequest
    {
#if NETSTANDARD2_0
        public string Title { get; set; }

        public string JobName { get; set; }

        public string JobGroup { get; set; }

        public string Event { get; set; }

        public string EventArgument { get; set; }

        public string GroupName { get; set; }

        public string Hook { get; set; }
#else
         public string Title { get; set; } = null!;

         public string? JobName { get; set; }

         public string? JobGroup { get; set; }

         public string EventName { get; set; } = null!;

         public string? EventArgument { get; set; }

         public string GroupName { get; set; } = null!;

         public string Hook { get; set; } = null!;
#endif
    }
}