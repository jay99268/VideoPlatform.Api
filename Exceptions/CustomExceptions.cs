using System;

namespace VideoPlatform.Api.Exceptions
{
    /// <summary>
    /// 表示“未找到”错误的自定义异常 (对应 HTTP 404)
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// 表示“错误请求”的自定义异常 (对应 HTTP 400)
    /// </summary>
    public class BadRequestException : Exception
    {
        public BadRequestException(string message) : base(message)
        {
        }
    }
}