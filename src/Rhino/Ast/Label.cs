/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Text;
using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>AST node representing a label.</summary>
	/// <remarks>
	/// AST node representing a label.  It is a distinct node type so it can
	/// record its length and position for code-processing tools.
	/// Node type is
	/// <see cref="Rhino.Token.LABEL">Rhino.Token.LABEL</see>
	/// .<p>
	/// </remarks>
	public class Label : Jump
	{
		private string name;

		public Label()
		{
			{
				type = Token.LABEL;
			}
		}

		public Label(int pos) : this(pos, -1)
		{
		}

		public Label(int pos, int len)
		{
			{
				type = Token.LABEL;
			}
			// can't call super (Jump) for historical reasons
			position = pos;
			length = len;
		}

		public Label(int pos, int len, string name) : this(pos, len)
		{
			SetName(name);
		}

		/// <summary>Returns the label text</summary>
		public virtual string GetName()
		{
			return name;
		}

		/// <summary>Sets the label text</summary>
		/// <exception cref="System.ArgumentException">
		/// if name is
		/// <code>null</code>
		/// or the
		/// empty string.
		/// </exception>
		public virtual void SetName(string name)
		{
			name = name == null ? null : name.Trim();
			if (name == null || string.Empty.Equals(name))
			{
				throw new ArgumentException("invalid label name");
			}
			this.name = name;
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append(name);
			sb.Append(":\n");
			return sb.ToString();
		}

		/// <summary>Visits this label.</summary>
		/// <remarks>Visits this label.  There are no children to visit.</remarks>
		public override void Visit(NodeVisitor v)
		{
			v.Visit(this);
		}
	}
}
