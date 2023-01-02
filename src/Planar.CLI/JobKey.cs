namespace Planar.CLI
{
    internal struct JobKey
    {
        public JobKey(string group, string name)
        {
            Name = name;
            Group = group;
        }

        public string Name { get; private set; }
        public string Group { get; private set; }

        public static JobKey Parse(string fullname)
        {
            var parts = fullname.Split('.');
            if (parts.Length == 2)
            {
                var result = new JobKey(parts[0], parts[1]);
                return result;
            }

            throw new CliException($"Fail to parse '{fullname}' to job key");
        }
    }
}