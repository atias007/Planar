namespace Planar.Service.Model.DataObjects
{
    public enum AnomalyMembers : byte
    {
        Undefined = 255,
        Normalcy = 0,
        DurationAnomaly = 1,
        EffectedRowsAnomaly = 2,
        StoppedJob = 100,
        StatusFail = 101,
    }
}