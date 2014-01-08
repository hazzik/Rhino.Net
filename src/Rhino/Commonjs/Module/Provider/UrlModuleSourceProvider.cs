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
using System.Text;
using Rhino.Commonjs.Module.Provider;
using Sharpen;

namespace Rhino.Commonjs.Module.Provider
{
	/// <summary>
	/// A URL-based script provider that can load modules against a set of base
	/// privileged and fallback URIs.
	/// </summary>
	/// <remarks>
	/// A URL-based script provider that can load modules against a set of base
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
	[System.Serializable]
	public class UrlModuleSourceProvider : ModuleSourceProviderBase
	{
		private const long serialVersionUID = 1L;

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
			return source != null ? source : LoadFromActualUri(uri, @base, validator);
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual ModuleSource LoadFromActualUri(Uri uri, Uri @base, object validator)
		{
			Uri url = new Uri(@base == null ? null : @base.ToURL(), uri.ToString());
			long request_time = Runtime.CurrentTimeMillis();
			URLConnection urlConnection = OpenUrlConnection(url);
			UrlModuleSourceProvider.URLValidator applicableValidator;
			if (validator is UrlModuleSourceProvider.URLValidator)
			{
				UrlModuleSourceProvider.URLValidator uriValidator = ((UrlModuleSourceProvider.URLValidator)validator);
				applicableValidator = uriValidator.AppliesTo(uri) ? uriValidator : null;
			}
			else
			{
				applicableValidator = null;
			}
			if (applicableValidator != null)
			{
				applicableValidator.ApplyConditionals(urlConnection);
			}
			try
			{
				urlConnection.Connect();
				if (applicableValidator != null && applicableValidator.UpdateValidator(urlConnection, request_time, urlConnectionExpiryCalculator))
				{
					Close(urlConnection);
					return ModuleSourceProviderConstants.NOT_MODIFIED;
				}
				return new ModuleSource(GetReader(urlConnection), GetSecurityDomain(urlConnection), uri, @base, new UrlModuleSourceProvider.URLValidator(uri, urlConnection, request_time, urlConnectionExpiryCalculator));
			}
			catch (FileNotFoundException)
			{
				return null;
			}
			catch (Exception e)
			{
				Close(urlConnection);
				throw;
			}
			catch (IOException e)
			{
				Close(urlConnection);
				throw;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static TextReader GetReader(URLConnection urlConnection)
		{
			return new StreamReader(urlConnection.GetInputStream(), GetCharacterEncoding(urlConnection));
		}

		private static string GetCharacterEncoding(URLConnection urlConnection)
		{
			ParsedContentType pct = new ParsedContentType(urlConnection.GetContentType());
			string encoding = pct.GetEncoding();
			if (encoding != null)
			{
				return encoding;
			}
			string contentType = pct.GetContentType();
			if (contentType != null && contentType.StartsWith("text/"))
			{
				return "8859_1";
			}
			else
			{
				return "utf-8";
			}
		}

		private object GetSecurityDomain(URLConnection urlConnection)
		{
			return urlConnectionSecurityDomainProvider == null ? null : urlConnectionSecurityDomainProvider.GetSecurityDomain(urlConnection);
		}

		private void Close(URLConnection urlConnection)
		{
			try
			{
				urlConnection.GetInputStream().Close();
			}
			catch (IOException e)
			{
				OnFailedClosingUrlConnection(urlConnection, e);
			}
		}

		/// <summary>
		/// Override if you want to get notified if the URL connection fails to
		/// close.
		/// </summary>
		/// <remarks>
		/// Override if you want to get notified if the URL connection fails to
		/// close. Does nothing by default.
		/// </remarks>
		/// <param name="urlConnection">the connection</param>
		/// <param name="cause">the cause it failed to close.</param>
		protected internal virtual void OnFailedClosingUrlConnection(URLConnection urlConnection, IOException cause)
		{
		}

		/// <summary>
		/// Can be overridden in subclasses to customize the URL connection opening
		/// process.
		/// </summary>
		/// <remarks>
		/// Can be overridden in subclasses to customize the URL connection opening
		/// process. By default, just calls
		/// <see cref="System.Uri.OpenConnection()">System.Uri.OpenConnection()</see>
		/// .
		/// </remarks>
		/// <param name="url">the URL</param>
		/// <returns>a connection to the URL.</returns>
		/// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
		protected internal virtual URLConnection OpenUrlConnection(Uri url)
		{
			return url.OpenConnection();
		}

		protected internal override bool EntityNeedsRevalidation(object validator)
		{
			return !(validator is UrlModuleSourceProvider.URLValidator) || ((UrlModuleSourceProvider.URLValidator)validator).EntityNeedsRevalidation();
		}

		[System.Serializable]
		private class URLValidator
		{
			private const long serialVersionUID = 1L;

			private readonly Uri uri;

			private readonly long lastModified;

			private readonly string entityTags;

			private long expiry;

			public URLValidator(Uri uri, URLConnection urlConnection, long request_time, UrlConnectionExpiryCalculator urlConnectionExpiryCalculator)
			{
				this.uri = uri;
				this.lastModified = urlConnection.GetLastModified();
				this.entityTags = GetEntityTags(urlConnection);
				expiry = CalculateExpiry(urlConnection, request_time, urlConnectionExpiryCalculator);
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal virtual bool UpdateValidator(URLConnection urlConnection, long request_time, UrlConnectionExpiryCalculator urlConnectionExpiryCalculator)
			{
				bool isResourceChanged = IsResourceChanged(urlConnection);
				if (!isResourceChanged)
				{
					expiry = CalculateExpiry(urlConnection, request_time, urlConnectionExpiryCalculator);
				}
				return isResourceChanged;
			}

			/// <exception cref="System.IO.IOException"></exception>
			private bool IsResourceChanged(URLConnection urlConnection)
			{
				if (urlConnection is HttpURLConnection)
				{
					return ((HttpURLConnection)urlConnection).GetResponseCode() == HttpURLConnection.HTTP_NOT_MODIFIED;
				}
				return lastModified == urlConnection.GetLastModified();
			}

			private long CalculateExpiry(URLConnection urlConnection, long request_time, UrlConnectionExpiryCalculator urlConnectionExpiryCalculator)
			{
				if ("no-cache".Equals(urlConnection.GetHeaderField("Pragma")))
				{
					return 0L;
				}
				string cacheControl = urlConnection.GetHeaderField("Cache-Control");
				if (cacheControl != null)
				{
					if (cacheControl.IndexOf("no-cache") != -1)
					{
						return 0L;
					}
					int max_age = GetMaxAge(cacheControl);
					if (-1 != max_age)
					{
						long response_time = Runtime.CurrentTimeMillis();
						long apparent_age = Math.Max(0, response_time - urlConnection.GetDate());
						long corrected_received_age = Math.Max(apparent_age, urlConnection.GetHeaderFieldInt("Age", 0) * 1000L);
						long response_delay = response_time - request_time;
						long corrected_initial_age = corrected_received_age + response_delay;
						long creation_time = response_time - corrected_initial_age;
						return max_age * 1000L + creation_time;
					}
				}
				long explicitExpiry = urlConnection.GetHeaderFieldDate("Expires", -1L);
				if (explicitExpiry != -1L)
				{
					return explicitExpiry;
				}
				return urlConnectionExpiryCalculator == null ? 0L : urlConnectionExpiryCalculator.CalculateExpiry(urlConnection);
			}

			private int GetMaxAge(string cacheControl)
			{
				int maxAgeIndex = cacheControl.IndexOf("max-age");
				if (maxAgeIndex == -1)
				{
					return -1;
				}
				int eq = cacheControl.IndexOf('=', maxAgeIndex + 7);
				if (eq == -1)
				{
					return -1;
				}
				int comma = cacheControl.IndexOf(',', eq + 1);
				string strAge;
				if (comma == -1)
				{
					strAge = Sharpen.Runtime.Substring(cacheControl, eq + 1);
				}
				else
				{
					strAge = Sharpen.Runtime.Substring(cacheControl, eq + 1, comma);
				}
				try
				{
					return System.Convert.ToInt32(strAge);
				}
				catch (FormatException)
				{
					return -1;
				}
			}

			private string GetEntityTags(URLConnection urlConnection)
			{
				IList<string> etags = urlConnection.GetHeaderFields().Get("ETag");
				if (etags == null || etags.IsEmpty())
				{
					return null;
				}
				StringBuilder b = new StringBuilder();
				IEnumerator<string> it = etags.GetEnumerator();
				b.Append(it.Next());
				while (it.HasNext())
				{
					b.Append(", ").Append(it.Next());
				}
				return b.ToString();
			}

			internal virtual bool AppliesTo(Uri uri)
			{
				return this.uri.Equals(uri);
			}

			internal virtual void ApplyConditionals(URLConnection urlConnection)
			{
				if (lastModified != 0L)
				{
					urlConnection.SetIfModifiedSince(lastModified);
				}
				if (entityTags != null && entityTags.Length > 0)
				{
					urlConnection.AddRequestProperty("If-None-Match", entityTags);
				}
			}

			internal virtual bool EntityNeedsRevalidation()
			{
				return Runtime.CurrentTimeMillis() > expiry;
			}
		}
	}
}
