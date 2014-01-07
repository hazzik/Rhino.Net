/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino
{
	public class CompilerEnvirons
	{
		public CompilerEnvirons()
		{
			errorReporter = DefaultErrorReporter.instance;
			languageVersion = LanguageVersion.VERSION_DEFAULT;
			generateDebugInfo = true;
			reservedKeywordAsIdentifier = true;
			allowMemberExprAsFunctionName = false;
			xmlAvailable = true;
			optimizationLevel = 0;
			generatingSource = true;
			strictMode = false;
			warningAsError = false;
			generateObserverCount = false;
			allowSharpComments = false;
		}

		public virtual void InitFromContext(Context cx)
		{
			SetErrorReporter(cx.GetErrorReporter());
			languageVersion = cx.GetLanguageVersion();
			generateDebugInfo = (!cx.IsGeneratingDebugChanged() || cx.IsGeneratingDebug());
			reservedKeywordAsIdentifier = cx.HasFeature(Context.FEATURE_RESERVED_KEYWORD_AS_IDENTIFIER);
			allowMemberExprAsFunctionName = cx.HasFeature(Context.FEATURE_MEMBER_EXPR_AS_FUNCTION_NAME);
			strictMode = cx.HasFeature(Context.FEATURE_STRICT_MODE);
			warningAsError = cx.HasFeature(Context.FEATURE_WARNING_AS_ERROR);
			xmlAvailable = cx.HasFeature(Context.FEATURE_E4X);
			optimizationLevel = cx.GetOptimizationLevel();
			generatingSource = cx.IsGeneratingSource();
			activationNames = cx.activationNames;
			// Observer code generation in compiled code :
			generateObserverCount = cx.generateObserverCount;
		}

		public ErrorReporter GetErrorReporter()
		{
			return errorReporter;
		}

		public virtual void SetErrorReporter(ErrorReporter errorReporter)
		{
			if (errorReporter == null)
			{
				throw new ArgumentException();
			}
			this.errorReporter = errorReporter;
		}

		public LanguageVersion GetLanguageVersion()
		{
			return languageVersion;
		}

		public virtual void SetLanguageVersion(LanguageVersion languageVersion)
		{
			Context.CheckLanguageVersion(languageVersion);
			this.languageVersion = languageVersion;
		}

		public bool IsGenerateDebugInfo()
		{
			return generateDebugInfo;
		}

		public virtual void SetGenerateDebugInfo(bool flag)
		{
			this.generateDebugInfo = flag;
		}

		public bool IsReservedKeywordAsIdentifier()
		{
			return reservedKeywordAsIdentifier;
		}

		public virtual void SetReservedKeywordAsIdentifier(bool flag)
		{
			reservedKeywordAsIdentifier = flag;
		}

		/// <summary>
		/// Extension to ECMA: if 'function &lt;name&gt;' is not followed
		/// by '(', assume &lt;name&gt; starts a
		/// <code>memberExpr</code>
		/// </summary>
		public bool IsAllowMemberExprAsFunctionName()
		{
			return allowMemberExprAsFunctionName;
		}

		public virtual void SetAllowMemberExprAsFunctionName(bool flag)
		{
			allowMemberExprAsFunctionName = flag;
		}

		public bool IsXmlAvailable()
		{
			return xmlAvailable;
		}

		public virtual void SetXmlAvailable(bool flag)
		{
			xmlAvailable = flag;
		}

		public int GetOptimizationLevel()
		{
			return optimizationLevel;
		}

		public virtual void SetOptimizationLevel(int level)
		{
			Context.CheckOptimizationLevel(level);
			this.optimizationLevel = level;
		}

		public bool IsGeneratingSource()
		{
			return generatingSource;
		}

		public virtual bool GetWarnTrailingComma()
		{
			return warnTrailingComma;
		}

		public virtual void SetWarnTrailingComma(bool warn)
		{
			warnTrailingComma = warn;
		}

		public bool IsStrictMode()
		{
			return strictMode;
		}

		public virtual void SetStrictMode(bool strict)
		{
			strictMode = strict;
		}

		public bool ReportWarningAsError()
		{
			return warningAsError;
		}

		/// <summary>Specify whether or not source information should be generated.</summary>
		/// <remarks>
		/// Specify whether or not source information should be generated.
		/// <p>
		/// Without source information, evaluating the "toString" method
		/// on JavaScript functions produces only "[native code]" for
		/// the body of the function.
		/// Note that code generated without source is not fully ECMA
		/// conformant.
		/// </remarks>
		public virtual void SetGeneratingSource(bool generatingSource)
		{
			this.generatingSource = generatingSource;
		}

		/// <returns>
		/// true iff code will be generated with callbacks to enable
		/// instruction thresholds
		/// </returns>
		public virtual bool IsGenerateObserverCount()
		{
			return generateObserverCount;
		}

		/// <summary>
		/// Turn on or off generation of code with callbacks to
		/// track the count of executed instructions.
		/// </summary>
		/// <remarks>
		/// Turn on or off generation of code with callbacks to
		/// track the count of executed instructions.
		/// Currently only affects JVM byte code generation: this slows down the
		/// generated code, but code generated without the callbacks will not
		/// be counted toward instruction thresholds. Rhino's interpretive
		/// mode does instruction counting without inserting callbacks, so
		/// there is no requirement to compile code differently.
		/// </remarks>
		/// <param name="generateObserverCount">
		/// if true, generated code will contain
		/// calls to accumulate an estimate of the instructions executed.
		/// </param>
		public virtual void SetGenerateObserverCount(bool generateObserverCount)
		{
			this.generateObserverCount = generateObserverCount;
		}

		public virtual bool IsRecordingComments()
		{
			return recordingComments;
		}

		public virtual void SetRecordingComments(bool record)
		{
			recordingComments = record;
		}

		public virtual bool IsRecordingLocalJsDocComments()
		{
			return recordingLocalJsDocComments;
		}

		public virtual void SetRecordingLocalJsDocComments(bool record)
		{
			recordingLocalJsDocComments = record;
		}

		/// <summary>Turn on or off full error recovery.</summary>
		/// <remarks>
		/// Turn on or off full error recovery.  In this mode, parse errors do not
		/// throw an exception, and the parser attempts to build a full syntax tree
		/// from the input.  Useful for IDEs and other frontends.
		/// </remarks>
		public virtual void SetRecoverFromErrors(bool recover)
		{
			recoverFromErrors = recover;
		}

		public virtual bool RecoverFromErrors()
		{
			return recoverFromErrors;
		}

		/// <summary>Puts the parser in "IDE" mode.</summary>
		/// <remarks>
		/// Puts the parser in "IDE" mode.  This enables some slightly more expensive
		/// computations, such as figuring out helpful error bounds.
		/// </remarks>
		public virtual void SetIdeMode(bool ide)
		{
			ideMode = ide;
		}

		public virtual bool IsIdeMode()
		{
			return ideMode;
		}

		public virtual ICollection<string> GetActivationNames()
		{
			return activationNames;
		}

		public virtual void SetActivationNames(ICollection<string> activationNames)
		{
			this.activationNames = activationNames;
		}

		/// <summary>Mozilla sources use the C preprocessor.</summary>
		/// <remarks>Mozilla sources use the C preprocessor.</remarks>
		public virtual void SetAllowSharpComments(bool allow)
		{
			allowSharpComments = allow;
		}

		public virtual bool GetAllowSharpComments()
		{
			return allowSharpComments;
		}

		/// <summary>
		/// Returns a
		/// <code>CompilerEnvirons</code>
		/// suitable for using Rhino
		/// in an IDE environment.  Most features are enabled by default.
		/// The
		/// <see cref="ErrorReporter">ErrorReporter</see>
		/// is set to an
		/// <see cref="Rhino.Ast.ErrorCollector">Rhino.Ast.ErrorCollector</see>
		/// .
		/// </summary>
		public static Rhino.CompilerEnvirons IdeEnvirons()
		{
			Rhino.CompilerEnvirons env = new Rhino.CompilerEnvirons();
			env.SetRecoverFromErrors(true);
			env.SetRecordingComments(true);
			env.SetStrictMode(true);
			env.SetWarnTrailingComma(true);
            env.SetLanguageVersion(LanguageVersion.VERSION_1_7);
			env.SetReservedKeywordAsIdentifier(true);
			env.SetIdeMode(true);
			env.SetErrorReporter(new ErrorCollector());
			return env;
		}

		private ErrorReporter errorReporter;

		private LanguageVersion languageVersion;

		private bool generateDebugInfo;

		private bool reservedKeywordAsIdentifier;

		private bool allowMemberExprAsFunctionName;

		private bool xmlAvailable;

		private int optimizationLevel;

		private bool generatingSource;

		private bool strictMode;

		private bool warningAsError;

		private bool generateObserverCount;

		private bool recordingComments;

		private bool recordingLocalJsDocComments;

		private bool recoverFromErrors;

		private bool warnTrailingComma;

		private bool ideMode;

		private bool allowSharpComments;

		internal ICollection<string> activationNames;
	}
}
