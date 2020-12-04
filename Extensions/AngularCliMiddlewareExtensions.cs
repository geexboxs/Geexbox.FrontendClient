using System;
using Geexbox.FrontendClient;

namespace Microsoft.AspNetCore.SpaServices.AngularCli
{
    public static class AngularCliMiddlewareExtensions
    {
        public static void UseAngularCliServer(this ISpaBuilder spaBuilder, string npmScript)
        {
            if (spaBuilder == null)
                throw new ArgumentNullException(nameof(spaBuilder));
            if (string.IsNullOrEmpty(spaBuilder.Options.SourcePath))
                throw new InvalidOperationException("To use UseAngularCliServer, you must supply a non-empty value for the SourcePath property of SpaOptions when calling UseSpa.");
            AngularCliMiddleware.Attach(spaBuilder, npmScript);
        }
    }
}