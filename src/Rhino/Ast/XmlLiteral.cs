/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Text;
using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>AST node for an E4X (Ecma-357) embedded XML literal.</summary>
	/// <remarks>
	/// AST node for an E4X (Ecma-357) embedded XML literal.  Node type is
	/// <see cref="Rhino.Token.XML">Rhino.Token.XML</see>
	/// .  The parser generates a simple list of strings and
	/// expressions.  In the future we may parse the XML and produce a richer set of
	/// nodes, but for now it's just a set of expressions evaluated to produce a
	/// string to pass to the
	/// <code>XML</code>
	/// constructor function.<p>
	/// </remarks>
	public class XmlLiteral : AstNode
	{
		private IList<XmlFragment> fragments = new List<XmlFragment>();

		public XmlLiteral()
		{
			{
				type = Token.XML;
			}
		}

		public XmlLiteral(int pos) : base(pos)
		{
			{
				type = Token.XML;
			}
		}

		public XmlLiteral(int pos, int len) : base(pos, len)
		{
			{
				type = Token.XML;
			}
		}

		/// <summary>Returns fragment list - a list of expression nodes.</summary>
		/// <remarks>Returns fragment list - a list of expression nodes.</remarks>
		public virtual IList<XmlFragment> GetFragments()
		{
			return fragments;
		}

		/// <summary>Sets fragment list, removing any existing fragments first.</summary>
		/// <remarks>
		/// Sets fragment list, removing any existing fragments first.
		/// Sets the parent pointer for each fragment in the list to this node.
		/// </remarks>
		/// <param name="fragments">fragment list.  Replaces any existing fragments.</param>
		/// <exception cref="System.ArgumentException">
		/// } if
		/// <code>fragments</code>
		/// is
		/// <code>null</code>
		/// </exception>
		public virtual void SetFragments(IList<XmlFragment> fragments)
		{
			AssertNotNull(fragments);
			this.fragments.Clear();
			foreach (XmlFragment fragment in fragments)
			{
				AddFragment(fragment);
			}
		}

		/// <summary>Adds a fragment to the fragment list.</summary>
		/// <remarks>Adds a fragment to the fragment list.  Sets its parent to this node.</remarks>
		/// <exception cref="System.ArgumentException">
		/// } if
		/// <code>fragment</code>
		/// is
		/// <code>null</code>
		/// </exception>
		public virtual void AddFragment(XmlFragment fragment)
		{
			AssertNotNull(fragment);
			fragments.AddItem(fragment);
			fragment.SetParent(this);
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder(250);
			foreach (XmlFragment frag in fragments)
			{
				sb.Append(frag.ToSource(0));
			}
			return sb.ToString();
		}

		/// <summary>Visits this node, then visits each child fragment in lexical order.</summary>
		/// <remarks>Visits this node, then visits each child fragment in lexical order.</remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				foreach (XmlFragment frag in fragments)
				{
					frag.Visit(v);
				}
			}
		}
	}
}
