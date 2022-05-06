// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Planar BL Standart", Scope = "member", Target = "~M:Planar.Service.API.JobDomain.PauseAll~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Planar BL Standart", Scope = "member", Target = "~M:Planar.Service.API.JobDomain.Pause(Planar.API.Common.Entities.JobOrTriggerKey)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Planar BL Standart", Scope = "member", Target = "~M:Planar.Service.API.JobDomain.ResumeAll~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Planar BL Standart", Scope = "member", Target = "~M:Planar.Service.API.JobDomain.Resume(Planar.API.Common.Entities.JobOrTriggerKey)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Planar BL Standart", Scope = "member", Target = "~M:Planar.Service.API.JobDomain.Stop(Planar.API.Common.Entities.FireInstanceIdRequest)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Planar BL Standart", Scope = "member", Target = "~M:Planar.Service.API.JobDomain.GetSettings(System.String)~System.Threading.Tasks.Task{System.Collections.Generic.Dictionary{System.String,System.String}}")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Planar BL Standart", Scope = "member", Target = "~M:Planar.Service.API.JobDomain.RemoveData(System.String,System.String)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Planar BL Standart", Scope = "member", Target = "~M:Planar.Service.API.JobDomain.GetRunningInfo(System.String)~System.Threading.Tasks.Task{Planar.API.Common.Entities.GetRunningInfoResponse}")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Planar BL Standart", Scope = "member", Target = "~M:Planar.Service.API.JobDomain.GetRunning(System.String)~System.Threading.Tasks.Task{System.Collections.Generic.List{Planar.API.Common.Entities.RunningJobDetails}}")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Planar BL Standart", Scope = "member", Target = "~M:Planar.Service.API.JobDomain.ClearData(System.String)~System.Threading.Tasks.Task{Planar.API.Common.Entities.BaseResponse}")] 