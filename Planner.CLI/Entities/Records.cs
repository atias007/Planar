using System;
using System.Collections.Generic;

namespace Planner.CLI.Entities
{
    public record UserRowDetails(int Id, string FirstName, string LastName, string Username, string EmailAddress1, string PhoneNumber1);

    public record GroupRowDetails(int Id, string Name);

    public record RunningInfo(string Information, IEnumerable<Exception> Exceptions);
}