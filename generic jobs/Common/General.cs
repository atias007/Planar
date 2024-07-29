using Microsoft.Extensions.Configuration;

namespace Common;

public sealed class General
{
    public General(IConfiguration configuration)
    {
        var section = configuration.GetSection("general");
        MaxDegreeOfParallelism = section.GetValue<int?>("max degree of parallelism") ?? 10;
        SequentialProcessing = section.GetValue<bool?>("sequential processing") ?? false;
        StopRunningOnFail = section.GetValue<bool?>("stop running on fail") ?? false;
    }

    public int MaxDegreeOfParallelism { get; private set; }
    public bool SequentialProcessing { get; private set; }

    public bool StopRunningOnFail { get; private set; }

    override public string ToString()
    {
        return SequentialProcessing ?
            $"processing mode: sequential, stop running on fail: {StopRunningOnFail}" :
            $"processing mode: parallel with max degree of {MaxDegreeOfParallelism}";
    }
}