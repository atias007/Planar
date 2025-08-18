// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Minor Code Smell", "S3251:Implementations should be provided for \"partial\" methods", Justification = "PARTIAL", Scope = "member", Target = "~M:Common.BaseCheckJob.SafeInvokeCheck``1(``0,System.Func{``0,System.Threading.Tasks.Task},Planar.Job.ITriggerDetail)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Minor Code Smell", "S3251:Implementations should be provided for \"partial\" methods", Justification = "PARTIAL", Scope = "member", Target = "~M:Common.BaseCheckJob.OnFail``1(``0,System.Exception)")]