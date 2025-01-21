using MailKit.Net.Smtp;
using MimeKit;
using Planar.Common;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Planar;

internal static class SmtpUtil
{
    public static async Task<string> SendMessage(MimeMessage message, CancellationToken cancellationToken = default)
    {
        var smtp = AppSettings.Smtp;

        if (message.From.Count == 0)
        {
            message.From.Add(new MailboxAddress(smtp.FromName, smtp.FromAddress));
        }

        using var client = new SmtpClient();
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        tokenSource.CancelAfter(TimeSpan.FromSeconds(30));

        await client.ConnectAsync(
            host: smtp.Host,
            port: smtp.Port,
            useSsl: smtp.UseSsl,
            cancellationToken: tokenSource.Token);

        if (!smtp.UseDefaultCredentials)
        {
            await client.AuthenticateAsync(encoding: Encoding.UTF8, smtp.Username, smtp.Password, tokenSource.Token);
        }

        var result = await client.SendAsync(message, tokenSource.Token);
        await client.DisconnectAsync(quit: true, tokenSource.Token);
        return result;
    }
}