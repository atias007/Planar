using Planar.CLI.Attributes;
using Planar.CLI.Proxy;
using System;
using System.Web;

namespace Planar.CLI.Entities;

public class CliJobWaitRequest
{
    [ActionProperty("g", "group")]
    public string? Group { get; set; }

    [ActionProperty("j", "job")]
    public string? Id { get; set; }

    public Uri GetQueryParam(string path)
    {
        Group = Group?.Trim();
        Id = Id?.Trim();

        var builder = new UriBuilder(RestProxy.BaseUri)
        {
            Path = path,
        };

        if (!string.IsNullOrEmpty(Group) && !string.IsNullOrEmpty(Id))
        {
            builder.Query = $"id={HttpUtility.UrlEncode(Id)}&group={HttpUtility.UrlEncode(Group)}";
        }

        if (string.IsNullOrEmpty(Group))
        {
            builder.Query = $"id={HttpUtility.UrlEncode(Id)}";
        }

        if (string.IsNullOrEmpty(Id))
        {
            builder.Query = $"group={HttpUtility.UrlEncode(Group)}";
        }

        var uri = builder.Uri;
        return uri;
    }
}