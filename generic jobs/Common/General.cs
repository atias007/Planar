using Microsoft.Extensions.Configuration;

namespace Common;

public sealed class General
{
    public General(IConfiguration configuration)
    {
        var section = configuration.GetSection("general");
        MaxDegreeOfParallelism = section.GetValue<int?>("max degree of parallelism") ?? 10;
        SequentialProcessing = section.GetValue<bool?>("sequential processing") ?? false;
    }

    public int MaxDegreeOfParallelism { get; private set; }
    public bool SequentialProcessing { get; private set; }

    override public string ToString()
    {
        return SequentialProcessing ?
            "processing mode: sequential" :
            $"processing mode: parallel with max degree of {MaxDegreeOfParallelism}";
    }
}