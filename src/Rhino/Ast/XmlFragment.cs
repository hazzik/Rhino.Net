/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>
	/// Abstract base type for components that comprise an
	/// <see cref="XmlLiteral">XmlLiteral</see>
	/// object. Node type is
	/// <see cref="Rhino.Token.XML">Rhino.Token.XML</see>
	/// .<p>
	/// </summary>
	public abstract class XmlFragment : AstNode
	{
		public XmlFragment()
		{
			{
				type = Token.XML;
			}
		}

		public XmlFragment(int pos) : base(pos)
		{
			{
				type = Token.XML;
			}
		}

		public XmlFragment(int pos, int len) : base(pos, len)
		{
			{
				type = Token.XML;
			}
		}
	}
}
