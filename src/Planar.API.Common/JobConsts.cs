using System;
using System.Text.RegularExpressions;

namespace Planar.API.Common;

public static partial class JobConsts
{
    public const string NameRegex = @"^[A-Za-z0-9._ -]+$";
    private const string JobNameRegexTemplate = @"^[A-Za-z0-9_ -]{2,50}$";
    private const string JobKeyRegexTemplate = @"^[A-Za-z0-9_ -]{2,50}\.[A-Za-z0-9_ -]{2,50}$";
    private const string JobIdTemplate = "^[a-z0-9]{11}$";

    public static readonly Regex JobIdRegex = new(JobIdTemplate, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

    public static readonly Regex JobNameRegex = new(
        JobNameRegexTemplate, RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    public static readonly Regex JobKeyRegex = new(
        JobKeyRegexTemplate, RegexOptions.Compiled, TimeSpan.FromSeconds(5));
}