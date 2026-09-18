using System;
using System.Text.RegularExpressions;

namespace Planar.API.Common;

public static class JobConsts
{
    private const string JobNameRegexTemplate = @"^[a-zA-Z0-9\-_\s]{@MinNameLength@,@MaxNameLength@}$";
    private const string JobKeyRegexTemplate = @"^[a-zA-Z0-9\-_\s]{@MinNameLength@,@MaxNameLength@}\.[a-zA-Z0-9\-_\s]{@MinNameLength@,@MaxNameLength@}$";
    private const int JobMaxNameLength = 50;
    private const int JobMinNameLength = 3;
    private const string JobIdTemplate = "^[a-z0-9]{11}$";

    public static readonly Regex JobIdRegex = new(JobIdTemplate, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

    public static readonly Regex JobNameRegex = new(
        JobNameRegexTemplate
        .Replace("@MinNameLength@", JobMinNameLength.ToString())
        .Replace("@MaxNameLength@", JobMaxNameLength.ToString()), RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    public static readonly Regex JobKeyRegex = new(
        JobKeyRegexTemplate
        .Replace("@MinNameLength@", JobMinNameLength.ToString())
        .Replace("@MaxNameLength@", JobMaxNameLength.ToString()), RegexOptions.Compiled, TimeSpan.FromSeconds(5));
}