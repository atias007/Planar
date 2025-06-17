using System;

namespace Planar.Hook
{
    public interface IMonitorBuilder<T> where T : class
    {
        T WithEventId(int eventId);

        T WithEventTitle(string eventTitle);

        T WithMonitorTitle(string monitorTitle);

        // TODO:
        // T WithGroup(Action<IMonitorGroupBuilder> groupBuilder);

        // TODO:
        // T AddUsers(Action<IMonitorUserBuilder> groupBuilder);

#if NETSTANDARD2_0

        T AddGlobalConfig(string key, string value);

#else
        T AddGlobalConfig(string key, string? value);
#endif

        T WithException(Exception ex);

        T WithMostInnerException(Exception ex);

        T WithMostInnerExceptionMessage(string message);

        T WithEnvironment(string environment);

        // TODO:
        //T AddTestUser();
    }
}