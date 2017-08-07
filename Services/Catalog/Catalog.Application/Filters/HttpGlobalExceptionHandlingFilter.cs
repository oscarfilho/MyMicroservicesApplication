﻿using Catalog.Application.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Filters
{
    public class HttpGlobalExceptionHandlingFilter : IExceptionFilter
    {
        private readonly ILogger<HttpGlobalExceptionHandlingFilter> _logger;
        
        public HttpGlobalExceptionHandlingFilter(ILogger<HttpGlobalExceptionHandlingFilter> logger)
        {
            _logger = logger;
        }
        
        public void OnException(ExceptionContext context)
        {
            _logger.LogError(
                new EventId(context.Exception.HResult), 
                context.Exception, 
                context.Exception.Message
            );
        }
    }
}