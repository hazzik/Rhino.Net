/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using System.Net;
using Rhino.CommonJS.Module.Provider;
using Sharpen;

namespace Rhino.Tools
{
	/// <author>Attila Szegedi</author>
	/// <version>$Id: SourceReader.java,v 1.2 2010/02/15 19:31:17 szegedia%freemail.hu Exp $</version>
	public static class SourceReader
	{
		public static Uri ToUrl(string path)
		{
			// Assume path is URL if it contains a colon and there are at least
			// 2 characters in the protocol part. The later allows under Windows
			// to interpret paths with driver letter as file, not URL.
			if (path.IndexOf(':') >= 2)
			{
				try
				{
					return new Uri(path);
				}
				catch (UriFormatException)
				{
				}
			}
			// not a URL
			return null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static string ReadFileOrUrlAsString(string absPath, string defaultEncoding)
		{
			Uri url = ToUrl(absPath);
			string encoding;
			string contentType;
			byte[] data;
			int capacityHint;
			using (Stream @is = GetStream(absPath, url, out contentType, out encoding, out capacityHint))
			{
				data = Kit.ReadStream(@is, capacityHint);
			}
			if (encoding == null)
			{
				encoding = DetectEncoding(defaultEncoding, data, url, contentType);
			}
			string strResult = Runtime.GetEncoding(encoding).GetString(data);
			// Skip BOM
			if (strResult.Length > 0 && strResult [0] == '\uFEFF')
			{
				strResult = strResult.Substring(1);
			}
			return strResult;
		}

		private static Stream GetStream(string absPath, Uri url, out string contentType, out string encoding, out int capacityHint)
		{
			Stream stream;
			if (url == null)
			{
				FileInfo file = new FileInfo(absPath);
				contentType = encoding = null;
				capacityHint = (int) file.Length;
				stream = file.OpenRead();
			}
			else
			{
				WebRequest request = WebRequest.Create(url);
				WebResponse response = request.GetResponse();
				stream = response.GetResponseStream();
				ParsedContentType pct = new ParsedContentType(request.ContentType);
				contentType = pct.GetContentType();
				encoding = pct.GetEncoding();
				capacityHint = (int) response.ContentLength;
				// Ignore insane values for Content-Length
				if (capacityHint > (1 << 20))
				{
					capacityHint = -1;
				}
			}
			if (capacityHint <= 0)
			{
				capacityHint = 4096;
			}
			return stream;
		}

		private static string DetectEncoding(string defaultEncoding, byte[] data, Uri url, string contentType)
		{
			string encoding;
			// None explicitly specified in Content-type header. Use RFC-4329
			// 4.2.2 section to autodetect
			if (data.Length > 3 && data [0] == -1 && data [1] == -2 && data [2] == 0 && data [3] == 0)
			{
				encoding = "UTF-32LE";
			}
			else if (data.Length > 3 && data [0] == 0 && data [1] == 0 && data [2] == -2 && data [3] == -1)
			{
				encoding = "UTF-32BE";
			}
			else if (data.Length > 2 && data [0] == -17 && data [1] == -69 && data [2] == -65)
			{
				encoding = "UTF-8";
			}
			else if (data.Length > 1 && data [0] == -1 && data [1] == -2)
			{
				encoding = "UTF-16LE";
			}
			else if (data.Length > 1 && data [0] == -2 && data [1] == -1)
			{
				encoding = "UTF-16BE";
			}
			else if (defaultEncoding != null)
			{
				// No autodetect. See if we have explicit value on command line
				encoding = defaultEncoding;
			}
			else if (url == null)
			{
				// No explicit encoding specification
				// Local files default to system encoding
				encoding = Runtime.GetProperty("file.encoding");
			}
			else if (contentType != null && contentType.StartsWith("application/"))
			{
				// application/* types default to UTF-8
				encoding = "UTF-8";
			}
			else
			{
				// text/* MIME types default to US-ASCII
				encoding = "US-ASCII";
			}
			return encoding;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static object ReadFileOrUrlAsByteArray(string path)
		{
			Uri url = ToUrl(path);
			byte[] data;
			int capacityHint;
			using (Stream @is = GetStream(path, url, out capacityHint))
			{
				data = Kit.ReadStream(@is, capacityHint);
			}
			return data;
		}

		private static Stream GetStream(string path, Uri url, out int capacityHint)
		{
			Stream stream;
			if (url == null)
			{
				var file = new FileInfo(path);
				capacityHint = (int) file.Length;
				stream = file.OpenRead();
			}
			else
			{
				WebRequest request = WebRequest.Create(url);
				WebResponse response = request.GetResponse();
				stream = response.GetResponseStream();
				capacityHint = (int) response.ContentLength;
			}
			// Ignore insane values for Content-Length
			if (capacityHint > (1 << 20))
			{
				capacityHint = -1;
			}
			if (capacityHint <= 0)
			{
				capacityHint = 4096;
			}
			return stream;
		}
	}
}
