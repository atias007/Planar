namespace Planar.API.Common.Entities
{
    public class JobOrTriggerKey
    {
        public string Id { get; set; }

        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrEmpty(Id);
            }
        }
    }
}