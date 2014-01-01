/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Rhino.CommonJS.Module.Provider;
using Sharpen;

namespace Rhino.CommonJS.Module.Provider
{
	/// <summary>
	/// A URL-based script provider that can load modules against a set of baseUri
	/// privileged and fallback URIs.
	/// </summary>
	/// <remarks>
	/// A URL-based script provider that can load modules against a set of baseUri
	/// privileged and fallback URIs. It is deliberately not named "URI provider"
	/// but a "URL provider" since it actually only works against those URIs that
	/// are URLs (and the JRE has a protocol handler for them). It creates cache
	/// validators that are suitable for use with both file: and http: URL
	/// protocols. Specifically, it is able to use both last-modified timestamps and
	/// ETags for cache revalidation, and follows the HTTP cache expiry calculation
	/// model, and allows for fallback heuristic expiry calculation when no server
	/// specified expiry is provided.
	/// </remarks>
	/// <author>Attila Szegedi</author>
	/// <version>$Id: UrlModuleSourceProvider.java,v 1.4 2011/04/07 20:26:12 hannes%helma.at Exp $</version>
	[Serializable]
	public class UrlModuleSourceProvider : ModuleSourceProviderBase
	{
		private readonly IEnumerable<Uri> privilegedUris;

		private readonly IEnumerable<Uri> fallbackUris;

		private readonly UrlConnectionSecurityDomainProvider urlConnectionSecurityDomainProvider;

		private readonly UrlConnectionExpiryCalculator urlConnectionExpiryCalculator;

		/// <summary>
		/// Creates a new module script provider that loads modules against a set of
		/// privileged and fallback URIs.
		/// </summary>
		/// <remarks>
		/// Creates a new module script provider that loads modules against a set of
		/// privileged and fallback URIs. It will use a fixed default cache expiry
		/// of 60 seconds, and provide no security domain objects for the resource.
		/// </remarks>
		/// <param name="privilegedUris">
		/// an iterable providing the privileged URIs. Can be
		/// null if no privileged URIs are used.
		/// </param>
		/// <param name="fallbackUris">
		/// an iterable providing the fallback URIs. Can be
		/// null if no fallback URIs are used.
		/// </param>
		public UrlModuleSourceProvider(IEnumerable<Uri> privilegedUris, IEnumerable<Uri> fallbackUris) : this(privilegedUris, fallbackUris, new DefaultUrlConnectionExpiryCalculator(), null)
		{
		}

