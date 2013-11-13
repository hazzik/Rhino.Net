/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>AST node for an XML-text-only component of an XML literal expression.</summary>
	/// <remarks>
	/// AST node for an XML-text-only component of an XML literal expression.  This
	/// node differs from a
	/// <see cref="StringLiteral">StringLiteral</see>
	/// in that it does not have quotes for
	/// delimiters.
	/// </remarks>
	public class XmlString : XmlFragment
	{
		private string xml;

		public XmlString()
		{
		}

		public XmlString(int pos) : base(pos)
		{
		}

		public XmlString(int pos, string s) : base(pos)
		{
			SetXml(s);
		}

		/// <summary>Sets the string for this XML component.</summary>
		/// <remarks>
		/// Sets the string for this XML component.  Sets the length of the
		/// component to the length of the passed string.
		/// </remarks>
		/// <param name="s">a string of xml text</param>
		/// <exception cref="System.ArgumentException">
		/// } if
		/// <code>s</code>
		/// is
		/// <code>null</code>
		/// </exception>
		public virtual void SetXml(string s)
		{
			AssertNotNull(s);
			xml = s;
			SetLength(s.Length);
		}

		/// <summary>Returns the xml string for this component.</summary>
		/// <remarks>
		/// Returns the xml string for this component.
		/// Note that it may not be well-formed XML; it is a fragment.
		/// </remarks>
		public virtual string GetXml()
		{
			return xml;
		}

		public override string ToSource(int depth)
		{
			return MakeIndent(depth) + xml;
		}

		/// <summary>Visits this node.</summary>
		/// <remarks>Visits this node.  There are no children to visit.</remarks>
		public override void Visit(NodeVisitor v)
		{
			v.Visit(this);
		}
	}
}
