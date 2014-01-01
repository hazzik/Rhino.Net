/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino;
using Rhino.Tools.Shell;
using Sharpen;

namespace Rhino.Tools.Shell
{
	public class ShellContextFactory : ContextFactory
	{
		private bool strictMode;

		private bool warningAsError;

		private int languageVersion = Context.VERSION_1_7;

		private int optimizationLevel;

		private bool generatingDebug;

		private bool allowReservedKeywords = true;

		private ErrorReporter errorReporter;

		private string characterEncoding;

		protected override bool HasFeature(Context cx, int featureIndex)
		{
			switch (featureIndex)
			{
				case Context.FEATURE_STRICT_VARS:
				case Context.FEATURE_STRICT_EVAL:
				case Context.FEATURE_STRICT_MODE:
				{
					return strictMode;
				}

				case Context.FEATURE_RESERVED_KEYWORD_AS_IDENTIFIER:
				{
					return allowReservedKeywords;
				}

				case Context.FEATURE_WARNING_AS_ERROR:
				{
					return warningAsError;
				}

				case Context.FEATURE_LOCATION_INFORMATION_IN_ERROR:
				{
					return generatingDebug;
				}
			}
			return base.HasFeature(cx, featureIndex);
		}

		protected override void OnContextCreated(Context cx)
		{
			cx.SetLanguageVersion(languageVersion);
			cx.SetOptimizationLevel(optimizationLevel);
			if (errorReporter != null)
			{
				cx.SetErrorReporter(errorReporter);
			}
			cx.SetGeneratingDebug(generatingDebug);
			base.OnContextCreated(cx);
		}

		public virtual void SetStrictMode(bool flag)
		{
			CheckNotSealed();
			this.strictMode = flag;
		}

		public virtual void SetWarningAsError(bool flag)
		{
			CheckNotSealed();
			this.warningAsError = flag;
		}

		public virtual void SetLanguageVersion(int version)
		{
			Context.CheckLanguageVersion(version);
			CheckNotSealed();
			this.languageVersion = version;
		}

		public virtual void SetOptimizationLevel(int optimizationLevel)
		{
			Context.CheckOptimizationLevel(optimizationLevel);
			CheckNotSealed();
			this.optimizationLevel = optimizationLevel;
		}

		public virtual void SetErrorReporter(ErrorReporter errorReporter)
		{
			if (errorReporter == null)
			{
				throw new ArgumentException();
			}
			this.errorReporter = errorReporter;
		}

		public virtual void SetGeneratingDebug(bool generatingDebug)
		{
			this.generatingDebug = generatingDebug;
		}

		public virtual string GetCharacterEncoding()
		{
			return characterEncoding;
		}

		public virtual void SetCharacterEncoding(string characterEncoding)
		{
			this.characterEncoding = characterEncoding;
		}

		public virtual void SetAllowReservedKeywords(bool allowReservedKeywords)
		{
			this.allowReservedKeywords = allowReservedKeywords;
		}
	}
}