		/// <summary>
		/// Creates a new module script provider that loads modules against a set of
		/// privileged and fallback URIs.
		/// </summary>
		/// <remarks>
		/// Creates a new module script provider that loads modules against a set of
		/// privileged and fallback URIs. It will use the specified heuristic cache
		/// expiry calculator and security domain provider.
		/// </remarks>
		/// <param name="privilegedUris">
		/// an iterable providing the privileged URIs. Can be
		/// null if no privileged URIs are used.
		/// </param>
		/// <param name="fallbackUris">
		/// an iterable providing the fallback URIs. Can be
		/// null if no fallback URIs are used.
		/// </param>
		/// <param name="urlConnectionExpiryCalculator">
		/// the calculator object for heuristic
		/// calculation of the resource expiry, used when no expiry is provided by
		/// the server of the resource. Can be null, in which case the maximum age
		/// of cached entries without validation will be zero.
		/// </param>
		/// <param name="urlConnectionSecurityDomainProvider">
		/// object that provides security
		/// domain objects for the loaded sources. Can be null, in which case the
		/// loaded sources will have no security domain associated with them.
		/// </param>
		public UrlModuleSourceProvider(IEnumerable<Uri> privilegedUris, IEnumerable<Uri> fallbackUris, UrlConnectionExpiryCalculator urlConnectionExpiryCalculator, UrlConnectionSecurityDomainProvider urlConnectionSecurityDomainProvider)
		{
			this.privilegedUris = privilegedUris;
			this.fallbackUris = fallbackUris;
			this.urlConnectionExpiryCalculator = urlConnectionExpiryCalculator;
			this.urlConnectionSecurityDomainProvider = urlConnectionSecurityDomainProvider;
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Sharpen.URISyntaxException"></exception>
		protected internal override ModuleSource LoadFromPrivilegedLocations(string moduleId, object validator)
		{
			return LoadFromPathList(moduleId, validator, privilegedUris);
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Sharpen.URISyntaxException"></exception>
		protected internal override ModuleSource LoadFromFallbackLocations(string moduleId, object validator)
		{
			return LoadFromPathList(moduleId, validator, fallbackUris);
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Sharpen.URISyntaxException"></exception>
		private ModuleSource LoadFromPathList(string moduleId, object validator, IEnumerable<Uri> paths)
		{
			if (paths == null)
			{
				return null;
			}
			foreach (Uri path in paths)
			{
				ModuleSource moduleSource = LoadFromUri(path.Resolve(moduleId), path, validator);
				if (moduleSource != null)
				{
					return moduleSource;
				}
			}
			return null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Sharpen.URISyntaxException"></exception>
		protected internal override ModuleSource LoadFromUri(Uri uri, Uri @base, object validator)
		{
			// We expect modules to have a ".js" file name extension ...
			Uri fullUri = new Uri(uri + ".js");
			ModuleSource source = LoadFromActualUri(fullUri, @base, validator);
			// ... but for compatibility we support modules without extension,
			// or ids with explicit extension.
			return source ?? LoadFromActualUri(uri, @base, validator);
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected virtual ModuleSource LoadFromActualUri(Uri uri, Uri baseUri, object validator)
		{
			Uri url = baseUri == null ? uri : new Uri(baseUri, uri);
			long requestTime = DateTime.UtcNow.ToMillisecondsSinceEpoch();
			var request = (HttpWebRequest) WebRequest.Create(url);

			URLConnection urlConnection = url.OpenConnection();

			var urlValidator = validator as UrlValidator;
			UrlValidator applicableValidator = urlValidator != null && urlValidator.AppliesTo(uri) ? urlValidator : null;
			if (applicableValidator != null)
				applicableValidator.ApplyConditionals(request);

			var response = (HttpWebResponse) request.GetResponse();
			if (applicableValidator != null && applicableValidator.UpdateValidator(response, requestTime, urlConnectionExpiryCalculator))
			{
				response.Close();
				return ModuleSourceProviderConstants.NOT_MODIFIED;
			}

			return new ModuleSource(GetReader(response), GetSecurityDomain(urlConnection), uri, baseUri, new UrlValidator(uri, response, requestTime, urlConnectionExpiryCalculator));
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static StreamReader GetReader(WebResponse response)
		{
			var stream = response.GetResponseStream();
			if (stream != null) return new StreamReader(stream, Encoding.GetEncoding(GetCharacterEncodingName(response)));
			return new StreamReader(new MemoryStream());
		}

		private static string GetCharacterEncodingName(WebResponse response)
		{
			var pct = new ParsedContentType(response.ContentType);
			string encoding = pct.GetEncoding();
			if (encoding != null)
				return encoding;
			string contentType = pct.GetContentType();
			return contentType != null && contentType.StartsWith("text/")
				? "8859_1"
				: "utf-8";
		}

		private object GetSecurityDomain(URLConnection urlConnection)
		{
			return urlConnectionSecurityDomainProvider == null ? null : urlConnectionSecurityDomainProvider.GetSecurityDomain(urlConnection);
		}

		protected internal override bool EntityNeedsRevalidation(object validator)
		{
			return !(validator is UrlValidator) || ((UrlValidator)validator).EntityNeedsRevalidation();
		}

		[Serializable]
		private sealed class UrlValidator
		{
			private readonly Uri _uri;

			private readonly DateTime lastModified;

			private readonly string entityTags;

			private long expiry;

			public UrlValidator(Uri uri, HttpWebResponse response, long requestTime, UrlConnectionExpiryCalculator urlConnectionExpiryCalculator)
			{
				_uri = uri;
				lastModified = response.LastModified;
				entityTags = response.Headers.Get("ETag");
				expiry = CalculateExpiry(response, requestTime, urlConnectionExpiryCalculator);
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal bool UpdateValidator(HttpWebResponse response, long requestTime, UrlConnectionExpiryCalculator urlConnectionExpiryCalculator)
			{
				if (response.StatusCode != HttpStatusCode.NotModified)
				{
					expiry = CalculateExpiry(response, requestTime, urlConnectionExpiryCalculator);
				}
				return response.StatusCode == HttpStatusCode.NotModified;
			}

			private static long CalculateExpiry(HttpWebResponse response, long requestTime, UrlConnectionExpiryCalculator urlConnectionExpiryCalculator)
			{
				if (string.Equals("no-cache", response.Headers ["Pragma"]))
					return 0L;


				string cacheControl = response.Headers["Cache-Control"];
				if (cacheControl != null)
				{
					if (cacheControl.IndexOf("no-cache", StringComparison.Ordinal) != -1)
						return 0L;
					
					int maxAge = GetMaxAge(cacheControl);
					if (-1 != maxAge)
					{
						//TODO: fix me
						/*
						long responseTime = DateTime.UtcNow.ToMillisecondsSinceEpoch();
						long apparentAge = Math.Max(0, responseTime - response.GetDate());
						long correctedReceivedAge = Math.Max(apparentAge, response.GetHeaderFieldInt("Age", 0) * 1000L);
						long responseDelay = responseTime - requestTime;
						long correctedInitialAge = correctedReceivedAge + responseDelay;
						long creationTime = responseTime - correctedInitialAge;
						return maxAge * 1000L + creationTime;
						 */
					}
				}
				/*
				long explicitExpiry = response.Headers["Expires"].GetHeaderFieldDate("Expires", -1L);
				if (explicitExpiry != -1L)
				{
					return explicitExpiry;
				} 
				 */
				return urlConnectionExpiryCalculator == null ? 0L : urlConnectionExpiryCalculator.CalculateExpiry(response);
			}

			private static int GetMaxAge(string cacheControl)
			{
				int maxAgeIndex = cacheControl.IndexOf("max-age", StringComparison.Ordinal);
				if (maxAgeIndex == -1)
					return -1;

				int eq = cacheControl.IndexOf('=', maxAgeIndex + 7);
				if (eq == -1)
					return -1;

				var afterEq = eq + 1;

				var comma = cacheControl.IndexOf(',', afterEq);
				var strAge = comma == -1
					? cacheControl.Substring(afterEq)
					: cacheControl.Substring(afterEq, comma - afterEq);

				int maxAge;
				if (!int.TryParse(strAge, out maxAge))
					return - 1;
				return maxAge;
			}

			internal bool AppliesTo(Uri uri)
			{
				return _uri.Equals(uri);
			}

			internal void ApplyConditionals(HttpWebRequest request)
			{
				if (lastModified != DateTime.MinValue)
					request.IfModifiedSince = lastModified;

				if (!string.IsNullOrEmpty(entityTags))
					request.Headers.Add("If-None-Match", entityTags);
			}

			internal bool EntityNeedsRevalidation()
			{
				return DateTime.UtcNow.ToMillisecondsSinceEpoch() > expiry;
			}
		}
	}
}
