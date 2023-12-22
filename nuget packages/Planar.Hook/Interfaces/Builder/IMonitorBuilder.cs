using System;

namespace Planar.Hook
{
    public interface IMonitorBuilder<T> where T : class
    {
        T WithEventId(int eventId);

        T WithEventTitle(string eventTitle);

        T WithMonitorTitle(string monitorTitle);

        T WithGroup(Action<IMonitorGroupBuilder> groupBuilder);

        T AddUsers(Action<IMonitorUserBuilder> groupBuilder);

        T AddGlobalConfig(string key, string? value);

        T WithException(Exception ex);

        T WithMostInnerException(Exception ex);

        T WithMostInnerExceptionMessage(string message);

        T WithEnvironment(string environment);

        T AddTestUser();
    }
}