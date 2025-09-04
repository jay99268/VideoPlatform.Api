using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using VideoPlatform.Api.DTOs;
using VideoPlatform.Api.Exceptions;

namespace VideoPlatform.Api.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception has occurred.");

            var statusCode = HttpStatusCode.InternalServerError; // 默认 500
            var message = "服务器发生内部错误，请稍后重试。";
            string details = null;

            switch (exception)
            {
                case NotFoundException e:
                    statusCode = HttpStatusCode.NotFound;
                    message = e.Message;
                    break;
                case BadRequestException e:
                    statusCode = HttpStatusCode.BadRequest;
                    message = e.Message;
                    break;
                    // 这里可以根据需要添加更多自定义异常的处理
            }

            // 在开发环境中，为了便于调试，可以返回详细的异常信息
            if (_env.IsDevelopment())
            {
                details = exception.ToString();
            }

            var response = new ErrorResponseDto
            {
                Message = message,
                Details = details // 在生产环境中，此字段为 null
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}