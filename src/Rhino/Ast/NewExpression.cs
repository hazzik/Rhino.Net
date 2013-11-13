/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Text;
using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>New expression.</summary>
	/// <remarks>
	/// New expression. Node type is
	/// <see cref="Rhino.Token.NEW">Rhino.Token.NEW</see>
	/// .<p>
	/// <pre><i>NewExpression</i> :
	/// MemberExpression
	/// <b>new</b> NewExpression</pre>
	/// This node is a subtype of
	/// <see cref="FunctionCall">FunctionCall</see>
	/// , mostly for internal code
	/// sharing.  Structurally a
	/// <code>NewExpression</code>
	/// node is very similar to a
	/// <code>FunctionCall</code>
	/// , so it made a certain amount of sense.
	/// </remarks>
	public class NewExpression : FunctionCall
	{
		private ObjectLiteral initializer;

		public NewExpression()
		{
			{
				type = Token.NEW;
			}
		}

		public NewExpression(int pos) : base(pos)
		{
			{
				type = Token.NEW;
			}
		}

		public NewExpression(int pos, int len) : base(pos, len)
		{
			{
				type = Token.NEW;
			}
		}

		/// <summary>Returns initializer object, if any.</summary>
		/// <remarks>Returns initializer object, if any.</remarks>
		/// <returns>
		/// extra initializer object-literal expression, or
		/// <code>null</code>
		/// if
		/// not specified.
		/// </returns>
		public virtual ObjectLiteral GetInitializer()
		{
			return initializer;
		}

		/// <summary>Sets initializer object.</summary>
		/// <remarks>
		/// Sets initializer object.  Rhino supports an experimental syntax
		/// of the form
		/// <code>new expr [ ( arglist ) ] [initializer]</code>
		/// ,
		/// in which initializer is an object literal that is used to set
		/// additional properties on the newly-created
		/// <code>expr</code>
		/// object.
		/// </remarks>
		/// <param name="initializer">
		/// extra initializer object.
		/// Can be
		/// <code>null</code>
		/// .
		/// </param>
		public virtual void SetInitializer(ObjectLiteral initializer)
		{
			this.initializer = initializer;
			if (initializer != null)
			{
				initializer.SetParent(this);
			}
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append("new ");
			sb.Append(target.ToSource(0));
			sb.Append("(");
			if (arguments != null)
			{
				PrintList(arguments, sb);
			}
			sb.Append(")");
			if (initializer != null)
			{
				sb.Append(" ");
				sb.Append(initializer.ToSource(0));
			}
			return sb.ToString();
		}

		/// <summary>Visits this node, the target, and each argument.</summary>
		/// <remarks>
		/// Visits this node, the target, and each argument.  If there is
		/// a trailing initializer node, visits that last.
		/// </remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				target.Visit(v);
				foreach (AstNode arg in GetArguments())
				{
					arg.Visit(v);
				}
				if (initializer != null)
				{
					initializer.Visit(v);
				}
			}
		}
	}
}
