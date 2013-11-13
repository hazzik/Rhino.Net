/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Sharpen;

namespace Rhino.Commonjs.Module.Provider
{
	/// <summary>
	/// Breaks a "contentType; charset=encoding" MIME type into content type and
	/// encoding parts.
	/// </summary>
	/// <remarks>
	/// Breaks a "contentType; charset=encoding" MIME type into content type and
	/// encoding parts.
	/// </remarks>
	/// <author>Attila Szegedi</author>
	/// <version>$Id: ParsedContentType.java,v 1.3 2011/04/07 20:26:12 hannes%helma.at Exp $</version>
	[System.Serializable]
	public sealed class ParsedContentType
	{
		private const long serialVersionUID = 1L;

		private readonly string contentType;

		private readonly string encoding;

		/// <summary>Creates a new parsed content type.</summary>
		/// <remarks>Creates a new parsed content type.</remarks>
		/// <param name="mimeType">
		/// the full MIME type; typically the value of the
		/// "Content-Type" header of some MIME-compliant message. Can be null.
		/// </param>
		public ParsedContentType(string mimeType)
		{
			string contentType = null;
			string encoding = null;
			if (mimeType != null)
			{
				StringTokenizer tok = new StringTokenizer(mimeType, ";");
				if (tok.HasMoreTokens())
				{
					contentType = tok.NextToken().Trim();
					while (tok.HasMoreTokens())
					{
						string param = tok.NextToken().Trim();
						if (param.StartsWith("charset="))
						{
							encoding = Sharpen.Runtime.Substring(param, 8).Trim();
							int l = encoding.Length;
							if (l > 0)
							{
								if (encoding[0] == '"')
								{
									encoding = Sharpen.Runtime.Substring(encoding, 1);
								}
								if (encoding[l - 1] == '"')
								{
									encoding = Sharpen.Runtime.Substring(encoding, 0, l - 1);
								}
							}
							break;
						}
					}
				}
			}
			this.contentType = contentType;
			this.encoding = encoding;
		}

		/// <summary>Returns the content type (without charset declaration) of the MIME type.</summary>
		/// <remarks>Returns the content type (without charset declaration) of the MIME type.</remarks>
		/// <returns>
		/// the content type (without charset declaration) of the MIME type.
		/// Can be null if the MIME type was null.
		/// </returns>
		public string GetContentType()
		{
			return contentType;
		}

		/// <summary>Returns the character encoding of the MIME type.</summary>
		/// <remarks>Returns the character encoding of the MIME type.</remarks>
		/// <returns>
		/// the character encoding of the MIME type. Can be null when it is
		/// not specified.
		/// </returns>
		public string GetEncoding()
		{
			return encoding;
		}
	}
}