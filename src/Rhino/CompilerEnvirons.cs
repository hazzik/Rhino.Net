/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using Rhino.Ast;

namespace Rhino
{
	public class CompilerEnvirons
	{
		public CompilerEnvirons()
		{
			errorReporter = DefaultErrorReporter.instance;
			languageVersion = LanguageVersion.VERSION_DEFAULT;
			GenerateDebugInfo = true;
			ReservedKeywordAsIdentifier = true;
			AllowMemberExprAsFunctionName = false;
			XmlAvailable = true;
			optimizationLevel = 0;
			GeneratingSource = true;
			StrictMode = false;
			ReportWarningAsError = false;
			GenerateObserverCount = false;
			AllowSharpComments = false;
		}

        public CompilerEnvirons(Context cx)
	    {
			ErrorReporter = cx.GetErrorReporter();
			languageVersion = cx.GetLanguageVersion();
			GenerateDebugInfo = (!cx.IsGeneratingDebugChanged() || cx.IsGeneratingDebug());
			ReservedKeywordAsIdentifier = cx.HasFeature(LanguageFeatures.ReservedKeywordAsIdentifier);
			AllowMemberExprAsFunctionName = cx.HasFeature(LanguageFeatures.MemberExprAsFunctionName);
			StrictMode = cx.HasFeature(LanguageFeatures.StrictMode);
			ReportWarningAsError = cx.HasFeature(LanguageFeatures.WarningAsError);
			XmlAvailable = cx.HasFeature(LanguageFeatures.E4X);
			optimizationLevel = cx.GetOptimizationLevel();
			GeneratingSource = cx.IsGeneratingSource();
			ActivationNames = cx.activationNames;
			// Observer code generation in compiled code :
			GenerateObserverCount = cx.generateObserverCount;
	    }

	    public ErrorReporter ErrorReporter
	    {
	        get { return errorReporter; }
	        set
	        {
	            if (value == null)
	            {
	                throw new ArgumentException();
	            }
	            errorReporter = value;
	        }
	    }

	    public LanguageVersion LanguageVersion
	    {
	        get { return languageVersion; }
	        set
	        {
	            Context.CheckLanguageVersion(value);
	            languageVersion = value;
	        }
	    }

	    public bool GenerateDebugInfo { get; set; }

	    public bool ReservedKeywordAsIdentifier { get; set; }

	    /// <summary>
	    /// Extension to ECMA: if 'function &lt;name&gt;' is not followed
	    /// by '(', assume &lt;name&gt; starts a
	    /// <code>memberExpr</code>
	    /// </summary>
	    public bool AllowMemberExprAsFunctionName { get; set; }

	    public bool XmlAvailable { get; set; }

	    public int OptimizationLevel
	    {
	        get { return optimizationLevel; }
	        set
	        {
	            Context.CheckOptimizationLevel(value);
	            optimizationLevel = value;
	        }
	    }

	    public bool WarnTrailingComma { get; set; }

	    public bool StrictMode { get; set; }

	    public bool ReportWarningAsError { get; set; }

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
	    public bool GeneratingSource { get; set; }

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
	    /// <value>
	    ///   if true, generated code will contain
	    ///   calls to accumulate an estimate of the instructions executed.
	    /// </value>
	    public bool GenerateObserverCount { get; set; }

	    public bool RecordingComments { get; set; }

	    public bool RecordingLocalJsDocComments { get; set; }

	    /// <summary>Turn on or off full error recovery.</summary>
	    /// <remarks>
	    /// Turn on or off full error recovery.  In this mode, parse errors do not
	    /// throw an exception, and the parser attempts to build a full syntax tree
	    /// from the input.  Useful for IDEs and other frontends.
	    /// </remarks>
	    public bool RecoverFromErrors { get; set; }

	    /// <summary>Puts the parser in "IDE" mode.</summary>
	    /// <remarks>
	    /// Puts the parser in "IDE" mode.  This enables some slightly more expensive
	    /// computations, such as figuring out helpful error bounds.
	    /// </remarks>
	    public bool IdeMode { get; set; }

	    public ICollection<string> ActivationNames { get; set; }

	    /// <summary>Mozilla sources use the C preprocessor.</summary>
	    /// <remarks>Mozilla sources use the C preprocessor.</remarks>
	    public bool AllowSharpComments { get; set; }

	    /// <summary>
		/// Returns a <code>CompilerEnvirons</code> suitable for using Rhino
		/// in an IDE environment.  Most features are enabled by default.
		/// The <see cref="Rhino.ErrorReporter">ErrorReporter</see> is set to 
		/// an <see cref="Rhino.Ast.ErrorCollector">Rhino.Ast.ErrorCollector</see>.
		/// </summary>
		public static CompilerEnvirons IdeEnvirons()
	    {
	        return new CompilerEnvirons
	        {
	            RecoverFromErrors = true,
	            RecordingComments = true,
	            StrictMode = true,
	            WarnTrailingComma = true,
	            LanguageVersion = LanguageVersion.VERSION_1_7,
	            ReservedKeywordAsIdentifier = true,
	            IdeMode = true,
	            ErrorReporter = new ErrorCollector()
	        };
		}

		private ErrorReporter errorReporter;

		private LanguageVersion languageVersion;

	    private int optimizationLevel;
	}
}
