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
	/// <summary>
	/// A JavaScript function declaration or expression.<p>
	/// Node type is
	/// <see cref="Rhino.Token.FUNCTION">Rhino.Token.FUNCTION</see>
	/// .<p>
	/// <pre><i>FunctionDeclaration</i> :
	/// <b>function</b> Identifier ( FormalParameterListopt ) { FunctionBody }
	/// <i>FunctionExpression</i> :
	/// <b>function</b> Identifieropt ( FormalParameterListopt ) { FunctionBody }
	/// <i>FormalParameterList</i> :
	/// Identifier
	/// FormalParameterList , Identifier
	/// <i>FunctionBody</i> :
	/// SourceElements
	/// <i>Program</i> :
	/// SourceElements
	/// <i>SourceElements</i> :
	/// SourceElement
	/// SourceElements SourceElement
	/// <i>SourceElement</i> :
	/// Statement
	/// FunctionDeclaration</pre>
	/// JavaScript 1.8 introduces "function closures" of the form
	/// <pre>function ([params] ) Expression</pre>
	/// In this case the FunctionNode node will have no body but will have an
	/// expression.
	/// </summary>
	public class FunctionNode : ScriptNode
	{
		/// <summary>There are three types of functions that can be defined.</summary>
		/// <remarks>
		/// There are three types of functions that can be defined. The first
		/// is a function statement. This is a function appearing as a top-level
		/// statement (i.e., not nested inside some other statement) in either a
		/// script or a function.<p>
		/// The second is a function expression, which is a function appearing in
		/// an expression except for the third type, which is...<p>
		/// The third type is a function expression where the expression is the
		/// top-level expression in an expression statement.<p>
		/// The three types of functions have different treatment and must be
		/// distinguished.<p>
		/// </remarks>
		public const int FUNCTION_STATEMENT = 1;

		public const int FUNCTION_EXPRESSION = 2;

		public const int FUNCTION_EXPRESSION_STATEMENT = 3;

		public enum Form
		{
			FUNCTION,
			GETTER,
			SETTER
		}

		private static readonly IList<AstNode> NO_PARAMS = new List<AstNode>().AsReadOnly();

		private Name functionName;

		private IList<AstNode> @params;

		private AstNode body;

		private bool isExpressionClosure;

		private FunctionNode.Form functionForm = FunctionNode.Form.FUNCTION;

		private int lp = -1;

		private int rp = -1;

		private int functionType;

		private bool needsActivation;

		private bool isGenerator;

		private IList<Node> generatorResumePoints;

		private IDictionary<Node, int[]> liveLocals;

		private AstNode memberExprNode;

		public FunctionNode()
		{
			{
				// codegen variables
				type = Token.FUNCTION;
			}
		}

		public FunctionNode(int pos) : base(pos)
		{
			{
				type = Token.FUNCTION;
			}
		}

		public FunctionNode(int pos, Name name) : base(pos)
		{
			{
				type = Token.FUNCTION;
			}
			SetFunctionName(name);
		}

		/// <summary>Returns function name</summary>
		/// <returns>
		/// function name,
		/// <code>null</code>
		/// for anonymous functions
		/// </returns>
		public virtual Name GetFunctionName()
		{
			return functionName;
		}

		/// <summary>Sets function name, and sets its parent to this node.</summary>
		/// <remarks>Sets function name, and sets its parent to this node.</remarks>
		/// <param name="name">
		/// function name,
		/// <code>null</code>
		/// for anonymous functions
		/// </param>
		public virtual void SetFunctionName(Name name)
		{
			functionName = name;
			if (name != null)
			{
				name.SetParent(this);
			}
		}

		/// <summary>Returns the function name as a string</summary>
		/// <returns>
		/// the function name,
		/// <code>""</code>
		/// if anonymous
		/// </returns>
		public virtual string GetName()
		{
			return functionName != null ? functionName.GetIdentifier() : string.Empty;
		}

		/// <summary>Returns the function parameter list</summary>
		/// <returns>
		/// the function parameter list.  Returns an immutable empty
		/// list if there are no parameters.
		/// </returns>
		public virtual IList<AstNode> GetParams()
		{
			return @params != null ? @params : NO_PARAMS;
		}

		/// <summary>
		/// Sets the function parameter list, and sets the parent for
		/// each element of the list.
		/// </summary>
		/// <remarks>
		/// Sets the function parameter list, and sets the parent for
		/// each element of the list.
		/// </remarks>
		/// <param name="params">
		/// the function parameter list, or
		/// <code>null</code>
		/// if no params
		/// </param>
		public virtual void SetParams(IList<AstNode> @params)
		{
			if (@params == null)
			{
				this.@params = null;
			}
			else
			{
				if (this.@params != null)
				{
					this.@params.Clear();
				}
				foreach (AstNode param in @params)
				{
					AddParam(param);
				}
			}
		}

		/// <summary>Adds a parameter to the function parameter list.</summary>
		/// <remarks>
		/// Adds a parameter to the function parameter list.
		/// Sets the parent of the param node to this node.
		/// </remarks>
		/// <param name="param">the parameter</param>
		/// <exception cref="System.ArgumentException">
		/// if param is
		/// <code>null</code>
		/// </exception>
		public virtual void AddParam(AstNode param)
		{
			AssertNotNull(param);
			if (@params == null)
			{
				@params = new List<AstNode>();
			}
			@params.Add(param);
			param.SetParent(this);
		}

		/// <summary>
		/// Returns true if the specified
		/// <see cref="AstNode">AstNode</see>
		/// node is a parameter
		/// of this Function node.  This provides a way during AST traversal
		/// to disambiguate the function name node from the parameter nodes.
		/// </summary>
		public virtual bool IsParam(AstNode node)
		{
			return @params == null ? false : @params.Contains(node);
		}

		/// <summary>Returns function body.</summary>
		/// <remarks>
		/// Returns function body.  Normally a
		/// <see cref="Block">Block</see>
		/// , but can be a plain
		/// <see cref="AstNode">AstNode</see>
		/// if it's a function closure.
		/// </remarks>
		/// <returns>
		/// the body.  Can be
		/// <code>null</code>
		/// only if the AST is malformed.
		/// </returns>
		public virtual AstNode GetBody()
		{
			return body;
		}

		/// <summary>Sets function body, and sets its parent to this node.</summary>
		/// <remarks>
		/// Sets function body, and sets its parent to this node.
		/// Also sets the encoded source bounds based on the body bounds.
		/// Assumes the function node absolute position has already been set,
		/// and the body node's absolute position and length are set.<p>
		/// </remarks>
		/// <param name="body">
		/// function body.  Its parent is set to this node, and its
		/// position is updated to be relative to this node.
		/// </param>
		/// <exception cref="System.ArgumentException">
		/// if body is
		/// <code>null</code>
		/// </exception>
		public virtual void SetBody(AstNode body)
		{
			AssertNotNull(body);
			this.body = body;
			if (true.Equals(body.GetProp(Node.EXPRESSION_CLOSURE_PROP)))
			{
				SetIsExpressionClosure(true);
			}
			int absEnd = body.GetPosition() + body.GetLength();
			body.SetParent(this);
			this.SetLength(absEnd - this.position);
			SetEncodedSourceBounds(this.position, absEnd);
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

		/// <summary>Returns whether this is a 1.8 function closure</summary>
		public virtual bool IsExpressionClosure()
		{
			return isExpressionClosure;
		}

		/// <summary>Sets whether this is a 1.8 function closure</summary>
		public virtual void SetIsExpressionClosure(bool isExpressionClosure)
		{
			this.isExpressionClosure = isExpressionClosure;
		}

		/// <summary>Return true if this function requires an Ecma-262 Activation object.</summary>
		/// <remarks>
		/// Return true if this function requires an Ecma-262 Activation object.
		/// The Activation object is implemented by
		/// <see cref="Rhino.NativeCall">Rhino.NativeCall</see>
		/// , and is fairly expensive
		/// to create, so when possible, the interpreter attempts to use a plain
		/// call frame instead.
		/// </remarks>
		/// <returns>
		/// true if this function needs activation.  It could be needed
		/// if there is a lexical closure, or in a number of other situations.
		/// </returns>
		public virtual bool RequiresActivation()
		{
			return needsActivation;
		}

		public virtual void SetRequiresActivation()
		{
			needsActivation = true;
		}

		public virtual bool IsGenerator()
		{
			return isGenerator;
		}

		public virtual void SetIsGenerator()
		{
			isGenerator = true;
		}

		public virtual void AddResumptionPoint(Node target)
		{
			if (generatorResumePoints == null)
			{
				generatorResumePoints = new List<Node>();
			}
			generatorResumePoints.Add(target);
		}

		public virtual IList<Node> GetResumptionPoints()
		{
			return generatorResumePoints;
		}

		public virtual IDictionary<Node, int[]> GetLiveLocals()
		{
			return liveLocals;
		}

		public virtual void AddLiveLocals(Node node, int[] locals)
		{
			if (liveLocals == null)
			{
				liveLocals = new Dictionary<Node, int[]>();
			}
			liveLocals[node] = locals;
		}

		public override int AddFunction(Rhino.Ast.FunctionNode fnNode)
		{
			int result = base.AddFunction(fnNode);
			if (GetFunctionCount() > 0)
			{
				needsActivation = true;
			}
			return result;
		}

		/// <summary>Returns the function type (statement, expr, statement expr)</summary>
		public virtual int GetFunctionType()
		{
			return functionType;
		}

		public virtual void SetFunctionType(int type)
		{
			functionType = type;
		}

		public virtual bool IsGetterOrSetter()
		{
			return functionForm == FunctionNode.Form.GETTER || functionForm == FunctionNode.Form.SETTER;
		}

		public virtual bool IsGetter()
		{
			return functionForm == FunctionNode.Form.GETTER;
		}

		public virtual bool IsSetter()
		{
			return functionForm == FunctionNode.Form.SETTER;
		}

		public virtual void SetFunctionIsGetter()
		{
			functionForm = FunctionNode.Form.GETTER;
		}

		public virtual void SetFunctionIsSetter()
		{
			functionForm = FunctionNode.Form.SETTER;
		}

		/// <summary>
		/// Rhino supports a nonstandard Ecma extension that allows you to
		/// say, for instance, function a.b.c(arg1, arg) {...}, and it will
		/// be rewritten at codegen time to:  a.b.c = function(arg1, arg2) {...}
		/// If we detect an expression other than a simple Name in the position
		/// where a function name was expected, we record that expression here.
		/// </summary>
		/// <remarks>
		/// Rhino supports a nonstandard Ecma extension that allows you to
		/// say, for instance, function a.b.c(arg1, arg) {...}, and it will
		/// be rewritten at codegen time to:  a.b.c = function(arg1, arg2) {...}
		/// If we detect an expression other than a simple Name in the position
		/// where a function name was expected, we record that expression here.
		/// <p>
		/// This extension is only available by setting the CompilerEnv option
		/// "isAllowMemberExprAsFunctionName" in the Parser.
		/// </remarks>
		public virtual void SetMemberExprNode(AstNode node)
		{
			memberExprNode = node;
			if (node != null)
			{
				node.SetParent(this);
			}
		}

		public virtual AstNode GetMemberExprNode()
		{
			return memberExprNode;
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append("function");
			if (functionName != null)
			{
				sb.Append(" ");
				sb.Append(functionName.ToSource(0));
			}
			if (@params == null)
			{
				sb.Append("() ");
			}
			else
			{
				sb.Append("(");
				PrintList(@params, sb);
				sb.Append(") ");
			}
			if (isExpressionClosure)
			{
				AstNode body = GetBody();
				if (body.GetLastChild() is ReturnStatement)
				{
					// omit "return" keyword, just print the expression
					body = ((ReturnStatement)body.GetLastChild()).GetReturnValue();
					sb.Append(body.ToSource(0));
					if (functionType == FUNCTION_STATEMENT)
					{
						sb.Append(";");
					}
				}
				else
				{
					// should never happen
					sb.Append(" ");
					sb.Append(body.ToSource(0));
				}
			}
			else
			{
				sb.Append(GetBody().ToSource(depth).Trim());
			}
			if (functionType == FUNCTION_STATEMENT)
			{
				sb.Append("\n");
			}
			return sb.ToString();
		}

		/// <summary>
		/// Visits this node, the function name node if supplied,
		/// the parameters, and the body.
		/// </summary>
		/// <remarks>
		/// Visits this node, the function name node if supplied,
		/// the parameters, and the body.  If there is a member-expr node,
		/// it is visited last.
		/// </remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				if (functionName != null)
				{
					functionName.Visit(v);
				}
				foreach (AstNode param in GetParams())
				{
					param.Visit(v);
				}
				GetBody().Visit(v);
				if (!isExpressionClosure)
				{
					if (memberExprNode != null)
					{
						memberExprNode.Visit(v);
					}
				}
			}
		}
	}
}
