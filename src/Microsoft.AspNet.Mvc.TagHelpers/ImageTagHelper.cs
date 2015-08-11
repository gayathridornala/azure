// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.Razor.TagHelpers;
using Microsoft.AspNet.Mvc.TagHelpers.Internal;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    /// <summary>
    /// <see cref="ITagHelper"/> implementation targeting &lt;img&gt; elements that supports file versioning.
    /// </summary>
    /// <remarks>
    /// The tag helper won't process for cases with just the 'src' attribute.
    /// </remarks>
    [TargetElement(
        "img",
        Attributes = AppendVersionAttributeName + "," + SrcAttributeName,
        TagStructure = TagStructure.WithoutEndTag)]
    public class ImageTagHelper : UrlResolutionTagHelper
    {
        private static readonly string Namespace = typeof(ImageTagHelper).Namespace;

        private const string AppendVersionAttributeName = "asp-append-version";
        private const string SrcAttributeName = "src";

        private FileVersionProvider _fileVersionProvider;

        /// <summary>
        /// Creates a new <see cref="ImageTagHelper"/>.
        /// </summary>
        /// <param name="hostingEnvironment">The <see cref="IHostingEnvironment"/>.</param>
        /// <param name="cache">The <see cref="IMemoryCache"/>.</param>
        /// <param name="urlHelper">The <see cref="IUrlHelper"/>.</param>
        public ImageTagHelper(
            IHostingEnvironment hostingEnvironment,
            IMemoryCache cache,
            IHtmlEncoder htmlEncoder,
            IUrlHelper urlHelper)
            : base(urlHelper, htmlEncoder)
        {
            HostingEnvironment = hostingEnvironment;
            Cache = cache;
        }

        /// <summary>
        /// Source of the image.
        /// </summary>
        /// <remarks>
        /// Passed through to the generated HTML in all cases.
        /// </remarks>
        [HtmlAttributeName(SrcAttributeName)]
        public string Src { get; set; }

        /// <summary>
        /// Value indicating if file version should be appended to the src urls.
        /// </summary>
        /// <remarks>
        /// If <c>true</c> then a query string "v" with the encoded content of the file is added.
        /// </remarks>
        [HtmlAttributeName(AppendVersionAttributeName)]
        public bool AppendVersion { get; set; }

        protected IHostingEnvironment HostingEnvironment { get; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        protected IMemoryCache Cache { get; }

        /// <inheritdoc />
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (AppendVersion)
            {
                EnsureFileVersionProvider();

                string resolvedUrl;
                if (TryResolveUrl(Src, encodeWebRoot: false, resolvedUrl: out resolvedUrl))
                {
                    Src = resolvedUrl;
                }
                output.Attributes[SrcAttributeName] = _fileVersionProvider.AddFileVersionToPath(Src);
            }
            else
            {
                // Pass through attribute that is also a well-known HTML attribute.
                output.CopyHtmlAttribute(SrcAttributeName, context);
                ProcessUrlAttribute(SrcAttributeName, output);
            }
        }

        private void EnsureFileVersionProvider()
        {
            if (_fileVersionProvider == null)
            {
                _fileVersionProvider = new FileVersionProvider(
                    HostingEnvironment.WebRootFileProvider,
                    Cache,
                    ViewContext.HttpContext.Request.PathBase);
            }
        }
    }
}