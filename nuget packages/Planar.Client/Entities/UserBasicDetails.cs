namespace Planar.Client.Entities
{
    public class UserBasicDetails : UserMostBasicDetails
    {
#if NETSTANDARD2_0
        public string EmailAddress1 { get; set; }
        public string PhoneNumber1 { get; set; }
#else
        public string? EmailAddress1 { get; set; }
        public string? PhoneNumber1 { get; set; }
#endif
        public int Id { get; set; }
    }
}