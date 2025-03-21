using Planar.API.Common.Entities;
using Planar.Service.Model;
using Riok.Mapperly.Abstractions;

namespace Planar.Service.MapperProfiles;

[Mapper]
public partial class JobLastRunMapper
{
    public partial JobLastRun MapJobLastRun(HistoryLastLog entity);
}