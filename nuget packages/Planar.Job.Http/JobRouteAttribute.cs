namespace Planar.Job.Http
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class JobRouteAttribute : Attribute
    {
        public string Route { get; }

        public JobRouteAttribute(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
            {
                throw new ArgumentNullException(nameof(route), "Route cannot be null or whitespace.");
            }

            Route = route;
        }
    }
}