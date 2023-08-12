using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Tests
{
    internal class FakeRemoteIpAddressMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IPAddress fakeIpAddress = IPAddress.Parse("127.0.0.1");

        public FakeRemoteIpAddressMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            httpContext.Connection.RemoteIpAddress = fakeIpAddress;
            await _next(httpContext);
        }
    }
}
