using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Service.Data;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Planar.Service.List.Base
{
    public class BaseJobListenerWithDataLayer<T> : BaseListener<T>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public BaseJobListenerWithDataLayer(IServiceScopeFactory serviceScopeFactory, ILogger<T> logger)
            : base(logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task ExecuteDal(Expression<Func<DataLayer, Task>> exp)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var dal = scope.ServiceProvider.GetRequiredService<DataLayer>();
                await exp.Compile().Invoke(dal);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error initialize/Execute DataLayer at BaseJobListenerWithDataLayer");
                throw;
            }
        }
    }
}