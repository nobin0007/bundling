﻿using System;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Internal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Karambolo.AspNetCore.Bundling
{
    public class BundlingMiddleware
    {
        readonly RequestDelegate _next;
        readonly IBundleManager _bundleManager;
        readonly StaticFileMiddleware _staticFileMiddleware;

        public BundlingMiddleware(RequestDelegate next, IHostingEnvironment env, ILoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor,
            IOptions<BundleGlobalOptions> globalOptions, IBundleManagerFactory bundleManagerFactory, BundleCollection bundles, IOptions<BundlingOptions> options)
        {
            if (next == null)
                throw new ArgumentNullException(nameof(next));

            if (env == null)
                throw new ArgumentNullException(nameof(env));

            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            if (httpContextAccessor == null)
                throw new ArgumentNullException(nameof(httpContextAccessor));

            if (globalOptions == null)
                throw new ArgumentNullException(nameof(globalOptions));

            if (bundleManagerFactory == null)
                throw new ArgumentNullException(nameof(bundleManagerFactory));

            if (bundles == null)
                throw new ArgumentNullException(nameof(bundles));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _next = next;

            var optionsUnwrapped = options.Value;

            _bundleManager = optionsUnwrapped.BundleManager ?? bundleManagerFactory.Create(bundles, new BundlingContext
            {
                BundlesPathPrefix = optionsUnwrapped.RequestPath,
                StaticFilesPathPrefix = optionsUnwrapped.StaticFilesRequestPath
            });

            optionsUnwrapped.FileProvider = optionsUnwrapped.FileProvider ?? new BundleFileProvider(_bundleManager, httpContextAccessor);

            var globalOptionsUnwrapped = globalOptions.Value;
            if (globalOptionsUnwrapped.EnableCacheHeader)
            {
                var originalPrepareResponse = optionsUnwrapped.OnPrepareResponse;
                optionsUnwrapped.OnPrepareResponse = ctx =>
                {
                    var headers = ctx.Context.Response.GetTypedHeaders();
                    headers.CacheControl = new CacheControlHeaderValue { MaxAge = globalOptionsUnwrapped.CacheHeaderMaxAge };
                    originalPrepareResponse?.Invoke(ctx);
                };
            }

            _staticFileMiddleware = new StaticFileMiddleware(next, env, options, loggerFactory);
        }

        public async Task Invoke(HttpContext context)
        {
            if (await _bundleManager.TryEnsureUrlAsync(context))
                await _staticFileMiddleware.Invoke(context);
            else
                await _next(context);
        }
    }
}
