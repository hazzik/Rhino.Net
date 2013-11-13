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
	/// <summary>Represents a symbol-table entry.</summary>
	/// <remarks>Represents a symbol-table entry.</remarks>
	public class Symbol
	{
		private int declType;

		private int index = -1;

		private string name;

		private Node node;

		private Scope containingTable;

		public Symbol()
		{
		}

		/// <summary>Constructs a new Symbol with a specific name and declaration type</summary>
		/// <param name="declType">
		/// 
		/// <see cref="Rhino.Token.FUNCTION">Rhino.Token.FUNCTION</see>
		/// ,
		/// <see cref="Rhino.Token.LP">Rhino.Token.LP</see>
		/// (for params),
		/// <see cref="Rhino.Token.VAR">Rhino.Token.VAR</see>
		/// ,
		/// <see cref="Rhino.Token.LET">Rhino.Token.LET</see>
		/// or
		/// <see cref="Rhino.Token.CONST">Rhino.Token.CONST</see>
		/// </param>
		public Symbol(int declType, string name)
		{
			// One of Token.FUNCTION, Token.LP (for parameters), Token.VAR,
			// Token.LET, or Token.CONST
			SetName(name);
			SetDeclType(declType);
		}

		/// <summary>Returns symbol declaration type</summary>
		public virtual int GetDeclType()
		{
			return declType;
		}

		/// <summary>Sets symbol declaration type</summary>
		public virtual void SetDeclType(int declType)
		{
			if (!(declType == Token.FUNCTION || declType == Token.LP || declType == Token.VAR || declType == Token.LET || declType == Token.CONST))
			{
				throw new ArgumentException("Invalid declType: " + declType);
			}
			this.declType = declType;
		}

		/// <summary>Returns symbol name</summary>
		public virtual string GetName()
		{
			return name;
		}

		/// <summary>Sets symbol name</summary>
		public virtual void SetName(string name)
		{
			this.name = name;
		}

		/// <summary>Returns the node associated with this identifier</summary>
		public virtual Node GetNode()
		{
			return node;
		}

		/// <summary>Returns symbol's index in its scope</summary>
		public virtual int GetIndex()
		{
			return index;
		}

		/// <summary>Sets symbol's index in its scope</summary>
		public virtual void SetIndex(int index)
		{
			this.index = index;
		}

		/// <summary>Sets the node associated with this identifier</summary>
		public virtual void SetNode(Node node)
		{
			this.node = node;
		}

		/// <summary>Returns the Scope in which this symbol is entered</summary>
		public virtual Scope GetContainingTable()
		{
			return containingTable;
		}

		/// <summary>Sets this symbol's Scope</summary>
		public virtual void SetContainingTable(Scope containingTable)
		{
			this.containingTable = containingTable;
		}

		public virtual string GetDeclTypeName()
		{
			return Token.TypeToName(declType);
		}

		public override string ToString()
		{
			StringBuilder result = new StringBuilder();
			result.Append("Symbol (");
			result.Append(GetDeclTypeName());
			result.Append(") name=");
			result.Append(name);
			if (node != null)
			{
				result.Append(" line=");
				result.Append(node.GetLineno());
			}
			return result.ToString();
		}
	}
}
