using Common;

namespace SeqAlertsCheck;

internal class SeqAlert : BaseDefault, ICheckElement, IVetoEntity
{
    public SeqAlert(AlertState state, Defaults defaults) : base(defaults)
    {
        Key = state.AlertId;
        Title = state.Title;
        AlertState = state;
    }

    public string Key { get; private set; }
    public string Title { get; set; }
    public AlertState AlertState { get; private set; }
}