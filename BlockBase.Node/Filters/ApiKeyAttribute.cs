using System;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace BlockBase.Node.Filters
{

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAttribute : Attribute, IAsyncActionFilter
    {
        private ApiSecurityConfigurations _apiSecurityConfigurations;
        public ApiKeyAttribute(IOptions<ApiSecurityConfigurations> apiSecurityConfigurations)
        {
            _apiSecurityConfigurations = apiSecurityConfigurations?.Value;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {

            if (_apiSecurityConfigurations != null 
                && _apiSecurityConfigurations.Use
                && (!context.HttpContext.Request.Headers.TryGetValue("ApiKey", out var potentialApiKey) || _apiSecurityConfigurations.ApiKey != potentialApiKey))
            {
                context.Result = new UnauthorizedResult();
                return;
            }
            
            await next();

        }
    }
}