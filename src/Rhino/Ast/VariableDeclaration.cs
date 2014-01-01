/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Text;
using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>A list of one or more var, const or let declarations.</summary>
	/// <remarks>
	/// A list of one or more var, const or let declarations.
	/// Node type is
	/// <see cref="Rhino.Token.VAR">Rhino.Token.VAR</see>
	/// ,
	/// <see cref="Rhino.Token.CONST">Rhino.Token.CONST</see>
	/// or
	/// <see cref="Rhino.Token.LET">Rhino.Token.LET</see>
	/// .<p>
	/// If the node is for
	/// <code>var</code>
	/// or
	/// <code>const</code>
	/// , the node position
	/// is the beginning of the
	/// <code>var</code>
	/// or
	/// <code>const</code>
	/// keyword.
	/// For
	/// <code>let</code>
	/// declarations, the node position coincides with the
	/// first
	/// <see cref="VariableInitializer">VariableInitializer</see>
	/// child.<p>
	/// A standalone variable declaration in a statement context returns
	/// <code>true</code>
	/// from its
	/// <see cref="IsStatement()">IsStatement()</see>
	/// method.
	/// </remarks>
	public class VariableDeclaration : AstNode
	{
		private IList<VariableInitializer> variables = new List<VariableInitializer>();

		private bool isStatement;

		public VariableDeclaration()
		{
			{
				type = Token.VAR;
			}
		}

		public VariableDeclaration(int pos) : base(pos)
		{
			{
				type = Token.VAR;
			}
		}

		public VariableDeclaration(int pos, int len) : base(pos, len)
		{
			{
				type = Token.VAR;
			}
		}

		/// <summary>Returns variable list.</summary>
		/// <remarks>
		/// Returns variable list.  Never
		/// <code>null</code>
		/// .
		/// </remarks>
		public virtual IList<VariableInitializer> GetVariables()
		{
			return variables;
		}

		/// <summary>Sets variable list</summary>
		/// <exception cref="System.ArgumentException">
		/// if variables list is
		/// <code>null</code>
		/// </exception>
		public virtual void SetVariables(IList<VariableInitializer> variables)
		{
			AssertNotNull(variables);
			this.variables.Clear();
			foreach (VariableInitializer vi in variables)
			{
				AddVariable(vi);
			}
		}

		/// <summary>Adds a variable initializer node to the child list.</summary>
		/// <remarks>
		/// Adds a variable initializer node to the child list.
		/// Sets initializer node's parent to this node.
		/// </remarks>
		/// <exception cref="System.ArgumentException">
		/// if v is
		/// <code>null</code>
		/// </exception>
		public virtual void AddVariable(VariableInitializer v)
		{
			AssertNotNull(v);
			variables.AddItem(v);
			v.SetParent(this);
		}

		/// <summary>Sets the node type and returns this node.</summary>
		/// <remarks>Sets the node type and returns this node.</remarks>
		/// <exception cref="System.ArgumentException">
		/// if
		/// <code>declType</code>
		/// is invalid
		/// </exception>
		public override Node SetType(int type)
		{
			if (type != Token.VAR && type != Token.CONST && type != Token.LET)
			{
				throw new ArgumentException("invalid decl type: " + type);
			}
			return base.SetType(type);
		}

		/// <summary>
		/// Returns true if this is a
		/// <code>var</code>
		/// (not
		/// <code>const</code>
		/// or
		/// <code>let</code>
		/// ) declaration.
		/// </summary>
		/// <returns>
		/// true if
		/// <code>declType</code>
		/// is
		/// <see cref="Rhino.Token.VAR">Rhino.Token.VAR</see>
		/// </returns>
		public virtual bool IsVar()
		{
			return type == Token.VAR;
		}

		/// <summary>
		/// Returns true if this is a
		/// <see cref="Rhino.Token.CONST">Rhino.Token.CONST</see>
		/// declaration.
		/// </summary>
		public virtual bool IsConst()
		{
			return type == Token.CONST;
		}

		/// <summary>
		/// Returns true if this is a
		/// <see cref="Rhino.Token.LET">Rhino.Token.LET</see>
		/// declaration.
		/// </summary>
		public virtual bool IsLet()
		{
			return type == Token.LET;
		}

		/// <summary>Returns true if this node represents a statement.</summary>
		/// <remarks>Returns true if this node represents a statement.</remarks>
		public virtual bool IsStatement()
		{
			return isStatement;
		}

		/// <summary>Set or unset the statement flag.</summary>
		/// <remarks>Set or unset the statement flag.</remarks>
		public virtual void SetIsStatement(bool isStatement)
		{
			this.isStatement = isStatement;
		}

		private string DeclTypeName()
		{
			return Token.TypeToName(type).ToLower();
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append(DeclTypeName());
			sb.Append(" ");
			PrintList(variables, sb);
			if (IsStatement())
			{
				sb.Append(";\n");
			}
			return sb.ToString();
		}

		/// <summary>
		/// Visits this node, then each
		/// <see cref="VariableInitializer">VariableInitializer</see>
		/// child.
		/// </summary>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				foreach (AstNode var in variables)
				{
					var.Visit(v);
				}
			}
		}
	}
}
