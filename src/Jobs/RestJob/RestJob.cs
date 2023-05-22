using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Common.Helpers;
using Quartz;
using RestSharp;

namespace Planar
{
    public class RestJob : BaseCommonJob<RestJob, RestJobProperties>
    {
        public RestJob(ILogger<RestJob> logger, IJobPropertyDataLayer dataLayer) : base(logger, dataLayer)
        {
        }

        public override async Task Execute(IJobExecutionContext context)
        {
            try
            {
                await Initialize(context);
                ValidateRestJob();
                var task = Task.Run(() => ExecuteRest(context));
                await WaitForJobTask(context, task);
            }
            catch (Exception ex)
            {
                var metadata = JobExecutionMetadata.GetInstance(context);
                metadata.UnhandleException = ex;
            }
            finally
            {
                FinalizeJob(context);
            }
        }

        private async Task ExecuteRest(IJobExecutionContext context)
        {
            var uri = new Uri(Properties.Url, UriKind.Absolute);
            var timeout = TriggerHelper.GetTimeout(context.Trigger) ?? TimeSpan.FromMinutes(30);

            var options = new RestClientOptions
            {
                MaxTimeout = Convert.ToInt32(timeout.TotalMilliseconds),
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                {
                    return Properties.IgnoreSslErrors;
                }
            };

            var client = new RestClient(options);

            var request = new RestRequest
            {
                Resource = uri.ToString(),
                Method = Enum.Parse<Method>(Properties.Method, ignoreCase: true)
            };

            foreach (var h in Properties.Headers)
            {
                request.AddHeader(h.Key, h.Value ?? string.Empty);
            }

            foreach (var h in Properties.FormData)
            {
                request.AlwaysMultipartFormData = true;
                request.AddParameter(h.Key, h.Value);
            }

            var response = await client.ExecuteAsync(request);
            MessageBroker.AppendLog(LogLevel.Information, $"Status Code: {response.StatusCode}");
            MessageBroker.AppendLog(LogLevel.Information, $"Status Description: {response.StatusDescription}");
            MessageBroker.AppendLog(LogLevel.Information, $"Response Uri: {response.ResponseUri}");
            MessageBroker.AppendLog(LogLevel.Information, $"Response Content: {response.Content}");

            if (!response.IsSuccessful)
            {
                MessageBroker.AppendLog(LogLevel.Error, $"Response fail");
                MessageBroker.AppendLog(LogLevel.Error, $"Error Message: {response.ErrorMessage}");

                if (response.ErrorException == null)
                {
                    throw new RestJobException($"Rest job fail with response status code: {response.StatusCode} {response.StatusDescription}");
                }
                else
                {
                    throw new RestJobException("Rest job fail", response.ErrorException);
                }
            }
        }

        private void ValidateRestJob()
        {
            try
            {
                ValidateMandatoryString(Properties.Path, nameof(Properties.Path));
                var bodyFullname = FolderConsts.GetSpecialFilePath(
                    PlanarSpecialFolder.Jobs,
                    Properties.Path ?? string.Empty,
                    Properties.BodyFile ?? string.Empty);

                if (!string.IsNullOrEmpty(Properties.BodyFile) && !File.Exists(bodyFullname))
                {
                    throw new RestJobException($"body file '{bodyFullname}' could not be found");
                }
            }
            catch (Exception ex)
            {
                var source = nameof(ValidateRestJob);
                _logger.LogError(ex, "Fail at {Source}", source);
                MessageBroker.AppendLog(LogLevel.Error, $"Fail at {source}. {ex.Message}");
                throw;
            }
        }
    }
}