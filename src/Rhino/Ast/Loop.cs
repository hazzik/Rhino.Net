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
	/// <summary>Abstract base type for loops.</summary>
	/// <remarks>Abstract base type for loops.</remarks>
	public abstract class Loop : Scope
	{
		protected internal AstNode body;

		protected internal int lp = -1;

		protected internal int rp = -1;

		public Loop()
		{
		}

		public Loop(int pos) : base(pos)
		{
		}

		public Loop(int pos, int len) : base(pos, len)
		{
		}

		/// <summary>Returns loop body</summary>
		public virtual AstNode GetBody()
		{
			return body;
		}

		/// <summary>Sets loop body.</summary>
		/// <remarks>
		/// Sets loop body.  Sets the parent of the body to this loop node,
		/// and updates its offset to be relative.  Extends the length of this
		/// node to include the body.
		/// </remarks>
		public virtual void SetBody(AstNode body)
		{
			this.body = body;
			int end = body.Position + body.GetLength();
			this.SetLength(end - this.Position);
			body.SetParent(this);
		}

		/// <summary>Returns left paren position, -1 if missing</summary>
		public virtual int GetLp()
		{
			return lp;
		}

		/// <summary>Sets left paren position</summary>
		public virtual void SetLp(int lp)
		{
			this.lp = lp;
		}

		/// <summary>Returns right paren position, -1 if missing</summary>
		public virtual int GetRp()
		{
			return rp;
		}

		/// <summary>Sets right paren position</summary>
		public virtual void SetRp(int rp)
		{
			this.rp = rp;
		}

		/// <summary>Sets both paren positions</summary>
		public virtual void SetParens(int lp, int rp)
		{
			this.lp = lp;
			this.rp = rp;
		}
	}
}
