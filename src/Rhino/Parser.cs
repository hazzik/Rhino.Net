/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino
{
	/// <summary>
	/// This class implements the JavaScript parser.<p>
	/// It is based on the SpiderMonkey C source files jsparse.c and jsparse.h in the
	/// jsref package.<p>
	/// The parser generates an
	/// <see cref="Rhino.Ast.AstRoot">Rhino.Ast.AstRoot</see>
	/// parse tree representing the source
	/// code.  No tree rewriting is permitted at this stage, so that the parse tree
	/// is a faithful representation of the source for frontend processing tools and
	/// IDEs.<p>
	/// This parser implementation is not intended to be reused after a parse
	/// finishes, and will throw an IllegalStateException() if invoked again.<p>
	/// </summary>
	/// <seealso cref="TokenStream">TokenStream</seealso>
	/// <author>Mike McCabe</author>
	/// <author>Brendan Eich</author>
	public class Parser
	{
		/// <summary>
		/// Maximum number of allowed function or constructor arguments,
		/// to follow SpiderMonkey.
		/// </summary>
		/// <remarks>
		/// Maximum number of allowed function or constructor arguments,
		/// to follow SpiderMonkey.
		/// </remarks>
		public const int ARGC_LIMIT = 1 << 16;

		internal const int CLEAR_TI_MASK = unchecked((int)(0xFFFF));

		internal const int TI_AFTER_EOL = 1 << 16;

		internal const int TI_CHECK_LABEL = 1 << 17;

		internal CompilerEnvirons compilerEnv;

		private ErrorReporter errorReporter;

		private IdeErrorReporter errorCollector;

		private string sourceURI;

		private char[] sourceChars;

		internal bool calledByCompileFunction;

		private bool parseFinished;

		private TokenStream ts;

		private int currentFlaggedToken = Token.EOF;

		private int currentToken;

		private int syntaxErrorCount;

		private IList<Comment> scannedComments;

		private Comment currentJsDocComment;

		protected internal int nestingOfFunction;

		private LabeledStatement currentLabel;

		private bool inDestructuringAssignment;

		protected internal bool inUseStrictDirective;

		internal ScriptNode currentScriptOrFn;

		internal Scope currentScope;

		private int endFlags;

		private bool inForInit;

		private IDictionary<string, LabeledStatement> labelSet;

		private IList<Loop> loopSet;

		private IList<Jump> loopAndSwitchSet;

		private int prevNameTokenStart;

		private string prevNameTokenString = string.Empty;

		private int prevNameTokenLineno;

		[System.Serializable]
		private class ParserException : Exception
		{
			// we use basically every class
			// TokenInformation flags : currentFlaggedToken stores them together
			// with token type
			// mask to clear token information bits
			// first token of the source line
			// indicates to check for label
			// ugly - set directly by Context
			// set when finished to prevent reuse
			// The following are per function variables and should be saved/restored
			// during function parsing.  See PerFunctionVariables class below.
			// bound temporarily during forStatement()
			// end of per function variables
			// Lacking 2-token lookahead, labels become a problem.
			// These vars store the token info of the last matched name,
			// iff it wasn't the last matched token.
			// Exception to unwind
		}

		public Parser() : this(new CompilerEnvirons())
		{
		}

		public Parser(CompilerEnvirons compilerEnv) : this(compilerEnv, compilerEnv.GetErrorReporter())
		{
		}

		public Parser(CompilerEnvirons compilerEnv, ErrorReporter errorReporter)
		{
			this.compilerEnv = compilerEnv;
			this.errorReporter = errorReporter;
			var ideReporter = errorReporter as IdeErrorReporter;
			if (ideReporter != null)
			{
				errorCollector = ideReporter;
			}
		}

		// Add a strict warning on the last matched token.
		internal virtual void AddStrictWarning(string messageId, string messageArg)
		{
			int beg = -1;
			int end = -1;
			if (ts != null)
			{
				beg = ts.tokenBeg;
				end = ts.tokenEnd - ts.tokenBeg;
			}
			AddStrictWarning(messageId, messageArg, beg, end);
		}

		internal virtual void AddStrictWarning(string messageId, string messageArg, int position, int length)
		{
			if (compilerEnv.IsStrictMode())
			{
				AddWarning(messageId, messageArg, position, length);
			}
		}

		internal virtual void AddWarning(string messageId, string messageArg)
		{
			int beg = -1;
			int end = -1;
			if (ts != null)
			{
				beg = ts.tokenBeg;
				end = ts.tokenEnd - ts.tokenBeg;
			}
			AddWarning(messageId, messageArg, beg, end);
		}

		internal virtual void AddWarning(string messageId, int position, int length)
		{
			AddWarning(messageId, null, position, length);
		}

		internal virtual void AddWarning(string messageId, string messageArg, int position, int length)
		{
			string message = LookupMessage(messageId, messageArg);
			if (compilerEnv.ReportWarningAsError())
			{
				AddError(messageId, messageArg, position, length);
			}
			else
			{
				if (errorCollector != null)
				{
					errorCollector.Warning(message, sourceURI, position, length);
				}
				else
				{
					errorReporter.Warning(message, sourceURI, ts.GetLineno(), ts.GetLine(), ts.GetOffset());
				}
			}
		}

		internal virtual void AddError(string messageId)
		{
			AddError(messageId, ts.tokenBeg, ts.tokenEnd - ts.tokenBeg);
		}

		internal virtual void AddError(string messageId, int position, int length)
		{
			AddError(messageId, null, position, length);
		}

		internal virtual void AddError(string messageId, string messageArg)
		{
			AddError(messageId, messageArg, ts.tokenBeg, ts.tokenEnd - ts.tokenBeg);
		}

		internal virtual void AddError(string messageId, string messageArg, int position, int length)
		{
			++syntaxErrorCount;
			string message = LookupMessage(messageId, messageArg);
			if (errorCollector != null)
			{
				errorCollector.Error(message, sourceURI, position, length);
			}
			else
			{
				int lineno = 1;
				int offset = 1;
				string line = string.Empty;
				if (ts != null)
				{
					// happens in some regression tests
					lineno = ts.GetLineno();
					line = ts.GetLine();
					offset = ts.GetOffset();
				}
				errorReporter.Error(message, sourceURI, lineno, line, offset);
			}
		}

		internal virtual string LookupMessage(string messageId)
		{
			return LookupMessage(messageId, null);
		}

		internal virtual string LookupMessage(string messageId, string messageArg)
		{
			return messageArg == null ? ScriptRuntime.GetMessage0(messageId) : ScriptRuntime.GetMessage1(messageId, messageArg);
		}

		internal virtual void ReportError(string messageId)
		{
			ReportError(messageId, null);
		}

		internal virtual void ReportError(string messageId, string messageArg)
		{
			if (ts == null)
			{
				// happens in some regression tests
				ReportError(messageId, messageArg, 1, 1);
			}
			else
			{
				ReportError(messageId, messageArg, ts.tokenBeg, ts.tokenEnd - ts.tokenBeg);
			}
		}

		internal virtual void ReportError(string messageId, int position, int length)
		{
			ReportError(messageId, null, position, length);
		}

		internal virtual void ReportError(string messageId, string messageArg, int position, int length)
		{
			AddError(messageId, position, length);
			if (!compilerEnv.RecoverFromErrors())
			{
				throw new Parser.ParserException();
			}
		}

		// Computes the absolute end offset of node N.
		// Use with caution!  Assumes n.getPosition() is -absolute-, which
		// is only true before the node is added to its parent.
		private int GetNodeEnd(AstNode n)
		{
			return n.GetPosition() + n.GetLength();
		}

		private void RecordComment(int lineno, string comment)
		{
			if (scannedComments == null)
			{
				scannedComments = new List<Comment>();
			}
			Comment commentNode = new Comment(ts.tokenBeg, ts.GetTokenLength(), ts.commentType, comment);
			if (ts.commentType == Token.CommentType.JSDOC && compilerEnv.IsRecordingLocalJsDocComments())
			{
				currentJsDocComment = commentNode;
			}
			commentNode.SetLineno(lineno);
			scannedComments.Add(commentNode);
		}

		private Comment GetAndResetJsDoc()
		{
			Comment saved = currentJsDocComment;
			currentJsDocComment = null;
			return saved;
		}

		private int GetNumberOfEols(string comment)
		{
			int lines = 0;
			for (int i = comment.Length - 1; i >= 0; i--)
			{
				if (comment[i] == '\n')
				{
					lines++;
				}
			}
			return lines;
		}

		// Returns the next token without consuming it.
		// If previous token was consumed, calls scanner to get new token.
		// If previous token was -not- consumed, returns it (idempotent).
		//
		// This function will not return a newline (Token.EOL - instead, it
		// gobbles newlines until it finds a non-newline token, and flags
		// that token as appearing just after a newline.
		//
		// This function will also not return a Token.COMMENT.  Instead, it
		// records comments in the scannedComments list.  If the token
		// returned by this function immediately follows a jsdoc comment,
		// the token is flagged as such.
		//
		// Note that this function always returned the un-flagged token!
		// The flags, if any, are saved in currentFlaggedToken.
		/// <exception cref="System.IO.IOException"></exception>
		private int PeekToken()
		{
			// By far the most common case:  last token hasn't been consumed,
			// so return already-peeked token.
			if (currentFlaggedToken != Token.EOF)
			{
				return currentToken;
			}
			int lineno = ts.GetLineno();
			int tt = ts.GetToken();
			bool sawEOL = false;
			// process comments and whitespace
			while (tt == Token.EOL || tt == Token.COMMENT)
			{
				if (tt == Token.EOL)
				{
					lineno++;
					sawEOL = true;
				}
				else
				{
					if (compilerEnv.IsRecordingComments())
					{
						string comment = ts.GetAndResetCurrentComment();
						RecordComment(lineno, comment);
						// Comments may contain multiple lines, get the number
						// of EoLs and increase the lineno
						lineno += GetNumberOfEols(comment);
					}
				}
				tt = ts.GetToken();
			}
			currentToken = tt;
			currentFlaggedToken = tt | (sawEOL ? TI_AFTER_EOL : 0);
			return currentToken;
		}

		// return unflagged token
		/// <exception cref="System.IO.IOException"></exception>
		private int PeekFlaggedToken()
		{
			PeekToken();
			return currentFlaggedToken;
		}

		private void ConsumeToken()
		{
			currentFlaggedToken = Token.EOF;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int NextToken()
		{
			int tt = PeekToken();
			ConsumeToken();
			return tt;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int NextFlaggedToken()
		{
			PeekToken();
			int ttFlagged = currentFlaggedToken;
			ConsumeToken();
			return ttFlagged;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool MatchToken(int toMatch)
		{
			if (PeekToken() != toMatch)
			{
				return false;
			}
			ConsumeToken();
			return true;
		}

		// Returns Token.EOL if the current token follows a newline, else returns
		// the current token.  Used in situations where we don't consider certain
		// token types valid if they are preceded by a newline.  One example is the
		// postfix ++ or -- operator, which has to be on the same line as its
		// operand.
		/// <exception cref="System.IO.IOException"></exception>
		private int PeekTokenOrEOL()
		{
			int tt = PeekToken();
			// Check for last peeked token flags
			if ((currentFlaggedToken & TI_AFTER_EOL) != 0)
			{
				tt = Token.EOL;
			}
			return tt;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool MustMatchToken(int toMatch, string messageId)
		{
			return MustMatchToken(toMatch, messageId, ts.tokenBeg, ts.tokenEnd - ts.tokenBeg);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool MustMatchToken(int toMatch, string msgId, int pos, int len)
		{
			if (MatchToken(toMatch))
			{
				return true;
			}
			ReportError(msgId, pos, len);
			return false;
		}

		private void MustHaveXML()
		{
			if (!compilerEnv.IsXmlAvailable())
			{
				ReportError("msg.XML.not.available");
			}
		}

		public virtual bool Eof()
		{
			return ts.Eof();
		}

		internal virtual bool InsideFunction()
		{
			return nestingOfFunction != 0;
		}

		internal virtual void PushScope(Scope scope)
		{
			Scope parent = scope.GetParentScope();
			// During codegen, parent scope chain may already be initialized,
			// in which case we just need to set currentScope variable.
			if (parent != null)
			{
				if (parent != currentScope)
				{
					CodeBug();
				}
			}
			else
			{
				currentScope.AddChildScope(scope);
			}
			currentScope = scope;
		}

		internal virtual void PopScope()
		{
			currentScope = currentScope.GetParentScope();
		}

		private void EnterLoop(Loop loop)
		{
			if (loopSet == null)
			{
				loopSet = new List<Loop>();
			}
			loopSet.Add(loop);
			if (loopAndSwitchSet == null)
			{
				loopAndSwitchSet = new List<Jump>();
			}
			loopAndSwitchSet.Add(loop);
			PushScope(loop);
			if (currentLabel != null)
			{
				currentLabel.SetStatement(loop);
				currentLabel.GetFirstLabel().SetLoop(loop);
				// This is the only time during parsing that we set a node's parent
				// before parsing the children.  In order for the child node offsets
				// to be correct, we adjust the loop's reported position back to an
				// absolute source offset, and restore it when we call exitLoop().
				loop.SetRelative(-currentLabel.GetPosition());
			}
		}

		private void ExitLoop()
		{
			int index = loopSet.Count - 1;
			Loop loop = loopSet[index];
			loopSet.RemoveAt(index);
			loopAndSwitchSet.RemoveAt(loopAndSwitchSet.Count - 1);
			if (loop.GetParent() != null)
			{
				// see comment in enterLoop
				loop.SetRelative(loop.GetParent().GetPosition());
			}
			PopScope();
		}

		private void EnterSwitch(SwitchStatement node)
		{
			if (loopAndSwitchSet == null)
			{
				loopAndSwitchSet = new List<Jump>();
			}
			loopAndSwitchSet.Add(node);
		}

		private void ExitSwitch()
		{
			loopAndSwitchSet.RemoveAt(loopAndSwitchSet.Count - 1);
		}

		/// <summary>Builds a parse tree from the given source string.</summary>
		/// <remarks>Builds a parse tree from the given source string.</remarks>
		/// <returns>
		/// an
		/// <see cref="Rhino.Ast.AstRoot">Rhino.Ast.AstRoot</see>
		/// object representing the parsed program.  If
		/// the parse fails,
		/// <code>null</code>
		/// will be returned.  (The parse failure will
		/// result in a call to the
		/// <see cref="ErrorReporter">ErrorReporter</see>
		/// from
		/// <see cref="CompilerEnvirons">CompilerEnvirons</see>
		/// .)
		/// </returns>
		public virtual AstRoot Parse(string sourceString, string uri, int lineno)
		{
			if (parseFinished)
			{
				throw new InvalidOperationException("parser reused");
			}
			this.sourceURI = uri;
			if (compilerEnv.IsIdeMode())
			{
				this.sourceChars = sourceString.ToCharArray();
			}
			this.ts = new TokenStream(this, null, sourceString, lineno);
			try
			{
				return Parse();
			}
			catch (IOException)
			{
				// Should never happen
				throw new InvalidOperationException();
			}
			finally
			{
				parseFinished = true;
			}
		}

		/// <summary>Builds a parse tree from the given sourcereader.</summary>
		/// <remarks>Builds a parse tree from the given sourcereader.</remarks>
		/// <seealso cref="Parse(string, string, int)">Parse(string, string, int)</seealso>
		/// <exception cref="System.IO.IOException">
		/// if the
		/// <see cref="System.IO.TextReader">System.IO.TextReader</see>
		/// encounters an error
		/// </exception>
		public virtual AstRoot Parse(TextReader reader, string uri, int lineno)
		{
			if (parseFinished)
			{
				throw new InvalidOperationException("parser reused");
			}
			if (compilerEnv.IsIdeMode())
			{
				return Parse(reader.ReadToEnd(), uri, lineno);
			}
			try
			{
				this.sourceURI = uri;
				ts = new TokenStream(this, reader, null, lineno);
				return Parse();
			}
			finally
			{
				parseFinished = true;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstRoot Parse()
		{
			int pos = 0;
			AstRoot root = new AstRoot(pos);
			currentScope = currentScriptOrFn = root;
			int baseLineno = ts.lineno;
			// line number where source starts
			int end = pos;
			// in case source is empty
			bool inDirectivePrologue = true;
			bool savedStrictMode = inUseStrictDirective;
			// TODO: eval code should get strict mode from invoking code
			inUseStrictDirective = false;
			try
			{
				for (; ; )
				{
					int tt = PeekToken();
					if (tt <= Token.EOF)
					{
						break;
					}
					AstNode n;
					if (tt == Token.FUNCTION)
					{
						ConsumeToken();
						try
						{
							n = Function(calledByCompileFunction ? FunctionNode.FUNCTION_EXPRESSION : FunctionNode.FUNCTION_STATEMENT);
						}
						catch (Parser.ParserException)
						{
							break;
						}
					}
					else
					{
						n = Statement();
						if (inDirectivePrologue)
						{
							string directive = GetDirective(n);
							if (directive == null)
							{
								inDirectivePrologue = false;
							}
							else
							{
								if (directive.Equals("use strict"))
								{
									inUseStrictDirective = true;
									root.SetInStrictMode(true);
								}
							}
						}
					}
					end = GetNodeEnd(n);
					root.AddChildToBack(n);
					n.SetParent(root);
				}
			}
			catch (StackOverflowException)
			{
				string msg = LookupMessage("msg.too.deep.parser.recursion");
				if (!compilerEnv.IsIdeMode())
				{
					throw Context.ReportRuntimeError(msg, sourceURI, ts.lineno, null, 0);
				}
			}
			finally
			{
				inUseStrictDirective = savedStrictMode;
			}
			if (this.syntaxErrorCount != 0)
			{
				string msg = this.syntaxErrorCount.ToString();
				msg = LookupMessage("msg.got.syntax.errors", msg);
				if (!compilerEnv.IsIdeMode())
				{
					throw errorReporter.RuntimeError(msg, sourceURI, baseLineno, null, 0);
				}
			}
			// add comments to root in lexical order
			if (scannedComments != null)
			{
				// If we find a comment beyond end of our last statement or
				// function, extend the root bounds to the end of that comment.
				int last = scannedComments.Count - 1;
				end = Math.Max(end, GetNodeEnd(scannedComments[last]));
				foreach (Comment c in scannedComments)
				{
					root.AddComment(c);
				}
			}
			root.SetLength(end - pos);
			root.SetSourceName(sourceURI);
			root.SetBaseLineno(baseLineno);
			root.SetEndLineno(ts.lineno);
			return root;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode ParseFunctionBody()
		{
			bool isExpressionClosure = false;
			if (!MatchToken(Token.LC))
			{
				if (compilerEnv.GetLanguageVersion() < LanguageVersion.VERSION_1_8)
				{
					ReportError("msg.no.brace.body");
				}
				else
				{
					isExpressionClosure = true;
				}
			}
			++nestingOfFunction;
			int pos = ts.tokenBeg;
			Block pn = new Block(pos);
			// starts at LC position
			bool inDirectivePrologue = true;
			bool savedStrictMode = inUseStrictDirective;
			// Don't set 'inUseStrictDirective' to false: inherit strict mode.
			pn.SetLineno(ts.lineno);
			try
			{
				if (isExpressionClosure)
				{
					ReturnStatement n = new ReturnStatement(ts.lineno);
					n.SetReturnValue(AssignExpr());
					// expression closure flag is required on both nodes
					n.PutProp(Node.EXPRESSION_CLOSURE_PROP, true);
					pn.PutProp(Node.EXPRESSION_CLOSURE_PROP, true);
					pn.AddStatement(n);
				}
				else
				{
					for (; ; )
					{
						AstNode n;
						int tt = PeekToken();
						switch (tt)
						{
							case Token.ERROR:
							case Token.EOF:
							case Token.RC:
							{
								goto bodyLoop_break;
							}

							case Token.FUNCTION:
							{
								ConsumeToken();
								n = Function(FunctionNode.FUNCTION_STATEMENT);
								break;
							}

							default:
							{
								n = Statement();
								if (inDirectivePrologue)
								{
									string directive = GetDirective(n);
									if (directive == null)
									{
										inDirectivePrologue = false;
									}
									else
									{
										if (directive.Equals("use strict"))
										{
											inUseStrictDirective = true;
										}
									}
								}
								break;
							}
						}
						pn.AddStatement(n);
bodyLoop_continue: ;
					}
bodyLoop_break: ;
				}
			}
			catch (Parser.ParserException)
			{
			}
			finally
			{
				// Ignore it
				--nestingOfFunction;
				inUseStrictDirective = savedStrictMode;
			}
			int end = ts.tokenEnd;
			GetAndResetJsDoc();
			if (!isExpressionClosure && MustMatchToken(Token.RC, "msg.no.brace.after.body"))
			{
				end = ts.tokenEnd;
			}
			pn.SetLength(end - pos);
			return pn;
		}

		private string GetDirective(AstNode n)
		{
			var expressionStatement = n as ExpressionStatement;
			if (expressionStatement != null)
			{
				var stringLiteral = expressionStatement.GetExpression() as StringLiteral;
				if (stringLiteral != null)
				{
					return stringLiteral.GetValue();
				}
			}
			return null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ParseFunctionParams(FunctionNode fnNode)
		{
			if (MatchToken(Token.RP))
			{
				fnNode.SetRp(ts.tokenBeg - fnNode.GetPosition());
				return;
			}
			// Would prefer not to call createDestructuringAssignment until codegen,
			// but the symbol definitions have to happen now, before body is parsed.
			IDictionary<string, Node> destructuring = null;
			ICollection<string> paramNames = new HashSet<string>();
			do
			{
				int tt = PeekToken();
				if (tt == Token.LB || tt == Token.LC)
				{
					AstNode expr = DestructuringPrimaryExpr();
					MarkDestructuring(expr);
					fnNode.AddParam(expr);
					// Destructuring assignment for parameters: add a dummy
					// parameter name, and add a statement to the body to initialize
					// variables from the destructuring assignment
					if (destructuring == null)
					{
						destructuring = new Dictionary<string, Node>();
					}
					string pname = currentScriptOrFn.GetNextTempName();
					DefineSymbol(Token.LP, pname, false);
					destructuring[pname] = expr;
				}
				else
				{
					if (MustMatchToken(Token.NAME, "msg.no.parm"))
					{
						fnNode.AddParam(CreateNameNode());
						string paramName = ts.GetString();
						DefineSymbol(Token.LP, paramName);
						if (this.inUseStrictDirective)
						{
							if ("eval".Equals(paramName) || "arguments".Equals(paramName))
							{
								ReportError("msg.bad.id.strict", paramName);
							}
							if (paramNames.Contains(paramName))
							{
								AddError("msg.dup.param.strict", paramName);
							}
							paramNames.Add(paramName);
						}
					}
					else
					{
						fnNode.AddParam(MakeErrorNode());
					}
				}
			}
			while (MatchToken(Token.COMMA));
			if (destructuring != null)
			{
				Node destructuringNode = new Node(Token.COMMA);
				// Add assignment helper for each destructuring parameter
				foreach (KeyValuePair<string, Node> param in destructuring)
				{
					Node assign = CreateDestructuringAssignment(Token.VAR, param.Value, CreateName(param.Key));
					destructuringNode.AddChildToBack(assign);
				}
				fnNode.PutProp(Node.DESTRUCTURING_PARAMS, destructuringNode);
			}
			if (MustMatchToken(Token.RP, "msg.no.paren.after.parms"))
			{
				fnNode.SetRp(ts.tokenBeg - fnNode.GetPosition());
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private FunctionNode Function(int type)
		{
			int syntheticType = type;
			int baseLineno = ts.lineno;
			// line number where source starts
			int functionSourceStart = ts.tokenBeg;
			// start of "function" kwd
			Name name = null;
			AstNode memberExprNode = null;
			if (MatchToken(Token.NAME))
			{
				name = CreateNameNode(true, Token.NAME);
				if (inUseStrictDirective)
				{
					string id = name.GetIdentifier();
					if ("eval".Equals(id) || "arguments".Equals(id))
					{
						ReportError("msg.bad.id.strict", id);
					}
				}
				if (!MatchToken(Token.LP))
				{
					if (compilerEnv.IsAllowMemberExprAsFunctionName())
					{
						AstNode memberExprHead = name;
						name = null;
						memberExprNode = MemberExprTail(false, memberExprHead);
					}
					MustMatchToken(Token.LP, "msg.no.paren.parms");
				}
			}
			else
			{
				if (MatchToken(Token.LP))
				{
				}
				else
				{
					// Anonymous function:  leave name as null
					if (compilerEnv.IsAllowMemberExprAsFunctionName())
					{
						// Note that memberExpr can not start with '(' like
						// in function (1+2).toString(), because 'function (' already
						// processed as anonymous function
						memberExprNode = MemberExpr(false);
					}
					MustMatchToken(Token.LP, "msg.no.paren.parms");
				}
			}
			int lpPos = currentToken == Token.LP ? ts.tokenBeg : -1;
			if (memberExprNode != null)
			{
				syntheticType = FunctionNode.FUNCTION_EXPRESSION;
			}
			if (syntheticType != FunctionNode.FUNCTION_EXPRESSION && name != null && name.Length() > 0)
			{
				// Function statements define a symbol in the enclosing scope
				DefineSymbol(Token.FUNCTION, name.GetIdentifier());
			}
			FunctionNode fnNode = new FunctionNode(functionSourceStart, name);
			fnNode.SetFunctionType(type);
			if (lpPos != -1)
			{
				fnNode.SetLp(lpPos - functionSourceStart);
			}
			fnNode.SetJsDocNode(GetAndResetJsDoc());
			Parser.PerFunctionVariables savedVars = new Parser.PerFunctionVariables(this, fnNode);
			try
			{
				ParseFunctionParams(fnNode);
				fnNode.SetBody(ParseFunctionBody());
				fnNode.SetEncodedSourceBounds(functionSourceStart, ts.tokenEnd);
				fnNode.SetLength(ts.tokenEnd - functionSourceStart);
				if (compilerEnv.IsStrictMode() && !fnNode.GetBody().HasConsistentReturnUsage())
				{
					string msg = (name != null && name.Length() > 0) ? "msg.no.return.value" : "msg.anon.no.return.value";
					AddStrictWarning(msg, name == null ? string.Empty : name.GetIdentifier());
				}
			}
			finally
			{
				savedVars.Restore();
			}
			if (memberExprNode != null)
			{
				// TODO(stevey): fix missing functionality
				Kit.CodeBug();
				fnNode.SetMemberExprNode(memberExprNode);
			}
			// rewrite later
			fnNode.SetSourceName(sourceURI);
			fnNode.SetBaseLineno(baseLineno);
			fnNode.SetEndLineno(ts.lineno);
			// Set the parent scope.  Needed for finding undeclared vars.
			// Have to wait until after parsing the function to set its parent
			// scope, since defineSymbol needs the defining-scope check to stop
			// at the function boundary when checking for redeclarations.
			if (compilerEnv.IsIdeMode())
			{
				fnNode.SetParentScope(currentScope);
			}
			return fnNode;
		}

		// This function does not match the closing RC: the caller matches
		// the RC so it can provide a suitable error message if not matched.
		// This means it's up to the caller to set the length of the node to
		// include the closing RC.  The node start pos is set to the
		// absolute buffer start position, and the caller should fix it up
		// to be relative to the parent node.  All children of this block
		// node are given relative start positions and correct lengths.
		/// <exception cref="System.IO.IOException"></exception>
		private AstNode Statements(AstNode parent)
		{
			if (currentToken != Token.LC && !compilerEnv.IsIdeMode())
			{
				// assertion can be invalid in bad code
				CodeBug();
			}
			int pos = ts.tokenBeg;
			AstNode block = parent != null ? parent : new Block(pos);
			block.SetLineno(ts.lineno);
			int tt;
			while ((tt = PeekToken()) > Token.EOF && tt != Token.RC)
			{
				block.AddChild(Statement());
			}
			block.SetLength(ts.tokenBeg - pos);
			return block;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode Statements()
		{
			return Statements(null);
		}

		private class ConditionData
		{
			internal AstNode condition;

			internal int lp = -1;

			internal int rp = -1;
		}

		// parse and return a parenthesized expression
		/// <exception cref="System.IO.IOException"></exception>
		private Parser.ConditionData Condition()
		{
			Parser.ConditionData data = new Parser.ConditionData();
			if (MustMatchToken(Token.LP, "msg.no.paren.cond"))
			{
				data.lp = ts.tokenBeg;
			}
			data.condition = Expr();
			if (MustMatchToken(Token.RP, "msg.no.paren.after.cond"))
			{
				data.rp = ts.tokenBeg;
			}
			// Report strict warning on code like "if (a = 7) ...". Suppress the
			// warning if the condition is parenthesized, like "if ((a = 7)) ...".
			if (data.condition is Assignment)
			{
				AddStrictWarning("msg.equal.as.assign", string.Empty, data.condition.GetPosition(), data.condition.GetLength());
			}
			return data;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode Statement()
		{
			int pos = ts.tokenBeg;
			try
			{
				AstNode pn = StatementHelper();
				if (pn != null)
				{
					if (compilerEnv.IsStrictMode() && !pn.HasSideEffects())
					{
						int beg = pn.GetPosition();
						beg = Math.Max(beg, LineBeginningFor(beg));
						AddStrictWarning(pn is EmptyStatement ? "msg.extra.trailing.semi" : "msg.no.side.effects", string.Empty, beg, NodeEnd(pn) - beg);
					}
					return pn;
				}
			}
			catch (Parser.ParserException)
			{
			}
			// an ErrorNode was added to the ErrorReporter
			// error:  skip ahead to a probable statement boundary
			for (; ; )
			{
				int tt = PeekTokenOrEOL();
				ConsumeToken();
				switch (tt)
				{
					case Token.ERROR:
					case Token.EOF:
					case Token.EOL:
					case Token.SEMI:
					{
						goto guessingStatementEnd_break;
					}
				}
guessingStatementEnd_continue: ;
			}
guessingStatementEnd_break: ;
			// We don't make error nodes explicitly part of the tree;
			// they get added to the ErrorReporter.  May need to do
			// something different here.
			return new EmptyStatement(pos, ts.tokenBeg - pos);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode StatementHelper()
		{
			// If the statement is set, then it's been told its label by now.
			if (currentLabel != null && currentLabel.GetStatement() != null)
			{
				currentLabel = null;
			}
			AstNode pn = null;
			int tt = PeekToken();
			int pos = ts.tokenBeg;
			switch (tt)
			{
				case Token.IF:
				{
					return IfStatement();
				}

				case Token.SWITCH:
				{
					return SwitchStatement();
				}

				case Token.WHILE:
				{
					return WhileLoop();
				}

				case Token.DO:
				{
					return DoLoop();
				}

				case Token.FOR:
				{
					return ForLoop();
				}

				case Token.TRY:
				{
					return TryStatement();
				}

				case Token.THROW:
				{
					pn = ThrowStatement();
					break;
				}

				case Token.BREAK:
				{
					pn = BreakStatement();
					break;
				}

				case Token.CONTINUE:
				{
					pn = ContinueStatement();
					break;
				}

				case Token.WITH:
				{
					if (this.inUseStrictDirective)
					{
						ReportError("msg.no.with.strict");
					}
					return WithStatement();
				}

				case Token.CONST:
				case Token.VAR:
				{
					ConsumeToken();
					int lineno = ts.lineno;
					pn = Variables(currentToken, ts.tokenBeg, true);
					pn.SetLineno(lineno);
					break;
				}

				case Token.LET:
				{
					pn = LetStatement();
					if (pn is VariableDeclaration && PeekToken() == Token.SEMI)
					{
						break;
					}
					return pn;
				}

				case Token.RETURN:
				case Token.YIELD:
				{
					pn = ReturnOrYield(tt, false);
					break;
				}

				case Token.DEBUGGER:
				{
					ConsumeToken();
					pn = new KeywordLiteral(ts.tokenBeg, ts.tokenEnd - ts.tokenBeg, tt);
					pn.SetLineno(ts.lineno);
					break;
				}

				case Token.LC:
				{
					return Block();
				}

				case Token.ERROR:
				{
					ConsumeToken();
					return MakeErrorNode();
				}

				case Token.SEMI:
				{
					ConsumeToken();
					pos = ts.tokenBeg;
					pn = new EmptyStatement(pos, ts.tokenEnd - pos);
					pn.SetLineno(ts.lineno);
					return pn;
				}

				case Token.FUNCTION:
				{
					ConsumeToken();
					return Function(FunctionNode.FUNCTION_EXPRESSION_STATEMENT);
				}

				case Token.DEFAULT:
				{
					pn = DefaultXmlNamespace();
					break;
				}

				case Token.NAME:
				{
					pn = NameOrLabel();
					if (pn is ExpressionStatement)
					{
						break;
					}
					return pn;
				}

				default:
				{
					// LabeledStatement
					int lineno = ts.lineno;
					pn = new ExpressionStatement(Expr(), !InsideFunction());
					pn.SetLineno(lineno);
					break;
				}
			}
			AutoInsertSemicolon(pn);
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AutoInsertSemicolon(AstNode pn)
		{
			int ttFlagged = PeekFlaggedToken();
			int pos = pn.GetPosition();
			switch (ttFlagged & CLEAR_TI_MASK)
			{
				case Token.SEMI:
				{
					// Consume ';' as a part of expression
					ConsumeToken();
					// extend the node bounds to include the semicolon.
					pn.SetLength(ts.tokenEnd - pos);
					break;
				}

				case Token.ERROR:
				case Token.EOF:
				case Token.RC:
				{
					// Autoinsert ;
					WarnMissingSemi(pos, NodeEnd(pn));
					break;
				}

				default:
				{
					if ((ttFlagged & TI_AFTER_EOL) == 0)
					{
						// Report error if no EOL or autoinsert ; otherwise
						ReportError("msg.no.semi.stmt");
					}
					else
					{
						WarnMissingSemi(pos, NodeEnd(pn));
					}
					break;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private IfStatement IfStatement()
		{
			if (currentToken != Token.IF)
			{
				CodeBug();
			}
			ConsumeToken();
			int pos = ts.tokenBeg;
			int lineno = ts.lineno;
			int elsePos = -1;
			Parser.ConditionData data = Condition();
			AstNode ifTrue = Statement();
			AstNode ifFalse = null;
			if (MatchToken(Token.ELSE))
			{
				elsePos = ts.tokenBeg - pos;
				ifFalse = Statement();
			}
			int end = GetNodeEnd(ifFalse != null ? ifFalse : ifTrue);
			IfStatement pn = new IfStatement(pos, end - pos);
			pn.SetCondition(data.condition);
			pn.SetParens(data.lp - pos, data.rp - pos);
			pn.SetThenPart(ifTrue);
			pn.SetElsePart(ifFalse);
			pn.SetElsePosition(elsePos);
			pn.SetLineno(lineno);
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private SwitchStatement SwitchStatement()
		{
			if (currentToken != Token.SWITCH)
			{
				CodeBug();
			}
			ConsumeToken();
			int pos = ts.tokenBeg;
			SwitchStatement pn = new SwitchStatement(pos);
			if (MustMatchToken(Token.LP, "msg.no.paren.switch"))
			{
				pn.SetLp(ts.tokenBeg - pos);
			}
			pn.SetLineno(ts.lineno);
			AstNode discriminant = Expr();
			pn.SetExpression(discriminant);
			EnterSwitch(pn);
			try
			{
				if (MustMatchToken(Token.RP, "msg.no.paren.after.switch"))
				{
					pn.SetRp(ts.tokenBeg - pos);
				}
				MustMatchToken(Token.LC, "msg.no.brace.switch");
				bool hasDefault = false;
				int tt;
				for (; ; )
				{
					tt = NextToken();
					int casePos = ts.tokenBeg;
					int caseLineno = ts.lineno;
					AstNode caseExpression = null;
					switch (tt)
					{
						case Token.RC:
						{
							pn.SetLength(ts.tokenEnd - pos);
							goto switchLoop_break;
						}

						case Token.CASE:
						{
							caseExpression = Expr();
							MustMatchToken(Token.COLON, "msg.no.colon.case");
							break;
						}

						case Token.DEFAULT:
						{
							if (hasDefault)
							{
								ReportError("msg.double.switch.default");
							}
							hasDefault = true;
							caseExpression = null;
							MustMatchToken(Token.COLON, "msg.no.colon.case");
							break;
						}

						default:
						{
							ReportError("msg.bad.switch");
							goto switchLoop_break;
						}
					}
					SwitchCase caseNode = new SwitchCase(casePos);
					caseNode.SetExpression(caseExpression);
					caseNode.SetLength(ts.tokenEnd - pos);
					// include colon
					caseNode.SetLineno(caseLineno);
					while ((tt = PeekToken()) != Token.RC && tt != Token.CASE && tt != Token.DEFAULT && tt != Token.EOF)
					{
						caseNode.AddStatement(Statement());
					}
					// updates length
					pn.AddCase(caseNode);
switchLoop_continue: ;
				}
switchLoop_break: ;
			}
			finally
			{
				ExitSwitch();
			}
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private WhileLoop WhileLoop()
		{
			if (currentToken != Token.WHILE)
			{
				CodeBug();
			}
			ConsumeToken();
			int pos = ts.tokenBeg;
			WhileLoop pn = new WhileLoop(pos);
			pn.SetLineno(ts.lineno);
			EnterLoop(pn);
			try
			{
				Parser.ConditionData data = Condition();
				pn.SetCondition(data.condition);
				pn.SetParens(data.lp - pos, data.rp - pos);
				AstNode body = Statement();
				pn.SetLength(GetNodeEnd(body) - pos);
				pn.SetBody(body);
			}
			finally
			{
				ExitLoop();
			}
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private DoLoop DoLoop()
		{
			if (currentToken != Token.DO)
			{
				CodeBug();
			}
			ConsumeToken();
			int pos = ts.tokenBeg;
			int end;
			DoLoop pn = new DoLoop(pos);
			pn.SetLineno(ts.lineno);
			EnterLoop(pn);
			try
			{
				AstNode body = Statement();
				MustMatchToken(Token.WHILE, "msg.no.while.do");
				pn.SetWhilePosition(ts.tokenBeg - pos);
				Parser.ConditionData data = Condition();
				pn.SetCondition(data.condition);
				pn.SetParens(data.lp - pos, data.rp - pos);
				end = GetNodeEnd(body);
				pn.SetBody(body);
			}
			finally
			{
				ExitLoop();
			}
			// Always auto-insert semicolon to follow SpiderMonkey:
			// It is required by ECMAScript but is ignored by the rest of
			// world, see bug 238945
			if (MatchToken(Token.SEMI))
			{
				end = ts.tokenEnd;
			}
			pn.SetLength(end - pos);
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private Loop ForLoop()
		{
			if (currentToken != Token.FOR)
			{
				CodeBug();
			}
			ConsumeToken();
			int forPos = ts.tokenBeg;
			int lineno = ts.lineno;
			bool isForEach = false;
			bool isForIn = false;
			int eachPos = -1;
			int inPos = -1;
			int lp = -1;
			int rp = -1;
			AstNode init = null;
			// init is also foo in 'foo in object'
			AstNode cond = null;
			// cond is also object in 'foo in object'
			AstNode incr = null;
			Loop pn = null;
			Scope tempScope = new Scope();
			PushScope(tempScope);
			// decide below what AST class to use
			try
			{
				// See if this is a for each () instead of just a for ()
				if (MatchToken(Token.NAME))
				{
					if ("each".Equals(ts.GetString()))
					{
						isForEach = true;
						eachPos = ts.tokenBeg - forPos;
					}
					else
					{
						ReportError("msg.no.paren.for");
					}
				}
				if (MustMatchToken(Token.LP, "msg.no.paren.for"))
				{
					lp = ts.tokenBeg - forPos;
				}
				int tt = PeekToken();
				init = ForLoopInit(tt);
				if (MatchToken(Token.IN))
				{
					isForIn = true;
					inPos = ts.tokenBeg - forPos;
					cond = Expr();
				}
				else
				{
					// object over which we're iterating
					// ordinary for-loop
					MustMatchToken(Token.SEMI, "msg.no.semi.for");
					if (PeekToken() == Token.SEMI)
					{
						// no loop condition
						cond = new EmptyExpression(ts.tokenBeg, 1);
						cond.SetLineno(ts.lineno);
					}
					else
					{
						cond = Expr();
					}
					MustMatchToken(Token.SEMI, "msg.no.semi.for.cond");
					int tmpPos = ts.tokenEnd;
					if (PeekToken() == Token.RP)
					{
						incr = new EmptyExpression(tmpPos, 1);
						incr.SetLineno(ts.lineno);
					}
					else
					{
						incr = Expr();
					}
				}
				if (MustMatchToken(Token.RP, "msg.no.paren.for.ctrl"))
				{
					rp = ts.tokenBeg - forPos;
				}
				if (isForIn)
				{
					ForInLoop fis = new ForInLoop(forPos);
					var variableDeclaration = init as VariableDeclaration;
					if (variableDeclaration != null)
					{
						// check that there was only one variable given
						if (variableDeclaration.GetVariables().Count > 1)
						{
							ReportError("msg.mult.index");
						}
					}
					fis.SetIterator(init);
					fis.SetIteratedObject(cond);
					fis.SetInPosition(inPos);
					fis.SetIsForEach(isForEach);
					fis.SetEachPosition(eachPos);
					pn = fis;
				}
				else
				{
					ForLoop fl = new ForLoop(forPos);
					fl.SetInitializer(init);
					fl.SetCondition(cond);
					fl.SetIncrement(incr);
					pn = fl;
				}
				// replace temp scope with the new loop object
				currentScope.ReplaceWith(pn);
				PopScope();
				// We have to parse the body -after- creating the loop node,
				// so that the loop node appears in the loopSet, allowing
				// break/continue statements to find the enclosing loop.
				EnterLoop(pn);
				try
				{
					AstNode body = Statement();
					pn.SetLength(GetNodeEnd(body) - forPos);
					pn.SetBody(body);
				}
				finally
				{
					ExitLoop();
				}
			}
			finally
			{
				if (currentScope == tempScope)
				{
					PopScope();
				}
			}
			pn.SetParens(lp, rp);
			pn.SetLineno(lineno);
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode ForLoopInit(int tt)
		{
			try
			{
				inForInit = true;
				// checked by variables() and relExpr()
				AstNode init = null;
				if (tt == Token.SEMI)
				{
					init = new EmptyExpression(ts.tokenBeg, 1);
					init.SetLineno(ts.lineno);
				}
				else
				{
					if (tt == Token.VAR || tt == Token.LET)
					{
						ConsumeToken();
						init = Variables(tt, ts.tokenBeg, false);
					}
					else
					{
						init = Expr();
						MarkDestructuring(init);
					}
				}
				return init;
			}
			finally
			{
				inForInit = false;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private TryStatement TryStatement()
		{
			if (currentToken != Token.TRY)
			{
				CodeBug();
			}
			ConsumeToken();
			// Pull out JSDoc info and reset it before recursing.
			Comment jsdocNode = GetAndResetJsDoc();
			int tryPos = ts.tokenBeg;
			int lineno = ts.lineno;
			int finallyPos = -1;
			if (PeekToken() != Token.LC)
			{
				ReportError("msg.no.brace.try");
			}
			AstNode tryBlock = Statement();
			int tryEnd = GetNodeEnd(tryBlock);
			IList<CatchClause> clauses = null;
			bool sawDefaultCatch = false;
			int peek = PeekToken();
			if (peek == Token.CATCH)
			{
				while (MatchToken(Token.CATCH))
				{
					int catchLineNum = ts.lineno;
					if (sawDefaultCatch)
					{
						ReportError("msg.catch.unreachable");
					}
					int catchPos = ts.tokenBeg;
					int lp = -1;
					int rp = -1;
					int guardPos = -1;
					if (MustMatchToken(Token.LP, "msg.no.paren.catch"))
					{
						lp = ts.tokenBeg;
					}
					MustMatchToken(Token.NAME, "msg.bad.catchcond");
					Name varName = CreateNameNode();
					string varNameString = varName.GetIdentifier();
					if (inUseStrictDirective)
					{
						if ("eval".Equals(varNameString) || "arguments".Equals(varNameString))
						{
							ReportError("msg.bad.id.strict", varNameString);
						}
					}
					AstNode catchCond = null;
					if (MatchToken(Token.IF))
					{
						guardPos = ts.tokenBeg;
						catchCond = Expr();
					}
					else
					{
						sawDefaultCatch = true;
					}
					if (MustMatchToken(Token.RP, "msg.bad.catchcond"))
					{
						rp = ts.tokenBeg;
					}
					MustMatchToken(Token.LC, "msg.no.brace.catchblock");
					Block catchBlock = (Block)Statements();
					tryEnd = GetNodeEnd(catchBlock);
					CatchClause catchNode = new CatchClause(catchPos);
					catchNode.SetVarName(varName);
					catchNode.SetCatchCondition(catchCond);
					catchNode.SetBody(catchBlock);
					if (guardPos != -1)
					{
						catchNode.SetIfPosition(guardPos - catchPos);
					}
					catchNode.SetParens(lp, rp);
					catchNode.SetLineno(catchLineNum);
					if (MustMatchToken(Token.RC, "msg.no.brace.after.body"))
					{
						tryEnd = ts.tokenEnd;
					}
					catchNode.SetLength(tryEnd - catchPos);
					if (clauses == null)
					{
						clauses = new List<CatchClause>();
					}
					clauses.Add(catchNode);
				}
			}
			else
			{
				if (peek != Token.FINALLY)
				{
					MustMatchToken(Token.FINALLY, "msg.try.no.catchfinally");
				}
			}
			AstNode finallyBlock = null;
			if (MatchToken(Token.FINALLY))
			{
				finallyPos = ts.tokenBeg;
				finallyBlock = Statement();
				tryEnd = GetNodeEnd(finallyBlock);
			}
			TryStatement pn = new TryStatement(tryPos, tryEnd - tryPos);
			pn.SetTryBlock(tryBlock);
			pn.SetCatchClauses(clauses);
			pn.SetFinallyBlock(finallyBlock);
			if (finallyPos != -1)
			{
				pn.SetFinallyPosition(finallyPos - tryPos);
			}
			pn.SetLineno(lineno);
			if (jsdocNode != null)
			{
				pn.SetJsDocNode(jsdocNode);
			}
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private ThrowStatement ThrowStatement()
		{
			if (currentToken != Token.THROW)
			{
				CodeBug();
			}
			ConsumeToken();
			int pos = ts.tokenBeg;
			int lineno = ts.lineno;
			if (PeekTokenOrEOL() == Token.EOL)
			{
				// ECMAScript does not allow new lines before throw expression,
				// see bug 256617
				ReportError("msg.bad.throw.eol");
			}
			AstNode expr = Expr();
			ThrowStatement pn = new ThrowStatement(pos, GetNodeEnd(expr), expr);
			pn.SetLineno(lineno);
			return pn;
		}

		// If we match a NAME, consume the token and return the statement
		// with that label.  If the name does not match an existing label,
		// reports an error.  Returns the labeled statement node, or null if
		// the peeked token was not a name.  Side effect:  sets scanner token
		// information for the label identifier (tokenBeg, tokenEnd, etc.)
		/// <exception cref="System.IO.IOException"></exception>
		private LabeledStatement MatchJumpLabelName()
		{
			LabeledStatement label = null;
			if (PeekTokenOrEOL() == Token.NAME)
			{
				ConsumeToken();
				if (labelSet != null)
				{
					label = labelSet.Get(ts.GetString());
				}
				if (label == null)
				{
					ReportError("msg.undef.label");
				}
			}
			return label;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private BreakStatement BreakStatement()
		{
			if (currentToken != Token.BREAK)
			{
				CodeBug();
			}
			ConsumeToken();
			int lineno = ts.lineno;
			int pos = ts.tokenBeg;
			int end = ts.tokenEnd;
			Name breakLabel = null;
			if (PeekTokenOrEOL() == Token.NAME)
			{
				breakLabel = CreateNameNode();
				end = GetNodeEnd(breakLabel);
			}
			// matchJumpLabelName only matches if there is one
			LabeledStatement labels = MatchJumpLabelName();
			// always use first label as target
			Jump breakTarget = labels == null ? null : labels.GetFirstLabel();
			if (breakTarget == null && breakLabel == null)
			{
				if (loopAndSwitchSet == null || loopAndSwitchSet.Count == 0)
				{
					if (breakLabel == null)
					{
						ReportError("msg.bad.break", pos, end - pos);
					}
				}
				else
				{
					breakTarget = loopAndSwitchSet[loopAndSwitchSet.Count - 1];
				}
			}
			BreakStatement pn = new BreakStatement(pos, end - pos);
			pn.SetBreakLabel(breakLabel);
			// can be null if it's a bad break in error-recovery mode
			if (breakTarget != null)
			{
				pn.SetBreakTarget(breakTarget);
			}
			pn.SetLineno(lineno);
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private ContinueStatement ContinueStatement()
		{
			if (currentToken != Token.CONTINUE)
			{
				CodeBug();
			}
			ConsumeToken();
			int lineno = ts.lineno;
			int pos = ts.tokenBeg;
			int end = ts.tokenEnd;
			Name label = null;
			if (PeekTokenOrEOL() == Token.NAME)
			{
				label = CreateNameNode();
				end = GetNodeEnd(label);
			}
			// matchJumpLabelName only matches if there is one
			LabeledStatement labels = MatchJumpLabelName();
			Loop target = null;
			if (labels == null && label == null)
			{
				if (loopSet == null || loopSet.Count == 0)
				{
					ReportError("msg.continue.outside");
				}
				else
				{
					target = loopSet[loopSet.Count - 1];
				}
			}
			else
			{
				if (labels == null || !(labels.GetStatement() is Loop))
				{
					ReportError("msg.continue.nonloop", pos, end - pos);
				}
				target = labels == null ? null : (Loop)labels.GetStatement();
			}
			ContinueStatement pn = new ContinueStatement(pos, end - pos);
			if (target != null)
			{
				// can be null in error-recovery mode
				pn.SetTarget(target);
			}
			pn.SetLabel(label);
			pn.SetLineno(lineno);
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private WithStatement WithStatement()
		{
			if (currentToken != Token.WITH)
			{
				CodeBug();
			}
			ConsumeToken();
			Comment withComment = GetAndResetJsDoc();
			int lineno = ts.lineno;
			int pos = ts.tokenBeg;
			int lp = -1;
			int rp = -1;
			if (MustMatchToken(Token.LP, "msg.no.paren.with"))
			{
				lp = ts.tokenBeg;
			}
			AstNode obj = Expr();
			if (MustMatchToken(Token.RP, "msg.no.paren.after.with"))
			{
				rp = ts.tokenBeg;
			}
			AstNode body = Statement();
			WithStatement pn = new WithStatement(pos, GetNodeEnd(body) - pos);
			pn.SetJsDocNode(withComment);
			pn.SetExpression(obj);
			pn.SetStatement(body);
			pn.SetParens(lp, rp);
			pn.SetLineno(lineno);
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode LetStatement()
		{
			if (currentToken != Token.LET)
			{
				CodeBug();
			}
			ConsumeToken();
			int lineno = ts.lineno;
			int pos = ts.tokenBeg;
			AstNode pn;
			if (PeekToken() == Token.LP)
			{
				pn = Let(true, pos);
			}
			else
			{
				pn = Variables(Token.LET, pos, true);
			}
			// else, e.g.: let x=6, y=7;
			pn.SetLineno(lineno);
			return pn;
		}

		/// <summary>Returns whether or not the bits in the mask have changed to all set.</summary>
		/// <remarks>Returns whether or not the bits in the mask have changed to all set.</remarks>
		/// <param name="before">bits before change</param>
		/// <param name="after">bits after change</param>
		/// <param name="mask">mask for bits</param>
		/// <returns>
		/// 
		/// <code>true</code>
		/// if all the bits in the mask are set in "after"
		/// but not in "before"
		/// </returns>
		private static bool NowAllSet(int before, int after, int mask)
		{
			return ((before & mask) != mask) && ((after & mask) == mask);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode ReturnOrYield(int tt, bool exprContext)
		{
			if (!InsideFunction())
			{
				ReportError(tt == Token.RETURN ? "msg.bad.return" : "msg.bad.yield");
			}
			ConsumeToken();
			int lineno = ts.lineno;
			int pos = ts.tokenBeg;
			int end = ts.tokenEnd;
			AstNode e = null;
			switch (PeekTokenOrEOL())
			{
				case Token.SEMI:
				case Token.RC:
				case Token.RB:
				case Token.RP:
				case Token.EOF:
				case Token.EOL:
				case Token.ERROR:
				case Token.YIELD:
				{
					// This is ugly, but we don't want to require a semicolon.
					break;
				}

				default:
				{
					e = Expr();
					end = GetNodeEnd(e);
					break;
				}
			}
			int before = endFlags;
			AstNode ret;
			if (tt == Token.RETURN)
			{
				endFlags |= e == null ? Node.END_RETURNS : Node.END_RETURNS_VALUE;
				ret = new ReturnStatement(pos, end - pos, e);
				// see if we need a strict mode warning
				if (NowAllSet(before, endFlags, Node.END_RETURNS | Node.END_RETURNS_VALUE))
				{
					AddStrictWarning("msg.return.inconsistent", string.Empty, pos, end - pos);
				}
			}
			else
			{
				if (!InsideFunction())
				{
					ReportError("msg.bad.yield");
				}
				endFlags |= Node.END_YIELDS;
				ret = new Yield(pos, end - pos, e);
				SetRequiresActivation();
				SetIsGenerator();
				if (!exprContext)
				{
					ret = new ExpressionStatement(ret);
				}
			}
			// see if we are mixing yields and value returns.
			if (InsideFunction() && NowAllSet(before, endFlags, Node.END_YIELDS | Node.END_RETURNS_VALUE))
			{
				Name name = ((FunctionNode)currentScriptOrFn).GetFunctionName();
				if (name == null || name.Length() == 0)
				{
					AddError("msg.anon.generator.returns", string.Empty);
				}
				else
				{
					AddError("msg.generator.returns", name.GetIdentifier());
				}
			}
			ret.SetLineno(lineno);
			return ret;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode Block()
		{
			if (currentToken != Token.LC)
			{
				CodeBug();
			}
			ConsumeToken();
			int pos = ts.tokenBeg;
			Scope block = new Scope(pos);
			block.SetLineno(ts.lineno);
			PushScope(block);
			try
			{
				Statements(block);
				MustMatchToken(Token.RC, "msg.no.brace.block");
				block.SetLength(ts.tokenEnd - pos);
				return block;
			}
			finally
			{
				PopScope();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode DefaultXmlNamespace()
		{
			if (currentToken != Token.DEFAULT)
			{
				CodeBug();
			}
			ConsumeToken();
			MustHaveXML();
			SetRequiresActivation();
			int lineno = ts.lineno;
			int pos = ts.tokenBeg;
			if (!(MatchToken(Token.NAME) && "xml".Equals(ts.GetString())))
			{
				ReportError("msg.bad.namespace");
			}
			if (!(MatchToken(Token.NAME) && "namespace".Equals(ts.GetString())))
			{
				ReportError("msg.bad.namespace");
			}
			if (!MatchToken(Token.ASSIGN))
			{
				ReportError("msg.bad.namespace");
			}
			AstNode e = Expr();
			UnaryExpression dxmln = new UnaryExpression(pos, GetNodeEnd(e) - pos);
			dxmln.SetOperator(Token.DEFAULTNAMESPACE);
			dxmln.SetOperand(e);
			dxmln.SetLineno(lineno);
			ExpressionStatement es = new ExpressionStatement(dxmln, true);
			return es;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void RecordLabel(Label label, LabeledStatement bundle)
		{
			// current token should be colon that primaryExpr left untouched
			if (PeekToken() != Token.COLON)
			{
				CodeBug();
			}
			ConsumeToken();
			string name = label.GetName();
			if (labelSet == null)
			{
				labelSet = new Dictionary<string, LabeledStatement>();
			}
			else
			{
				LabeledStatement ls = labelSet.Get(name);
				if (ls != null)
				{
					if (compilerEnv.IsIdeMode())
					{
						Label dup = ls.GetLabelByName(name);
						ReportError("msg.dup.label", dup.GetAbsolutePosition(), dup.GetLength());
					}
					ReportError("msg.dup.label", label.GetPosition(), label.GetLength());
				}
			}
			bundle.AddLabel(label);
			labelSet[name] = bundle;
		}

		/// <summary>Found a name in a statement context.</summary>
		/// <remarks>
		/// Found a name in a statement context.  If it's a label, we gather
		/// up any following labels and the next non-label statement into a
		/// <see cref="Rhino.Ast.LabeledStatement">Rhino.Ast.LabeledStatement</see>
		/// "bundle" and return that.  Otherwise we parse
		/// an expression and return it wrapped in an
		/// <see cref="Rhino.Ast.ExpressionStatement">Rhino.Ast.ExpressionStatement</see>
		/// .
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private AstNode NameOrLabel()
		{
			if (currentToken != Token.NAME)
			{
				throw CodeBug();
			}
			int pos = ts.tokenBeg;
			// set check for label and call down to primaryExpr
			currentFlaggedToken |= TI_CHECK_LABEL;
			AstNode expr = Expr();
			if (expr.GetType() != Token.LABEL)
			{
				AstNode n = new ExpressionStatement(expr, !InsideFunction());
				n.lineno = expr.lineno;
				return n;
			}
			LabeledStatement bundle = new LabeledStatement(pos);
			RecordLabel((Label)expr, bundle);
			bundle.SetLineno(ts.lineno);
			// look for more labels
			AstNode stmt = null;
			while (PeekToken() == Token.NAME)
			{
				currentFlaggedToken |= TI_CHECK_LABEL;
				expr = Expr();
				if (expr.GetType() != Token.LABEL)
				{
					stmt = new ExpressionStatement(expr, !InsideFunction());
					AutoInsertSemicolon(stmt);
					break;
				}
				RecordLabel((Label)expr, bundle);
			}
			// no more labels; now parse the labeled statement
			try
			{
				currentLabel = bundle;
				if (stmt == null)
				{
					stmt = StatementHelper();
				}
			}
			finally
			{
				currentLabel = null;
				// remove the labels for this statement from the global set
				foreach (Label lb in bundle.GetLabels())
				{
					labelSet.Remove(lb.GetName());
				}
			}
			// If stmt has parent assigned its position already is relative
			// (See bug #710225)
			bundle.SetLength(stmt.GetParent() == null ? GetNodeEnd(stmt) - pos : GetNodeEnd(stmt));
			bundle.SetStatement(stmt);
			return bundle;
		}

		/// <summary>
		/// Parse a 'var' or 'const' statement, or a 'var' init list in a for
		/// statement.
		/// </summary>
		/// <remarks>
		/// Parse a 'var' or 'const' statement, or a 'var' init list in a for
		/// statement.
		/// </remarks>
		/// <param name="declType">
		/// A token value: either VAR, CONST, or LET depending on
		/// context.
		/// </param>
		/// <param name="pos">
		/// the position where the node should start.  It's sometimes
		/// the var/const/let keyword, and other times the beginning of the first
		/// token in the first variable declaration.
		/// </param>
		/// <returns>the parsed variable list</returns>
		/// <exception cref="System.IO.IOException"></exception>
		private VariableDeclaration Variables(int declType, int pos, bool isStatement)
		{
			int end;
			VariableDeclaration pn = new VariableDeclaration(pos);
			pn.SetType(declType);
			pn.SetLineno(ts.lineno);
			Comment varjsdocNode = GetAndResetJsDoc();
			if (varjsdocNode != null)
			{
				pn.SetJsDocNode(varjsdocNode);
			}
			// Example:
			// var foo = {a: 1, b: 2}, bar = [3, 4];
			// var {b: s2, a: s1} = foo, x = 6, y, [s3, s4] = bar;
			for (; ; )
			{
				AstNode destructuring = null;
				Name name = null;
				int tt = PeekToken();
				int kidPos = ts.tokenBeg;
				end = ts.tokenEnd;
				if (tt == Token.LB || tt == Token.LC)
				{
					// Destructuring assignment, e.g., var [a,b] = ...
					destructuring = DestructuringPrimaryExpr();
					end = GetNodeEnd(destructuring);
					if (!(destructuring is DestructuringForm))
					{
						ReportError("msg.bad.assign.left", kidPos, end - kidPos);
					}
					MarkDestructuring(destructuring);
				}
				else
				{
					// Simple variable name
					MustMatchToken(Token.NAME, "msg.bad.var");
					name = CreateNameNode();
					name.SetLineno(ts.GetLineno());
					if (inUseStrictDirective)
					{
						string id = ts.GetString();
						if ("eval".Equals(id) || "arguments".Equals(ts.GetString()))
						{
							ReportError("msg.bad.id.strict", id);
						}
					}
					DefineSymbol(declType, ts.GetString(), inForInit);
				}
				int lineno = ts.lineno;
				Comment jsdocNode = GetAndResetJsDoc();
				AstNode init = null;
				if (MatchToken(Token.ASSIGN))
				{
					init = AssignExpr();
					end = GetNodeEnd(init);
				}
				VariableInitializer vi = new VariableInitializer(kidPos, end - kidPos);
				if (destructuring != null)
				{
					if (init == null && !inForInit)
					{
						ReportError("msg.destruct.assign.no.init");
					}
					vi.SetTarget(destructuring);
				}
				else
				{
					vi.SetTarget(name);
				}
				vi.SetInitializer(init);
				vi.SetType(declType);
				vi.SetJsDocNode(jsdocNode);
				vi.SetLineno(lineno);
				pn.AddVariable(vi);
				if (!MatchToken(Token.COMMA))
				{
					break;
				}
			}
			pn.SetLength(end - pos);
			pn.SetIsStatement(isStatement);
			return pn;
		}

		// have to pass in 'let' kwd position to compute kid offsets properly
		/// <exception cref="System.IO.IOException"></exception>
		private AstNode Let(bool isStatement, int pos)
		{
			LetNode pn = new LetNode(pos);
			pn.SetLineno(ts.lineno);
			if (MustMatchToken(Token.LP, "msg.no.paren.after.let"))
			{
				pn.SetLp(ts.tokenBeg - pos);
			}
			PushScope(pn);
			try
			{
				VariableDeclaration vars = Variables(Token.LET, ts.tokenBeg, isStatement);
				pn.SetVariables(vars);
				if (MustMatchToken(Token.RP, "msg.no.paren.let"))
				{
					pn.SetRp(ts.tokenBeg - pos);
				}
				if (isStatement && PeekToken() == Token.LC)
				{
					// let statement
					ConsumeToken();
					int beg = ts.tokenBeg;
					// position stmt at LC
					AstNode stmt = Statements();
					MustMatchToken(Token.RC, "msg.no.curly.let");
					stmt.SetLength(ts.tokenEnd - beg);
					pn.SetLength(ts.tokenEnd - pos);
					pn.SetBody(stmt);
					pn.SetType(Token.LET);
				}
				else
				{
					// let expression
					AstNode expr = Expr();
					pn.SetLength(GetNodeEnd(expr) - pos);
					pn.SetBody(expr);
					if (isStatement)
					{
						// let expression in statement context
						ExpressionStatement es = new ExpressionStatement(pn, !InsideFunction());
						es.SetLineno(pn.GetLineno());
						return es;
					}
				}
			}
			finally
			{
				PopScope();
			}
			return pn;
		}

		internal virtual void DefineSymbol(int declType, string name)
		{
			DefineSymbol(declType, name, false);
		}

		internal virtual void DefineSymbol(int declType, string name, bool ignoreNotInBlock)
		{
			if (name == null)
			{
				if (compilerEnv.IsIdeMode())
				{
					// be robust in IDE-mode
					return;
				}
				else
				{
					CodeBug();
				}
			}
			Scope definingScope = currentScope.GetDefiningScope(name);
			Symbol symbol = definingScope != null ? definingScope.GetSymbol(name) : null;
			int symDeclType = symbol != null ? symbol.GetDeclType() : -1;
			if (symbol != null && (symDeclType == Token.CONST || declType == Token.CONST || (definingScope == currentScope && symDeclType == Token.LET)))
			{
				AddError(symDeclType == Token.CONST ? "msg.const.redecl" : symDeclType == Token.LET ? "msg.let.redecl" : symDeclType == Token.VAR ? "msg.var.redecl" : symDeclType == Token.FUNCTION ? "msg.fn.redecl" : "msg.parm.redecl", name);
				return;
			}
			switch (declType)
			{
				case Token.LET:
				{
					if (!ignoreNotInBlock && ((currentScope.GetType() == Token.IF) || currentScope is Loop))
					{
						AddError("msg.let.decl.not.in.block");
						return;
					}
					currentScope.PutSymbol(new Symbol(declType, name));
					return;
				}

				case Token.VAR:
				case Token.CONST:
				case Token.FUNCTION:
				{
					if (symbol != null)
					{
						if (symDeclType == Token.VAR)
						{
							AddStrictWarning("msg.var.redecl", name);
						}
						else
						{
							if (symDeclType == Token.LP)
							{
								AddStrictWarning("msg.var.hides.arg", name);
							}
						}
					}
					else
					{
						currentScriptOrFn.PutSymbol(new Symbol(declType, name));
					}
					return;
				}

				case Token.LP:
				{
					if (symbol != null)
					{
						// must be duplicate parameter. Second parameter hides the
						// first, so go ahead and add the second parameter
						AddWarning("msg.dup.parms", name);
					}
					currentScriptOrFn.PutSymbol(new Symbol(declType, name));
					return;
				}

				default:
				{
					throw CodeBug();
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode Expr()
		{
			AstNode pn = AssignExpr();
			int pos = pn.GetPosition();
			while (MatchToken(Token.COMMA))
			{
				int opPos = ts.tokenBeg;
				if (compilerEnv.IsStrictMode() && !pn.HasSideEffects())
				{
					AddStrictWarning("msg.no.side.effects", string.Empty, pos, NodeEnd(pn) - pos);
				}
				if (PeekToken() == Token.YIELD)
				{
					ReportError("msg.yield.parenthesized");
				}
				pn = new InfixExpression(Token.COMMA, pn, AssignExpr(), opPos);
			}
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode AssignExpr()
		{
			int tt = PeekToken();
			if (tt == Token.YIELD)
			{
				return ReturnOrYield(tt, true);
			}
			AstNode pn = CondExpr();
			tt = PeekToken();
			if (Token.FIRST_ASSIGN <= tt && tt <= Token.LAST_ASSIGN)
			{
				ConsumeToken();
				// Pull out JSDoc info and reset it before recursing.
				Comment jsdocNode = GetAndResetJsDoc();
				MarkDestructuring(pn);
				int opPos = ts.tokenBeg;
				pn = new Assignment(tt, pn, AssignExpr(), opPos);
				if (jsdocNode != null)
				{
					pn.SetJsDocNode(jsdocNode);
				}
			}
			else
			{
				if (tt == Token.SEMI)
				{
					// This may be dead code added intentionally, for JSDoc purposes.
					// For example: /** @type Number */ C.prototype.x;
					if (currentJsDocComment != null)
					{
						pn.SetJsDocNode(GetAndResetJsDoc());
					}
				}
			}
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode CondExpr()
		{
			AstNode pn = OrExpr();
			if (MatchToken(Token.HOOK))
			{
				int line = ts.lineno;
				int qmarkPos = ts.tokenBeg;
				int colonPos = -1;
				bool wasInForInit = inForInit;
				inForInit = false;
				AstNode ifTrue;
				try
				{
					ifTrue = AssignExpr();
				}
				finally
				{
					inForInit = wasInForInit;
				}
				if (MustMatchToken(Token.COLON, "msg.no.colon.cond"))
				{
					colonPos = ts.tokenBeg;
				}
				AstNode ifFalse = AssignExpr();
				int beg = pn.GetPosition();
				int len = GetNodeEnd(ifFalse) - beg;
				ConditionalExpression ce = new ConditionalExpression(beg, len);
				ce.SetLineno(line);
				ce.SetTestExpression(pn);
				ce.SetTrueExpression(ifTrue);
				ce.SetFalseExpression(ifFalse);
				ce.SetQuestionMarkPosition(qmarkPos - beg);
				ce.SetColonPosition(colonPos - beg);
				pn = ce;
			}
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode OrExpr()
		{
			AstNode pn = AndExpr();
			if (MatchToken(Token.OR))
			{
				int opPos = ts.tokenBeg;
				pn = new InfixExpression(Token.OR, pn, OrExpr(), opPos);
			}
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode AndExpr()
		{
			AstNode pn = BitOrExpr();
			if (MatchToken(Token.AND))
			{
				int opPos = ts.tokenBeg;
				pn = new InfixExpression(Token.AND, pn, AndExpr(), opPos);
			}
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode BitOrExpr()
		{
			AstNode pn = BitXorExpr();
			while (MatchToken(Token.BITOR))
			{
				int opPos = ts.tokenBeg;
				pn = new InfixExpression(Token.BITOR, pn, BitXorExpr(), opPos);
			}
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode BitXorExpr()
		{
			AstNode pn = BitAndExpr();
			while (MatchToken(Token.BITXOR))
			{
				int opPos = ts.tokenBeg;
				pn = new InfixExpression(Token.BITXOR, pn, BitAndExpr(), opPos);
			}
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode BitAndExpr()
		{
			AstNode pn = EqExpr();
			while (MatchToken(Token.BITAND))
			{
				int opPos = ts.tokenBeg;
				pn = new InfixExpression(Token.BITAND, pn, EqExpr(), opPos);
			}
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode EqExpr()
		{
			AstNode pn = RelExpr();
			for (; ; )
			{
				int tt = PeekToken();
				int opPos = ts.tokenBeg;
				switch (tt)
				{
					case Token.EQ:
					case Token.NE:
					case Token.SHEQ:
					case Token.SHNE:
					{
						ConsumeToken();
						int parseToken = tt;
						if (compilerEnv.GetLanguageVersion() == LanguageVersion.VERSION_1_2)
						{
							// JavaScript 1.2 uses shallow equality for == and != .
							if (tt == Token.EQ)
							{
								parseToken = Token.SHEQ;
							}
							else
							{
								if (tt == Token.NE)
								{
									parseToken = Token.SHNE;
								}
							}
						}
						pn = new InfixExpression(parseToken, pn, RelExpr(), opPos);
						continue;
					}
				}
				break;
			}
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode RelExpr()
		{
			AstNode pn = ShiftExpr();
			for (; ; )
			{
				int tt = PeekToken();
				int opPos = ts.tokenBeg;
				switch (tt)
				{
					case Token.IN:
					{
						if (inForInit)
						{
							break;
						}
						goto case Token.INSTANCEOF;
					}

					case Token.INSTANCEOF:
					case Token.LE:
					case Token.LT:
					case Token.GE:
					case Token.GT:
					{
						// fall through
						ConsumeToken();
						pn = new InfixExpression(tt, pn, ShiftExpr(), opPos);
						continue;
					}
				}
				break;
			}
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode ShiftExpr()
		{
			AstNode pn = AddExpr();
			for (; ; )
			{
				int tt = PeekToken();
				int opPos = ts.tokenBeg;
				switch (tt)
				{
					case Token.LSH:
					case Token.URSH:
					case Token.RSH:
					{
						ConsumeToken();
						pn = new InfixExpression(tt, pn, AddExpr(), opPos);
						continue;
					}
				}
				break;
			}
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode AddExpr()
		{
			AstNode pn = MulExpr();
			for (; ; )
			{
				int tt = PeekToken();
				int opPos = ts.tokenBeg;
				if (tt == Token.ADD || tt == Token.SUB)
				{
					ConsumeToken();
					pn = new InfixExpression(tt, pn, MulExpr(), opPos);
					continue;
				}
				break;
			}
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode MulExpr()
		{
			AstNode pn = UnaryExpr();
			for (; ; )
			{
				int tt = PeekToken();
				int opPos = ts.tokenBeg;
				switch (tt)
				{
					case Token.MUL:
					case Token.DIV:
					case Token.MOD:
					{
						ConsumeToken();
						pn = new InfixExpression(tt, pn, UnaryExpr(), opPos);
						continue;
					}
				}
				break;
			}
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode UnaryExpr()
		{
			AstNode node;
			int tt = PeekToken();
			int line = ts.lineno;
			switch (tt)
			{
				case Token.VOID:
				case Token.NOT:
				case Token.BITNOT:
				case Token.TYPEOF:
				{
					ConsumeToken();
					node = new UnaryExpression(tt, ts.tokenBeg, UnaryExpr());
					node.SetLineno(line);
					return node;
				}

				case Token.ADD:
				{
					ConsumeToken();
					// Convert to special POS token in parse tree
					node = new UnaryExpression(Token.POS, ts.tokenBeg, UnaryExpr());
					node.SetLineno(line);
					return node;
				}

				case Token.SUB:
				{
					ConsumeToken();
					// Convert to special NEG token in parse tree
					node = new UnaryExpression(Token.NEG, ts.tokenBeg, UnaryExpr());
					node.SetLineno(line);
					return node;
				}

				case Token.INC:
				case Token.DEC:
				{
					ConsumeToken();
					UnaryExpression expr = new UnaryExpression(tt, ts.tokenBeg, MemberExpr(true));
					expr.SetLineno(line);
					CheckBadIncDec(expr);
					return expr;
				}

				case Token.DELPROP:
				{
					ConsumeToken();
					node = new UnaryExpression(tt, ts.tokenBeg, UnaryExpr());
					node.SetLineno(line);
					return node;
				}

				case Token.ERROR:
				{
					ConsumeToken();
					return MakeErrorNode();
				}

				case Token.LT:
				{
					// XML stream encountered in expression.
					if (compilerEnv.IsXmlAvailable())
					{
						ConsumeToken();
						return MemberExprTail(true, XmlInitializer());
					}
					goto default;
				}

				default:
				{
					// Fall thru to the default handling of RELOP
					AstNode pn = MemberExpr(true);
					// Don't look across a newline boundary for a postfix incop.
					tt = PeekTokenOrEOL();
					if (!(tt == Token.INC || tt == Token.DEC))
					{
						return pn;
					}
					ConsumeToken();
					UnaryExpression uexpr = new UnaryExpression(tt, ts.tokenBeg, pn, true);
					uexpr.SetLineno(line);
					CheckBadIncDec(uexpr);
					return uexpr;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode XmlInitializer()
		{
			if (currentToken != Token.LT)
			{
				CodeBug();
			}
			int pos = ts.tokenBeg;
			int tt = ts.GetFirstXMLToken();
			if (tt != Token.XML && tt != Token.XMLEND)
			{
				ReportError("msg.syntax");
				return MakeErrorNode();
			}
			XmlLiteral pn = new XmlLiteral(pos);
			pn.SetLineno(ts.lineno);
			for (; ; tt = ts.GetNextXMLToken())
			{
				switch (tt)
				{
					case Token.XML:
					{
						pn.AddFragment(new XmlString(ts.tokenBeg, ts.GetString()));
						MustMatchToken(Token.LC, "msg.syntax");
						int beg = ts.tokenBeg;
						AstNode expr = (PeekToken() == Token.RC) ? new EmptyExpression(beg, ts.tokenEnd - beg) : Expr();
						MustMatchToken(Token.RC, "msg.syntax");
						XmlExpression xexpr = new XmlExpression(beg, expr);
						xexpr.SetIsXmlAttribute(ts.IsXMLAttribute());
						xexpr.SetLength(ts.tokenEnd - beg);
						pn.AddFragment(xexpr);
						break;
					}

					case Token.XMLEND:
					{
						pn.AddFragment(new XmlString(ts.tokenBeg, ts.GetString()));
						return pn;
					}

					default:
					{
						ReportError("msg.syntax");
						return MakeErrorNode();
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private IList<AstNode> ArgumentList()
		{
			if (MatchToken(Token.RP))
			{
				return null;
			}
			IList<AstNode> result = new List<AstNode>();
			bool wasInForInit = inForInit;
			inForInit = false;
			try
			{
				do
				{
					if (PeekToken() == Token.YIELD)
					{
						ReportError("msg.yield.parenthesized");
					}
					AstNode en = AssignExpr();
					if (PeekToken() == Token.FOR)
					{
						try
						{
							result.Add(GeneratorExpression(en, 0, true));
						}
						catch (IOException)
						{
						}
					}
					else
					{
						// #TODO
						result.Add(en);
					}
				}
				while (MatchToken(Token.COMMA));
			}
			finally
			{
				inForInit = wasInForInit;
			}
			MustMatchToken(Token.RP, "msg.no.paren.arg");
			return result;
		}

		/// <summary>
		/// Parse a new-expression, or if next token isn't
		/// <see cref="Token.NEW">Token.NEW</see>
		/// ,
		/// a primary expression.
		/// </summary>
		/// <param name="allowCallSyntax">
		/// passed down to
		/// <see cref="MemberExprTail(bool, Rhino.Ast.AstNode)">MemberExprTail(bool, Rhino.Ast.AstNode)</see>
		/// </param>
		/// <exception cref="System.IO.IOException"></exception>
		private AstNode MemberExpr(bool allowCallSyntax)
		{
			int tt = PeekToken();
			int lineno = ts.lineno;
			AstNode pn;
			if (tt != Token.NEW)
			{
				pn = PrimaryExpr();
			}
			else
			{
				ConsumeToken();
				int pos = ts.tokenBeg;
				NewExpression nx = new NewExpression(pos);
				AstNode target = MemberExpr(false);
				int end = GetNodeEnd(target);
				nx.SetTarget(target);
				int lp = -1;
				if (MatchToken(Token.LP))
				{
					lp = ts.tokenBeg;
					IList<AstNode> args = ArgumentList();
					if (args != null && args.Count > ARGC_LIMIT)
					{
						ReportError("msg.too.many.constructor.args");
					}
					int rp = ts.tokenBeg;
					end = ts.tokenEnd;
					if (args != null)
					{
						nx.SetArguments(args);
					}
					nx.SetParens(lp - pos, rp - pos);
				}
				// Experimental syntax: allow an object literal to follow a new
				// expression, which will mean a kind of anonymous class built with
				// the JavaAdapter.  the object literal will be passed as an
				// additional argument to the constructor.
				if (MatchToken(Token.LC))
				{
					ObjectLiteral initializer = ObjectLiteral();
					end = GetNodeEnd(initializer);
					nx.SetInitializer(initializer);
				}
				nx.SetLength(end - pos);
				pn = nx;
			}
			pn.SetLineno(lineno);
			AstNode tail = MemberExprTail(allowCallSyntax, pn);
			return tail;
		}

		/// <summary>
		/// Parse any number of "(expr)", "[expr]" ".expr", "..expr",
		/// or ".(expr)" constructs trailing the passed expression.
		/// </summary>
		/// <remarks>
		/// Parse any number of "(expr)", "[expr]" ".expr", "..expr",
		/// or ".(expr)" constructs trailing the passed expression.
		/// </remarks>
		/// <param name="pn">the non-null parent node</param>
		/// <returns>
		/// the outermost (lexically last occurring) expression,
		/// which will have the passed parent node as a descendant
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		private AstNode MemberExprTail(bool allowCallSyntax, AstNode pn)
		{
			// we no longer return null for errors, so this won't be null
			if (pn == null)
			{
				CodeBug();
			}
			int pos = pn.GetPosition();
			int lineno;
			for (; ; )
			{
				int tt = PeekToken();
				switch (tt)
				{
					case Token.DOT:
					case Token.DOTDOT:
					{
						lineno = ts.lineno;
						pn = PropertyAccess(tt, pn);
						pn.SetLineno(lineno);
						break;
					}

					case Token.DOTQUERY:
					{
						ConsumeToken();
						int opPos = ts.tokenBeg;
						int rp = -1;
						lineno = ts.lineno;
						MustHaveXML();
						SetRequiresActivation();
						AstNode filter = Expr();
						int end = GetNodeEnd(filter);
						if (MustMatchToken(Token.RP, "msg.no.paren"))
						{
							rp = ts.tokenBeg;
							end = ts.tokenEnd;
						}
						XmlDotQuery q = new XmlDotQuery(pos, end - pos);
						q.SetLeft(pn);
						q.SetRight(filter);
						q.SetOperatorPosition(opPos);
						q.SetRp(rp - pos);
						q.SetLineno(lineno);
						pn = q;
						break;
					}

					case Token.LB:
					{
						ConsumeToken();
						int lb = ts.tokenBeg;
						int rb = -1;
						lineno = ts.lineno;
						AstNode expr = Expr();
						int end = GetNodeEnd(expr);
						if (MustMatchToken(Token.RB, "msg.no.bracket.index"))
						{
							rb = ts.tokenBeg;
							end = ts.tokenEnd;
						}
						ElementGet g = new ElementGet(pos, end - pos);
						g.SetTarget(pn);
						g.SetElement(expr);
						g.SetParens(lb, rb);
						g.SetLineno(lineno);
						pn = g;
						break;
					}

					case Token.LP:
					{
						if (!allowCallSyntax)
						{
							goto tailLoop_break;
						}
						lineno = ts.lineno;
						ConsumeToken();
						CheckCallRequiresActivation(pn);
						FunctionCall f = new FunctionCall(pos);
						f.SetTarget(pn);
						// Assign the line number for the function call to where
						// the paren appeared, not where the name expression started.
						f.SetLineno(lineno);
						f.SetLp(ts.tokenBeg - pos);
						IList<AstNode> args = ArgumentList();
						if (args != null && args.Count > ARGC_LIMIT)
						{
							ReportError("msg.too.many.function.args");
						}
						f.SetArguments(args);
						f.SetRp(ts.tokenBeg - pos);
						f.SetLength(ts.tokenEnd - pos);
						pn = f;
						break;
					}

					default:
					{
						goto tailLoop_break;
					}
				}
tailLoop_continue: ;
			}
tailLoop_break: ;
			return pn;
		}

		/// <summary>Handles any construct following a "." or ".." operator.</summary>
		/// <remarks>Handles any construct following a "." or ".." operator.</remarks>
		/// <param name="pn">the left-hand side (target) of the operator.  Never null.</param>
		/// <returns>a PropertyGet, XmlMemberGet, or ErrorNode</returns>
		/// <exception cref="System.IO.IOException"></exception>
		private AstNode PropertyAccess(int tt, AstNode pn)
		{
			if (pn == null)
			{
				CodeBug();
			}
			int memberTypeFlags = 0;
			int lineno = ts.lineno;
			int dotPos = ts.tokenBeg;
			ConsumeToken();
			if (tt == Token.DOTDOT)
			{
				MustHaveXML();
				memberTypeFlags = Node.DESCENDANTS_FLAG;
			}
			if (!compilerEnv.IsXmlAvailable())
			{
				int maybeName = NextToken();
				if (maybeName != Token.NAME && !(compilerEnv.IsReservedKeywordAsIdentifier() && TokenStream.IsKeyword(ts.GetString())))
				{
					ReportError("msg.no.name.after.dot");
				}
				Name name = CreateNameNode(true, Token.GETPROP);
				PropertyGet pg = new PropertyGet(pn, name, dotPos);
				pg.SetLineno(lineno);
				return pg;
			}
			AstNode @ref = null;
			// right side of . or .. operator
			int token = NextToken();
			switch (token)
			{
				case Token.THROW:
				{
					// needed for generator.throw();
					SaveNameTokenData(ts.tokenBeg, "throw", ts.lineno);
					@ref = PropertyName(-1, "throw", memberTypeFlags);
					break;
				}

				case Token.NAME:
				{
					// handles: name, ns::name, ns::*, ns::[expr]
					@ref = PropertyName(-1, ts.GetString(), memberTypeFlags);
					break;
				}

				case Token.MUL:
				{
					// handles: *, *::name, *::*, *::[expr]
					SaveNameTokenData(ts.tokenBeg, "*", ts.lineno);
					@ref = PropertyName(-1, "*", memberTypeFlags);
					break;
				}

				case Token.XMLATTR:
				{
					// handles: '@attr', '@ns::attr', '@ns::*', '@ns::*',
					//          '@::attr', '@::*', '@*', '@*::attr', '@*::*'
					@ref = AttributeAccess();
					break;
				}

				default:
				{
					if (compilerEnv.IsReservedKeywordAsIdentifier())
					{
						// allow keywords as property names, e.g. ({if: 1})
						string name = Token.KeywordToName(token);
						if (name != null)
						{
							SaveNameTokenData(ts.tokenBeg, name, ts.lineno);
							@ref = PropertyName(-1, name, memberTypeFlags);
							break;
						}
					}
					ReportError("msg.no.name.after.dot");
					return MakeErrorNode();
				}
			}
			bool xml = @ref is XmlRef;
			InfixExpression result = xml ? new XmlMemberGet() : (InfixExpression) new PropertyGet();
			if (xml && tt == Token.DOT)
			{
				result.SetType(Token.DOT);
			}
			int pos = pn.GetPosition();
			result.SetPosition(pos);
			result.SetLength(GetNodeEnd(@ref) - pos);
			result.SetOperatorPosition(dotPos - pos);
			result.SetLineno(pn.GetLineno());
			result.SetLeft(pn);
			// do this after setting position
			result.SetRight(@ref);
			return result;
		}

		/// <summary>
		/// Xml attribute expression:<p>
		/// <code>@attr</code>
		/// ,
		/// <code>@ns::attr</code>
		/// ,
		/// <code>@ns::*</code>
		/// ,
		/// <code>@ns::*</code>
		/// ,
		/// <code>@*</code>
		/// ,
		/// <code>@*::attr</code>
		/// ,
		/// <code>@*::*</code>
		/// ,
		/// <code>@ns::[expr]</code>
		/// ,
		/// <code>@*::[expr]</code>
		/// ,
		/// <code>@[expr]</code>
		/// <p>
		/// Called if we peeked an '@' token.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		private AstNode AttributeAccess()
		{
			int tt = NextToken();
			int atPos = ts.tokenBeg;
			switch (tt)
			{
				case Token.NAME:
				{
					// handles: @name, @ns::name, @ns::*, @ns::[expr]
					return PropertyName(atPos, ts.GetString(), 0);
				}

				case Token.MUL:
				{
					// handles: @*, @*::name, @*::*, @*::[expr]
					SaveNameTokenData(ts.tokenBeg, "*", ts.lineno);
					return PropertyName(atPos, "*", 0);
				}

				case Token.LB:
				{
					// handles @[expr]
					return XmlElemRef(atPos, null, -1);
				}

				default:
				{
					ReportError("msg.no.name.after.xmlAttr");
					return MakeErrorNode();
				}
			}
		}

		/// <summary>Check if :: follows name in which case it becomes a qualified name.</summary>
		/// <remarks>Check if :: follows name in which case it becomes a qualified name.</remarks>
		/// <param name="atPos">a natural number if we just read an '@' token, else -1</param>
		/// <param name="s">
		/// the name or string that was matched (an identifier, "throw" or
		/// "*").
		/// </param>
		/// <param name="memberTypeFlags">flags tracking whether we're a '.' or '..' child</param>
		/// <returns>
		/// an XmlRef node if it's an attribute access, a child of a
		/// '..' operator, or the name is followed by ::.  For a plain name,
		/// returns a Name node.  Returns an ErrorNode for malformed XML
		/// expressions.  (For now - might change to return a partial XmlRef.)
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		private AstNode PropertyName(int atPos, string s, int memberTypeFlags)
		{
			int pos = atPos != -1 ? atPos : ts.tokenBeg;
			int lineno = ts.lineno;
			int colonPos = -1;
			Name name = CreateNameNode(true, currentToken);
			Name ns = null;
			if (MatchToken(Token.COLONCOLON))
			{
				ns = name;
				colonPos = ts.tokenBeg;
				switch (NextToken())
				{
					case Token.NAME:
					{
						// handles name::name
						name = CreateNameNode();
						break;
					}

					case Token.MUL:
					{
						// handles name::*
						SaveNameTokenData(ts.tokenBeg, "*", ts.lineno);
						name = CreateNameNode(false, -1);
						break;
					}

					case Token.LB:
					{
						// handles name::[expr] or *::[expr]
						return XmlElemRef(atPos, ns, colonPos);
					}

					default:
					{
						ReportError("msg.no.name.after.coloncolon");
						return MakeErrorNode();
					}
				}
			}
			if (ns == null && memberTypeFlags == 0 && atPos == -1)
			{
				return name;
			}
			XmlPropRef @ref = new XmlPropRef(pos, GetNodeEnd(name) - pos);
			@ref.SetAtPos(atPos);
			@ref.SetNamespace(ns);
			@ref.SetColonPos(colonPos);
			@ref.SetPropName(name);
			@ref.SetLineno(lineno);
			return @ref;
		}

		/// <summary>Parse the [expr] portion of an xml element reference, e.g.</summary>
		/// <remarks>
		/// Parse the [expr] portion of an xml element reference, e.g.
		/// @*::[expr], or ns::[expr].
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private XmlElemRef XmlElemRef(int atPos, Name @namespace, int colonPos)
		{
			int lb = ts.tokenBeg;
			int rb = -1;
			int pos = atPos != -1 ? atPos : lb;
			AstNode expr = Expr();
			int end = GetNodeEnd(expr);
			if (MustMatchToken(Token.RB, "msg.no.bracket.index"))
			{
				rb = ts.tokenBeg;
				end = ts.tokenEnd;
			}
			XmlElemRef @ref = new XmlElemRef(pos, end - pos);
			@ref.SetNamespace(@namespace);
			@ref.SetColonPos(colonPos);
			@ref.SetAtPos(atPos);
			@ref.SetExpression(expr);
			@ref.SetBrackets(lb, rb);
			return @ref;
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Rhino.Parser.ParserException"></exception>
		private AstNode DestructuringPrimaryExpr()
		{
			try
			{
				inDestructuringAssignment = true;
				return PrimaryExpr();
			}
			finally
			{
				inDestructuringAssignment = false;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode PrimaryExpr()
		{
			int ttFlagged = NextFlaggedToken();
			int tt = ttFlagged & CLEAR_TI_MASK;
			switch (tt)
			{
				case Token.FUNCTION:
				{
					return Function(FunctionNode.FUNCTION_EXPRESSION);
				}

				case Token.LB:
				{
					return ArrayLiteral();
				}

				case Token.LC:
				{
					return ObjectLiteral();
				}

				case Token.LET:
				{
					return Let(false, ts.tokenBeg);
				}

				case Token.LP:
				{
					return ParenExpr();
				}

				case Token.XMLATTR:
				{
					MustHaveXML();
					return AttributeAccess();
				}

				case Token.NAME:
				{
					return Name(ttFlagged, tt);
				}

				case Token.NUMBER:
				{
					string s = ts.GetString();
					if (this.inUseStrictDirective && ts.IsNumberOctal())
					{
						ReportError("msg.no.octal.strict");
					}
					return new NumberLiteral(ts.tokenBeg, s, ts.GetNumber());
				}

				case Token.STRING:
				{
					return CreateStringLiteral();
				}

				case Token.DIV:
				case Token.ASSIGN_DIV:
				{
					// Got / or /= which in this context means a regexp
					ts.ReadRegExp(tt);
					int pos = ts.tokenBeg;
					int end = ts.tokenEnd;
					RegExpLiteral re = new RegExpLiteral(pos, end - pos);
					re.SetValue(ts.GetString());
					re.SetFlags(ts.ReadAndClearRegExpFlags());
					return re;
				}

				case Token.NULL:
				case Token.THIS:
				case Token.FALSE:
				case Token.TRUE:
				{
					int pos = ts.tokenBeg;
					int end = ts.tokenEnd;
					return new KeywordLiteral(pos, end - pos, tt);
				}

				case Token.RESERVED:
				{
					ReportError("msg.reserved.id");
					break;
				}

				case Token.ERROR:
				{
					// the scanner or one of its subroutines reported the error.
					break;
				}

				case Token.EOF:
				{
					ReportError("msg.unexpected.eof");
					break;
				}

				default:
				{
					ReportError("msg.syntax");
					break;
				}
			}
			// should only be reachable in IDE/error-recovery mode
			return MakeErrorNode();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode ParenExpr()
		{
			bool wasInForInit = inForInit;
			inForInit = false;
			try
			{
				Comment jsdocNode = GetAndResetJsDoc();
				int lineno = ts.lineno;
				int begin = ts.tokenBeg;
				AstNode e = Expr();
				if (PeekToken() == Token.FOR)
				{
					return GeneratorExpression(e, begin);
				}
				ParenthesizedExpression pn = new ParenthesizedExpression(e);
				if (jsdocNode == null)
				{
					jsdocNode = GetAndResetJsDoc();
				}
				if (jsdocNode != null)
				{
					pn.SetJsDocNode(jsdocNode);
				}
				MustMatchToken(Token.RP, "msg.no.paren");
				pn.SetLength(ts.tokenEnd - pn.GetPosition());
				pn.SetLineno(lineno);
				return pn;
			}
			finally
			{
				inForInit = wasInForInit;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode Name(int ttFlagged, int tt)
		{
			string nameString = ts.GetString();
			int namePos = ts.tokenBeg;
			int nameLineno = ts.lineno;
			if (0 != (ttFlagged & TI_CHECK_LABEL) && PeekToken() == Token.COLON)
			{
				// Do not consume colon.  It is used as an unwind indicator
				// to return to statementHelper.
				Label label = new Label(namePos, ts.tokenEnd - namePos);
				label.SetName(nameString);
				label.SetLineno(ts.lineno);
				return label;
			}
			// Not a label.  Unfortunately peeking the next token to check for
			// a colon has biffed ts.tokenBeg, ts.tokenEnd.  We store the name's
			// bounds in instance vars and createNameNode uses them.
			SaveNameTokenData(namePos, nameString, nameLineno);
			if (compilerEnv.IsXmlAvailable())
			{
				return PropertyName(-1, nameString, 0);
			}
			else
			{
				return CreateNameNode(true, Token.NAME);
			}
		}

		/// <summary>
		/// May return an
		/// <see cref="Rhino.Ast.ArrayLiteral">Rhino.Ast.ArrayLiteral</see>
		/// or
		/// <see cref="Rhino.Ast.ArrayComprehension">Rhino.Ast.ArrayComprehension</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		private AstNode ArrayLiteral()
		{
			if (currentToken != Token.LB)
			{
				CodeBug();
			}
			int pos = ts.tokenBeg;
			int end = ts.tokenEnd;
			IList<AstNode> elements = new List<AstNode>();
			ArrayLiteral pn = new ArrayLiteral(pos);
			bool after_lb_or_comma = true;
			int afterComma = -1;
			int skipCount = 0;
			for (; ; )
			{
				int tt = PeekToken();
				if (tt == Token.COMMA)
				{
					ConsumeToken();
					afterComma = ts.tokenEnd;
					if (!after_lb_or_comma)
					{
						after_lb_or_comma = true;
					}
					else
					{
						elements.Add(new EmptyExpression(ts.tokenBeg, 1));
						skipCount++;
					}
				}
				else
				{
					if (tt == Token.RB)
					{
						ConsumeToken();
						// for ([a,] in obj) is legal, but for ([a] in obj) is
						// not since we have both key and value supplied. The
						// trick is that [a,] and [a] are equivalent in other
						// array literal contexts. So we calculate a special
						// length value just for destructuring assignment.
						end = ts.tokenEnd;
						pn.SetDestructuringLength(elements.Count + (after_lb_or_comma ? 1 : 0));
						pn.SetSkipCount(skipCount);
						if (afterComma != -1)
						{
							WarnTrailingComma(pos, elements, afterComma);
						}
						break;
					}
					else
					{
						if (tt == Token.FOR && !after_lb_or_comma && elements.Count == 1)
						{
							return ArrayComprehension(elements[0], pos);
						}
						else
						{
							if (tt == Token.EOF)
							{
								ReportError("msg.no.bracket.arg");
								break;
							}
							else
							{
								if (!after_lb_or_comma)
								{
									ReportError("msg.no.bracket.arg");
								}
								elements.Add(AssignExpr());
								after_lb_or_comma = false;
								afterComma = -1;
							}
						}
					}
				}
			}
			foreach (AstNode e in elements)
			{
				pn.AddElement(e);
			}
			pn.SetLength(end - pos);
			return pn;
		}

		/// <summary>Parse a JavaScript 1.7 Array comprehension.</summary>
		/// <remarks>Parse a JavaScript 1.7 Array comprehension.</remarks>
		/// <param name="result">the first expression after the opening left-bracket</param>
		/// <param name="pos">start of LB token that begins the array comprehension</param>
		/// <returns>the array comprehension or an error node</returns>
		/// <exception cref="System.IO.IOException"></exception>
		private AstNode ArrayComprehension(AstNode result, int pos)
		{
			IList<ArrayComprehensionLoop> loops = new List<ArrayComprehensionLoop>();
			while (PeekToken() == Token.FOR)
			{
				loops.Add(ArrayComprehensionLoop());
			}
			int ifPos = -1;
			Parser.ConditionData data = null;
			if (PeekToken() == Token.IF)
			{
				ConsumeToken();
				ifPos = ts.tokenBeg - pos;
				data = Condition();
			}
			MustMatchToken(Token.RB, "msg.no.bracket.arg");
			ArrayComprehension pn = new ArrayComprehension(pos, ts.tokenEnd - pos);
			pn.SetResult(result);
			pn.SetLoops(loops);
			if (data != null)
			{
				pn.SetIfPosition(ifPos);
				pn.SetFilter(data.condition);
				pn.SetFilterLp(data.lp - pos);
				pn.SetFilterRp(data.rp - pos);
			}
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private ArrayComprehensionLoop ArrayComprehensionLoop()
		{
			if (NextToken() != Token.FOR)
			{
				CodeBug();
			}
			int pos = ts.tokenBeg;
			int eachPos = -1;
			int lp = -1;
			int rp = -1;
			int inPos = -1;
			ArrayComprehensionLoop pn = new ArrayComprehensionLoop(pos);
			PushScope(pn);
			try
			{
				if (MatchToken(Token.NAME))
				{
					if (ts.GetString().Equals("each"))
					{
						eachPos = ts.tokenBeg - pos;
					}
					else
					{
						ReportError("msg.no.paren.for");
					}
				}
				if (MustMatchToken(Token.LP, "msg.no.paren.for"))
				{
					lp = ts.tokenBeg - pos;
				}
				AstNode iter = null;
				switch (PeekToken())
				{
					case Token.LB:
					case Token.LC:
					{
						// handle destructuring assignment
						iter = DestructuringPrimaryExpr();
						MarkDestructuring(iter);
						break;
					}

					case Token.NAME:
					{
						ConsumeToken();
						iter = CreateNameNode();
						break;
					}

					default:
					{
						ReportError("msg.bad.var");
						break;
					}
				}
				// Define as a let since we want the scope of the variable to
				// be restricted to the array comprehension
				if (iter.GetType() == Token.NAME)
				{
					DefineSymbol(Token.LET, ts.GetString(), true);
				}
				if (MustMatchToken(Token.IN, "msg.in.after.for.name"))
				{
					inPos = ts.tokenBeg - pos;
				}
				AstNode obj = Expr();
				if (MustMatchToken(Token.RP, "msg.no.paren.for.ctrl"))
				{
					rp = ts.tokenBeg - pos;
				}
				pn.SetLength(ts.tokenEnd - pos);
				pn.SetIterator(iter);
				pn.SetIteratedObject(obj);
				pn.SetInPosition(inPos);
				pn.SetEachPosition(eachPos);
				pn.SetIsForEach(eachPos != -1);
				pn.SetParens(lp, rp);
				return pn;
			}
			finally
			{
				PopScope();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode GeneratorExpression(AstNode result, int pos)
		{
			return GeneratorExpression(result, pos, false);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode GeneratorExpression(AstNode result, int pos, bool inFunctionParams)
		{
			IList<GeneratorExpressionLoop> loops = new List<GeneratorExpressionLoop>();
			while (PeekToken() == Token.FOR)
			{
				loops.Add(GeneratorExpressionLoop());
			}
			int ifPos = -1;
			Parser.ConditionData data = null;
			if (PeekToken() == Token.IF)
			{
				ConsumeToken();
				ifPos = ts.tokenBeg - pos;
				data = Condition();
			}
			if (!inFunctionParams)
			{
				MustMatchToken(Token.RP, "msg.no.paren.let");
			}
			GeneratorExpression pn = new GeneratorExpression(pos, ts.tokenEnd - pos);
			pn.SetResult(result);
			pn.SetLoops(loops);
			if (data != null)
			{
				pn.SetIfPosition(ifPos);
				pn.SetFilter(data.condition);
				pn.SetFilterLp(data.lp - pos);
				pn.SetFilterRp(data.rp - pos);
			}
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private GeneratorExpressionLoop GeneratorExpressionLoop()
		{
			if (NextToken() != Token.FOR)
			{
				CodeBug();
			}
			int pos = ts.tokenBeg;
			int lp = -1;
			int rp = -1;
			int inPos = -1;
			GeneratorExpressionLoop pn = new GeneratorExpressionLoop(pos);
			PushScope(pn);
			try
			{
				if (MustMatchToken(Token.LP, "msg.no.paren.for"))
				{
					lp = ts.tokenBeg - pos;
				}
				AstNode iter = null;
				switch (PeekToken())
				{
					case Token.LB:
					case Token.LC:
					{
						// handle destructuring assignment
						iter = DestructuringPrimaryExpr();
						MarkDestructuring(iter);
						break;
					}

					case Token.NAME:
					{
						ConsumeToken();
						iter = CreateNameNode();
						break;
					}

					default:
					{
						ReportError("msg.bad.var");
						break;
					}
				}
				// Define as a let since we want the scope of the variable to
				// be restricted to the array comprehension
				if (iter.GetType() == Token.NAME)
				{
					DefineSymbol(Token.LET, ts.GetString(), true);
				}
				if (MustMatchToken(Token.IN, "msg.in.after.for.name"))
				{
					inPos = ts.tokenBeg - pos;
				}
				AstNode obj = Expr();
				if (MustMatchToken(Token.RP, "msg.no.paren.for.ctrl"))
				{
					rp = ts.tokenBeg - pos;
				}
				pn.SetLength(ts.tokenEnd - pos);
				pn.SetIterator(iter);
				pn.SetIteratedObject(obj);
				pn.SetInPosition(inPos);
				pn.SetParens(lp, rp);
				return pn;
			}
			finally
			{
				PopScope();
			}
		}

		private const int PROP_ENTRY = 1;

		private const int GET_ENTRY = 2;

		private const int SET_ENTRY = 4;

		/// <exception cref="System.IO.IOException"></exception>
		private ObjectLiteral ObjectLiteral()
		{
			int pos = ts.tokenBeg;
			int lineno = ts.lineno;
			int afterComma = -1;
			IList<ObjectProperty> elems = new List<ObjectProperty>();
			ICollection<string> getterNames = null;
			ICollection<string> setterNames = null;
			if (this.inUseStrictDirective)
			{
				getterNames = new HashSet<string>();
				setterNames = new HashSet<string>();
			}
			Comment objJsdocNode = GetAndResetJsDoc();
			for (; ; )
			{
				string propertyName = null;
				int entryKind = PROP_ENTRY;
				int tt = PeekToken();
				Comment jsdocNode = GetAndResetJsDoc();
				switch (tt)
				{
					case Token.NAME:
					{
						Name name = CreateNameNode();
						propertyName = ts.GetString();
						int ppos = ts.tokenBeg;
						ConsumeToken();
						// This code path needs to handle both destructuring object
						// literals like:
						// var {get, b} = {get: 1, b: 2};
						// and getters like:
						// var x = {get 1() { return 2; };
						// So we check a whitelist of tokens to check if we're at the
						// first case. (Because of keywords, the second case may be
						// many tokens.)
						int peeked = PeekToken();
						bool maybeGetterOrSetter = "get".Equals(propertyName) || "set".Equals(propertyName);
						if (maybeGetterOrSetter && peeked != Token.COMMA && peeked != Token.COLON && peeked != Token.RC)
						{
							bool isGet = "get".Equals(propertyName);
							entryKind = isGet ? GET_ENTRY : SET_ENTRY;
							AstNode pname = ObjliteralProperty();
							if (pname == null)
							{
								propertyName = null;
							}
							else
							{
								propertyName = ts.GetString();
								ObjectProperty objectProp = GetterSetterProperty(ppos, pname, isGet);
								pname.SetJsDocNode(jsdocNode);
								elems.Add(objectProp);
							}
						}
						else
						{
							name.SetJsDocNode(jsdocNode);
							elems.Add(PlainProperty(name, tt));
						}
						break;
					}

					case Token.RC:
					{
						if (afterComma != -1)
						{
							WarnTrailingComma(pos, elems, afterComma);
						}
						goto commaLoop_break;
					}

					default:
					{
						AstNode pname_1 = ObjliteralProperty();
						if (pname_1 == null)
						{
							propertyName = null;
						}
						else
						{
							propertyName = ts.GetString();
							pname_1.SetJsDocNode(jsdocNode);
							elems.Add(PlainProperty(pname_1, tt));
						}
						break;
					}
				}
				if (this.inUseStrictDirective && propertyName != null)
				{
					switch (entryKind)
					{
						case PROP_ENTRY:
						{
							if (getterNames.Contains(propertyName) || setterNames.Contains(propertyName))
							{
								AddError("msg.dup.obj.lit.prop.strict", propertyName);
							}
							getterNames.Add(propertyName);
							setterNames.Add(propertyName);
							break;
						}

						case GET_ENTRY:
						{
							if (getterNames.Contains(propertyName))
							{
								AddError("msg.dup.obj.lit.prop.strict", propertyName);
							}
							getterNames.Add(propertyName);
							break;
						}

						case SET_ENTRY:
						{
							if (setterNames.Contains(propertyName))
							{
								AddError("msg.dup.obj.lit.prop.strict", propertyName);
							}
							setterNames.Add(propertyName);
							break;
						}
					}
				}
				// Eat any dangling jsdoc in the property.
				GetAndResetJsDoc();
				if (MatchToken(Token.COMMA))
				{
					afterComma = ts.tokenEnd;
				}
				else
				{
					goto commaLoop_break;
				}
commaLoop_continue: ;
			}
commaLoop_break: ;
			MustMatchToken(Token.RC, "msg.no.brace.prop");
			ObjectLiteral pn = new ObjectLiteral(pos, ts.tokenEnd - pos);
			if (objJsdocNode != null)
			{
				pn.SetJsDocNode(objJsdocNode);
			}
			pn.SetElements(elems);
			pn.SetLineno(lineno);
			return pn;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstNode ObjliteralProperty()
		{
			AstNode pname;
			int tt = PeekToken();
			switch (tt)
			{
				case Token.NAME:
				{
					pname = CreateNameNode();
					break;
				}

				case Token.STRING:
				{
					pname = CreateStringLiteral();
					break;
				}

				case Token.NUMBER:
				{
					pname = new NumberLiteral(ts.tokenBeg, ts.GetString(), ts.GetNumber());
					break;
				}

				default:
				{
					if (compilerEnv.IsReservedKeywordAsIdentifier() && TokenStream.IsKeyword(ts.GetString()))
					{
						// convert keyword to property name, e.g. ({if: 1})
						pname = CreateNameNode();
						break;
					}
					ReportError("msg.bad.prop");
					return null;
				}
			}
			ConsumeToken();
			return pname;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private ObjectProperty PlainProperty(AstNode property, int ptt)
		{
			// Support, e.g., |var {x, y} = o| as destructuring shorthand
			// for |var {x: x, y: y} = o|, as implemented in spidermonkey JS 1.8.
			int tt = PeekToken();
			if ((tt == Token.COMMA || tt == Token.RC) && ptt == Token.NAME && compilerEnv.GetLanguageVersion() >= LanguageVersion.VERSION_1_8)
			{
				if (!inDestructuringAssignment)
				{
					ReportError("msg.bad.object.init");
				}
				AstNode nn = new Name(property.GetPosition(), property.GetString());
				ObjectProperty pn = new ObjectProperty();
				pn.PutProp(Node.DESTRUCTURING_SHORTHAND, true);
				pn.SetLeftAndRight(property, nn);
				return pn;
			}
			MustMatchToken(Token.COLON, "msg.no.colon.prop");
			ObjectProperty pn_1 = new ObjectProperty();
			pn_1.SetOperatorPosition(ts.tokenBeg);
			pn_1.SetLeftAndRight(property, AssignExpr());
			return pn_1;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private ObjectProperty GetterSetterProperty(int pos, AstNode propName, bool isGetter)
		{
			FunctionNode fn = Function(FunctionNode.FUNCTION_EXPRESSION);
			// We've already parsed the function name, so fn should be anonymous.
			Name name = fn.GetFunctionName();
			if (name != null && name.Length() != 0)
			{
				ReportError("msg.bad.prop");
			}
			ObjectProperty pn = new ObjectProperty(pos);
			if (isGetter)
			{
				pn.SetIsGetter();
			}
			else
			{
				pn.SetIsSetter();
			}
			int end = GetNodeEnd(fn);
			pn.SetLeft(propName);
			pn.SetRight(fn);
			pn.SetLength(end - pos);
			return pn;
		}

		private Name CreateNameNode()
		{
			return CreateNameNode(false, Token.NAME);
		}

		/// <summary>
		/// Create a
		/// <code>Name</code>
		/// node using the token info from the
		/// last scanned name.  In some cases we need to either synthesize
		/// a name node, or we lost the name token information by peeking.
		/// If the
		/// <code>token</code>
		/// parameter is not
		/// <see cref="Token.NAME">Token.NAME</see>
		/// , then
		/// we use token info saved in instance vars.
		/// </summary>
		private Name CreateNameNode(bool checkActivation, int token)
		{
			int beg = ts.tokenBeg;
			string s = ts.GetString();
			int lineno = ts.lineno;
			if (!string.Empty.Equals(prevNameTokenString))
			{
				beg = prevNameTokenStart;
				s = prevNameTokenString;
				lineno = prevNameTokenLineno;
				prevNameTokenStart = 0;
				prevNameTokenString = string.Empty;
				prevNameTokenLineno = 0;
			}
			if (s == null)
			{
				if (compilerEnv.IsIdeMode())
				{
					s = string.Empty;
				}
				else
				{
					CodeBug();
				}
			}
			Name name = new Name(beg, s);
			name.SetLineno(lineno);
			if (checkActivation)
			{
				CheckActivationName(s, token);
			}
			return name;
		}

		private StringLiteral CreateStringLiteral()
		{
			int pos = ts.tokenBeg;
			int end = ts.tokenEnd;
			StringLiteral s = new StringLiteral(pos, end - pos);
			s.SetLineno(ts.lineno);
			s.SetValue(ts.GetString());
			s.SetQuoteCharacter(ts.GetQuoteChar());
			return s;
		}

		protected internal virtual void CheckActivationName(string name, int token)
		{
			if (!InsideFunction())
			{
				return;
			}
			bool activation = false;
			if ("arguments".Equals(name) || (compilerEnv.GetActivationNames() != null && compilerEnv.GetActivationNames().Contains(name)))
			{
				activation = true;
			}
			else
			{
				if ("length".Equals(name))
				{
					if (token == Token.GETPROP && compilerEnv.GetLanguageVersion() == LanguageVersion.VERSION_1_2)
					{
						// Use of "length" in 1.2 requires an activation object.
						activation = true;
					}
				}
			}
			if (activation)
			{
				SetRequiresActivation();
			}
		}

		protected internal virtual void SetRequiresActivation()
		{
			if (InsideFunction())
			{
				((FunctionNode)currentScriptOrFn).SetRequiresActivation();
			}
		}

		private void CheckCallRequiresActivation(AstNode pn)
		{
			if ((pn.GetType() == Token.NAME && "eval".Equals(((Name)pn).GetIdentifier())) || (pn.GetType() == Token.GETPROP && "eval".Equals(((PropertyGet)pn).GetProperty().GetIdentifier())))
			{
				SetRequiresActivation();
			}
		}

		protected internal virtual void SetIsGenerator()
		{
			if (InsideFunction())
			{
				((FunctionNode)currentScriptOrFn).SetIsGenerator();
			}
		}

		private void CheckBadIncDec(UnaryExpression expr)
		{
			AstNode op = RemoveParens(expr.GetOperand());
			int tt = op.GetType();
			if (!(tt == Token.NAME || tt == Token.GETPROP || tt == Token.GETELEM || tt == Token.GET_REF || tt == Token.CALL))
			{
				ReportError(expr.GetType() == Token.INC ? "msg.bad.incr" : "msg.bad.decr");
			}
		}

		private ErrorNode MakeErrorNode()
		{
			ErrorNode pn = new ErrorNode(ts.tokenBeg, ts.tokenEnd - ts.tokenBeg);
			pn.SetLineno(ts.lineno);
			return pn;
		}

		// Return end of node.  Assumes node does NOT have a parent yet.
		private int NodeEnd(AstNode node)
		{
			return node.GetPosition() + node.GetLength();
		}

		private void SaveNameTokenData(int pos, string name, int lineno)
		{
			prevNameTokenStart = pos;
			prevNameTokenString = name;
			prevNameTokenLineno = lineno;
		}

		/// <summary>
		/// Return the file offset of the beginning of the input source line
		/// containing the passed position.
		/// </summary>
		/// <remarks>
		/// Return the file offset of the beginning of the input source line
		/// containing the passed position.
		/// </remarks>
		/// <param name="pos">
		/// an offset into the input source stream.  If the offset
		/// is negative, it's converted to 0, and if it's beyond the end of
		/// the source buffer, the last source position is used.
		/// </param>
		/// <returns>
		/// the offset of the beginning of the line containing pos
		/// (i.e. 1+ the offset of the first preceding newline).  Returns -1
		/// if the
		/// <see cref="CompilerEnvirons">CompilerEnvirons</see>
		/// is not set to ide-mode,
		/// and
		/// <see cref="Parse(System.IO.TextReader, string, int)">Parse(System.IO.TextReader, string, int)</see>
		/// was used.
		/// </returns>
		private int LineBeginningFor(int pos)
		{
			if (sourceChars == null)
			{
				return -1;
			}
			if (pos <= 0)
			{
				return 0;
			}
			char[] buf = sourceChars;
			if (pos >= buf.Length)
			{
				pos = buf.Length - 1;
			}
			while (--pos >= 0)
			{
				char c = buf[pos];
				if (c == '\n' || c == '\r')
				{
					return pos + 1;
				}
			}
			// want position after the newline
			return 0;
		}

		private void WarnMissingSemi(int pos, int end)
		{
			// Should probably change this to be a CompilerEnvirons setting,
			// with an enum Never, Always, Permissive, where Permissive means
			// don't warn for 1-line functions like function (s) {return x+2}
			if (compilerEnv.IsStrictMode())
			{
				int beg = Math.Max(pos, LineBeginningFor(end));
				if (end == -1)
				{
					end = ts.cursor;
				}
				AddStrictWarning("msg.missing.semi", string.Empty, beg, end - beg);
			}
		}

		private void WarnTrailingComma<T>(int pos, IList<T> elems, int commaPos) where T: Node
		{
			if (compilerEnv.GetWarnTrailingComma())
			{
				// back up from comma to beginning of line or array/objlit
				if (elems.Count > 0)
				{
					object foo = elems[0];
					pos = ((AstNode) foo).GetPosition();
				}
				pos = Math.Max(pos, LineBeginningFor(commaPos));
				AddWarning("msg.extra.trailing.comma", pos, commaPos - pos);
			}
		}

		protected internal class PerFunctionVariables
		{
			private ScriptNode savedCurrentScriptOrFn;

			private Scope savedCurrentScope;

			private int savedEndFlags;

			private bool savedInForInit;

			private IDictionary<string, LabeledStatement> savedLabelSet;

			private IList<Loop> savedLoopSet;

			private IList<Jump> savedLoopAndSwitchSet;

			internal PerFunctionVariables(Parser _enclosing, FunctionNode fnNode)
			{
				this._enclosing = _enclosing;
				// helps reduce clutter in the already-large function() method
				this.savedCurrentScriptOrFn = this._enclosing.currentScriptOrFn;
				this._enclosing.currentScriptOrFn = fnNode;
				this.savedCurrentScope = this._enclosing.currentScope;
				this._enclosing.currentScope = fnNode;
				this.savedLabelSet = this._enclosing.labelSet;
				this._enclosing.labelSet = null;
				this.savedLoopSet = this._enclosing.loopSet;
				this._enclosing.loopSet = null;
				this.savedLoopAndSwitchSet = this._enclosing.loopAndSwitchSet;
				this._enclosing.loopAndSwitchSet = null;
				this.savedEndFlags = this._enclosing.endFlags;
				this._enclosing.endFlags = 0;
				this.savedInForInit = this._enclosing.inForInit;
				this._enclosing.inForInit = false;
			}

			internal virtual void Restore()
			{
				this._enclosing.currentScriptOrFn = this.savedCurrentScriptOrFn;
				this._enclosing.currentScope = this.savedCurrentScope;
				this._enclosing.labelSet = this.savedLabelSet;
				this._enclosing.loopSet = this.savedLoopSet;
				this._enclosing.loopAndSwitchSet = this.savedLoopAndSwitchSet;
				this._enclosing.endFlags = this.savedEndFlags;
				this._enclosing.inForInit = this.savedInForInit;
			}

			private readonly Parser _enclosing;
		}

		/// <summary>
		/// Given a destructuring assignment with a left hand side parsed
		/// as an array or object literal and a right hand side expression,
		/// rewrite as a series of assignments to the variables defined in
		/// left from property accesses to the expression on the right.
		/// </summary>
		/// <remarks>
		/// Given a destructuring assignment with a left hand side parsed
		/// as an array or object literal and a right hand side expression,
		/// rewrite as a series of assignments to the variables defined in
		/// left from property accesses to the expression on the right.
		/// </remarks>
		/// <param name="type">declaration type: Token.VAR or Token.LET or -1</param>
		/// <param name="left">
		/// array or object literal containing NAME nodes for
		/// variables to assign
		/// </param>
		/// <param name="right">expression to assign from</param>
		/// <returns>
		/// expression that performs a series of assignments to
		/// the variables defined in left
		/// </returns>
		internal virtual Node CreateDestructuringAssignment(int type, Node left, Node right)
		{
			string tempName = currentScriptOrFn.GetNextTempName();
			Node result = DestructuringAssignmentHelper(type, left, right, tempName);
			Node comma = result.GetLastChild();
			comma.AddChildToBack(CreateName(tempName));
			return result;
		}

		internal virtual Node DestructuringAssignmentHelper(int variableType, Node left, Node right, string tempName)
		{
			Scope result = CreateScopeNode(Token.LETEXPR, left.GetLineno());
			result.AddChildToFront(new Node(Token.LET, CreateName(Token.NAME, tempName, right)));
			try
			{
				PushScope(result);
				DefineSymbol(Token.LET, tempName, true);
			}
			finally
			{
				PopScope();
			}
			Node comma = new Node(Token.COMMA);
			result.AddChildToBack(comma);
			IList<string> destructuringNames = new List<string>();
			bool empty = true;
			switch (left.GetType())
			{
				case Token.ARRAYLIT:
				{
					empty = DestructuringArray((ArrayLiteral)left, variableType, tempName, comma, destructuringNames);
					break;
				}

				case Token.OBJECTLIT:
				{
					empty = DestructuringObject((ObjectLiteral)left, variableType, tempName, comma, destructuringNames);
					break;
				}

				case Token.GETPROP:
				case Token.GETELEM:
				{
					switch (variableType)
					{
						case Token.CONST:
						case Token.LET:
						case Token.VAR:
						{
							ReportError("msg.bad.assign.left");
							break;
						}
					}
					comma.AddChildToBack(SimpleAssignment(left, CreateName(tempName)));
					break;
				}

				default:
				{
					ReportError("msg.bad.assign.left");
					break;
				}
			}
			if (empty)
			{
				// Don't want a COMMA node with no children. Just add a zero.
				comma.AddChildToBack(CreateNumber(0));
			}
			result.PutProp(Node.DESTRUCTURING_NAMES, destructuringNames);
			return result;
		}

		internal virtual bool DestructuringArray(ArrayLiteral array, int variableType, string tempName, Node parent, IList<string> destructuringNames)
		{
			bool empty = true;
			int setOp = variableType == Token.CONST ? Token.SETCONST : Token.SETNAME;
			int index = 0;
			foreach (AstNode n in array.GetElements())
			{
				if (n.GetType() == Token.EMPTY)
				{
					index++;
					continue;
				}
				Node rightElem = new Node(Token.GETELEM, CreateName(tempName), CreateNumber(index));
				if (n.GetType() == Token.NAME)
				{
					string name = n.GetString();
					parent.AddChildToBack(new Node(setOp, CreateName(Token.BINDNAME, name, null), rightElem));
					if (variableType != -1)
					{
						DefineSymbol(variableType, name, true);
						destructuringNames.Add(name);
					}
				}
				else
				{
					parent.AddChildToBack(DestructuringAssignmentHelper(variableType, n, rightElem, currentScriptOrFn.GetNextTempName()));
				}
				index++;
				empty = false;
			}
			return empty;
		}

		internal virtual bool DestructuringObject(ObjectLiteral node, int variableType, string tempName, Node parent, IList<string> destructuringNames)
		{
			bool empty = true;
			int setOp = variableType == Token.CONST ? Token.SETCONST : Token.SETNAME;
			foreach (ObjectProperty prop in node.GetElements())
			{
				int lineno = 0;
				// This function is sometimes called from the IRFactory when
				// when executing regression tests, and in those cases the
				// tokenStream isn't set.  Deal with it.
				if (ts != null)
				{
					lineno = ts.lineno;
				}
				AstNode id = prop.GetLeft();
				Node rightElem;
				var nameId = id as Name;
				if (nameId != null)
				{
					Node s = Node.NewString(nameId.GetIdentifier());
					rightElem = new Node(Token.GETPROP, CreateName(tempName), s);
				}
				else
				{
					var stringLiteralId = id as StringLiteral;
					if (stringLiteralId != null)
					{
						Node s = Node.NewString(stringLiteralId.GetValue());
						rightElem = new Node(Token.GETPROP, CreateName(tempName), s);
					}
					else
					{
						var numberLiteralId = id as NumberLiteral;
						if (numberLiteralId != null)
						{
							Node s = CreateNumber((int)numberLiteralId.GetNumber());
							rightElem = new Node(Token.GETELEM, CreateName(tempName), s);
						}
						else
						{
							throw CodeBug();
						}
					}
				}
				rightElem.SetLineno(lineno);
				AstNode value = prop.GetRight();
				if (value.GetType() == Token.NAME)
				{
					string name = ((Name)value).GetIdentifier();
					parent.AddChildToBack(new Node(setOp, CreateName(Token.BINDNAME, name, null), rightElem));
					if (variableType != -1)
					{
						DefineSymbol(variableType, name, true);
						destructuringNames.Add(name);
					}
				}
				else
				{
					parent.AddChildToBack(DestructuringAssignmentHelper(variableType, value, rightElem, currentScriptOrFn.GetNextTempName()));
				}
				empty = false;
			}
			return empty;
		}

		protected internal virtual Node CreateName(string name)
		{
			CheckActivationName(name, Token.NAME);
			return Node.NewString(Token.NAME, name);
		}

		protected internal virtual Node CreateName(int type, string name, Node child)
		{
			Node result = CreateName(name);
			result.SetType(type);
			if (child != null)
			{
				result.AddChildToBack(child);
			}
			return result;
		}

		protected internal virtual Node CreateNumber(double number)
		{
			return Node.NewNumber(number);
		}

		/// <summary>
		/// Create a node that can be used to hold lexically scoped variable
		/// definitions (via let declarations).
		/// </summary>
		/// <remarks>
		/// Create a node that can be used to hold lexically scoped variable
		/// definitions (via let declarations).
		/// </remarks>
		/// <param name="token">the token of the node to create</param>
		/// <param name="lineno">line number of source</param>
		/// <returns>the created node</returns>
		protected internal virtual Scope CreateScopeNode(int token, int lineno)
		{
			Scope scope = new Scope();
			scope.SetType(token);
			scope.SetLineno(lineno);
			return scope;
		}

		// Quickie tutorial for some of the interpreter bytecodes.
		//
		// GETPROP - for normal foo.bar prop access; right side is a name
		// GETELEM - for normal foo[bar] element access; rhs is an expr
		// SETPROP - for assignment when left side is a GETPROP
		// SETELEM - for assignment when left side is a GETELEM
		// DELPROP - used for delete foo.bar or foo[bar]
		//
		// GET_REF, SET_REF, DEL_REF - in general, these mean you're using
		// get/set/delete on a right-hand side expression (possibly with no
		// explicit left-hand side) that doesn't use the normal JavaScript
		// Object (i.e. ScriptableObject) get/set/delete functions, but wants
		// to provide its own versions instead.  It will ultimately implement
		// Ref, and currently SpecialRef (for __proto__ etc.) and XmlName
		// (for E4X XML objects) are the only implementations.  The runtime
		// notices these bytecodes and delegates get/set/delete to the object.
		//
		// BINDNAME:  used in assignments.  LHS is evaluated first to get a
		// specific object containing the property ("binding" the property
		// to the object) so that it's always the same object, regardless of
		// side effects in the RHS.
		protected internal virtual Node SimpleAssignment(Node left, Node right)
		{
			int nodeType = left.GetType();
			switch (nodeType)
			{
				case Token.NAME:
				{
					if (inUseStrictDirective && "eval".Equals(((Name)left).GetIdentifier()))
					{
						ReportError("msg.bad.id.strict", ((Name)left).GetIdentifier());
					}
					left.SetType(Token.BINDNAME);
					return new Node(Token.SETNAME, left, right);
				}

				case Token.GETPROP:
				case Token.GETELEM:
				{
					Node obj;
					Node id;
					// If it's a PropertyGet or ElementGet, we're in the parse pass.
					// We could alternately have PropertyGet and ElementGet
					// override getFirstChild/getLastChild and return the appropriate
					// field, but that seems just as ugly as this casting.
					var leftPropertyGet = left as PropertyGet;
					if (leftPropertyGet != null)
					{
						obj = leftPropertyGet.GetTarget();
						id = leftPropertyGet.GetProperty();
					}
					else
					{
						var leftElementGet = left as ElementGet;
						if (leftElementGet != null)
						{
							obj = leftElementGet.GetTarget();
							id = leftElementGet.GetElement();
						}
						else
						{
							// This branch is called during IRFactory transform pass.
							obj = left.GetFirstChild();
							id = left.GetLastChild();
						}
					}
					int type;
					if (nodeType == Token.GETPROP)
					{
						type = Token.SETPROP;
						// TODO(stevey) - see https://bugzilla.mozilla.org/show_bug.cgi?id=492036
						// The new AST code generates NAME tokens for GETPROP ids where the old parser
						// generated STRING nodes. If we don't set the type to STRING below, this will
						// cause java.lang.VerifyError in codegen for code like
						// "var obj={p:3};[obj.p]=[9];"
						id.SetType(Token.STRING);
					}
					else
					{
						type = Token.SETELEM;
					}
					return new Node(type, obj, id, right);
				}

				case Token.GET_REF:
				{
					Node @ref = left.GetFirstChild();
					CheckMutableReference(@ref);
					return new Node(Token.SET_REF, @ref, right);
				}
			}
			throw CodeBug();
		}

		protected internal virtual void CheckMutableReference(Node n)
		{
			int memberTypeFlags = n.GetIntProp(Node.MEMBER_TYPE_PROP, 0);
			if ((memberTypeFlags & Node.DESCENDANTS_FLAG) != 0)
			{
				ReportError("msg.bad.assign.left");
			}
		}

		// remove any ParenthesizedExpression wrappers
		protected internal virtual AstNode RemoveParens(AstNode node)
		{
			while (node is ParenthesizedExpression)
			{
				node = ((ParenthesizedExpression)node).GetExpression();
			}
			return node;
		}

		internal virtual void MarkDestructuring(AstNode node)
		{
			var destructuringForm = node as DestructuringForm;
			if (destructuringForm != null)
			{
				destructuringForm.SetIsDestructuring(true);
			}
			else
			{
				var parenthesizedExpression = node as ParenthesizedExpression;
				if (parenthesizedExpression != null)
				{
					MarkDestructuring(parenthesizedExpression.GetExpression());
				}
			}
		}

		// throw a failed-assertion with some helpful debugging info
		/// <exception cref="System.Exception"></exception>
		private Exception CodeBug()
		{
			throw Kit.CodeBug("ts.cursor=" + ts.cursor + ", ts.tokenBeg=" + ts.tokenBeg + ", currentToken=" + currentToken);
		}
	}
}
