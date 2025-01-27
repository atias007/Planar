using Common;
using Seq.Api.Model.Alerting;

namespace SeqAlertsCheck;

internal class SeqAlert : BaseDefault, ICheckElement, IVetoEntity
{
    public SeqAlert(AlertStateEntity state, Defaults defaults) : base(defaults)
    {
        Key = state.AlertId;
        Title = state.Title;
        AlertState = state;
    }

    public string Key { get; private set; } = string.Empty;
    public string Title { get; private set; }
    public AlertStateEntity AlertState { get; private set; }
}