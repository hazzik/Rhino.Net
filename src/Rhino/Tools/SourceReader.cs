/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using Rhino;
using Rhino.CommonJS.Module.Provider;
using Rhino.Tools;
using Sharpen;

namespace Rhino.Tools
{
	/// <author>Attila Szegedi</author>
	/// <version>$Id: SourceReader.java,v 1.2 2010/02/15 19:31:17 szegedia%freemail.hu Exp $</version>
	public class SourceReader
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
		public static object ReadFileOrUrl(string path, bool convertToString, string defaultEncoding)
		{
			Uri url = ToUrl(path);
			Stream @is = null;
			int capacityHint = 0;
			string encoding;
			string contentType;
			byte[] data;
			try
			{
				if (url == null)
				{
					FilePath file = new FilePath(path);
					contentType = encoding = null;
					capacityHint = (int)file.Length();
					@is = new FileInputStream(file);
				}
				else
				{
					URLConnection uc = url.OpenConnection();
					@is = uc.GetInputStream();
					if (convertToString)
					{
						ParsedContentType pct = new ParsedContentType(uc.GetContentType());
						contentType = pct.GetContentType();
						encoding = pct.GetEncoding();
					}
					else
					{
						contentType = encoding = null;
					}
					capacityHint = uc.GetContentLength();
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
				data = Kit.ReadStream(@is, capacityHint);
			}
			finally
			{
				if (@is != null)
				{
					@is.Close();
				}
			}
			object result;
			if (!convertToString)
			{
				result = data;
			}
			else
			{
				if (encoding == null)
				{
					// None explicitly specified in Content-type header. Use RFC-4329
					// 4.2.2 section to autodetect
					if (data.Length > 3 && data[0] == -1 && data[1] == -2 && data[2] == 0 && data[3] == 0)
					{
						encoding = "UTF-32LE";
					}
					else
					{
						if (data.Length > 3 && data[0] == 0 && data[1] == 0 && data[2] == -2 && data[3] == -1)
						{
							encoding = "UTF-32BE";
						}
						else
						{
							if (data.Length > 2 && data[0] == -17 && data[1] == -69 && data[2] == -65)
							{
								encoding = "UTF-8";
							}
							else
							{
								if (data.Length > 1 && data[0] == -1 && data[1] == -2)
								{
									encoding = "UTF-16LE";
								}
								else
								{
									if (data.Length > 1 && data[0] == -2 && data[1] == -1)
									{
										encoding = "UTF-16BE";
									}
									else
									{
										// No autodetect. See if we have explicit value on command line
										encoding = defaultEncoding;
										if (encoding == null)
										{
											// No explicit encoding specification
											if (url == null)
											{
												// Local files default to system encoding
												encoding = Runtime.GetProperty("file.encoding");
											}
											else
											{
												if (contentType != null && contentType.StartsWith("application/"))
												{
													// application/* types default to UTF-8
													encoding = "UTF-8";
												}
												else
												{
													// text/* MIME types default to US-ASCII
													encoding = "US-ASCII";
												}
											}
										}
									}
								}
							}
						}
					}
				}
				string strResult = Sharpen.Runtime.GetStringForBytes(data, encoding);
				// Skip BOM
				if (strResult.Length > 0 && strResult[0] == '\uFEFF')
				{
					strResult = Sharpen.Runtime.Substring(strResult, 1);
				}
				result = strResult;
			}
			return result;
		}
	}
}
