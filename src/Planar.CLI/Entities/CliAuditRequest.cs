using Planar.API.Common.Entities;
using Planar.CLI.Attributes;

namespace Planar.CLI.Entities;

public class CliAuditRequest : CliJobKey, IPagingRequest
{
    private readonly CliPagingRequest _paging = new();

    public CliAuditRequest()
    {
    }

    [ActionProperty("pn", "page-number")]
    public int? PageNumber
    {
        get { return _paging.PageNumber; }
        set { _paging.PageNumber = value; }
    }

    [ActionProperty("ps", "page-size")]
    public int? PageSize
    {
        get { return _paging.PageSize; }
        set { _paging.PageSize = value; }
    }

    public void SetPagingDefaults()
    {
        _paging.SetPagingDefaults();
    }
}