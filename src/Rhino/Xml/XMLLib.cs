/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino;
using Rhino.Xml;
using Sharpen;

namespace Rhino.Xml
{
	public abstract class XMLLib
	{
		private static readonly object XML_LIB_KEY = new object();

		/// <summary>An object which specifies an XMLLib implementation to be used at runtime.</summary>
		/// <remarks>
		/// An object which specifies an XMLLib implementation to be used at runtime.
		/// This interface should be considered experimental.  It may be better
		/// (and certainly more flexible) to write an interface that returns an
		/// XMLLib object rather than a class name, for example.  But that would
		/// cause many more ripple effects in the code, all the way back to
		/// <see cref="Rhino.ScriptRuntime">Rhino.ScriptRuntime</see>
		/// .
		/// </remarks>
		public abstract class Factory
		{
			public static XMLLib.Factory Create(string className)
			{
				return new _Factory_26(className);
			}

			private sealed class _Factory_26 : XMLLib.Factory
			{
				public _Factory_26(string className)
				{
					this.className = className;
				}

				public override string GetImplementationClassName()
				{
					return className;
				}

				private readonly string className;
			}

			public abstract string GetImplementationClassName();
		}

		public static XMLLib ExtractFromScopeOrNull(Scriptable scope)
		{
			ScriptableObject so = ScriptRuntime.GetLibraryScopeOrNull(scope);
			if (so == null)
			{
				// If library is not yet initialized, return null
				return null;
			}
			// Ensure lazily initialization of real XML library instance
			// which is done on first access to XML property
			ScriptableObject.GetProperty(so, "XML");
			return (XMLLib)so.GetAssociatedValue(XML_LIB_KEY);
		}

		public static XMLLib ExtractFromScope(Scriptable scope)
		{
			XMLLib lib = ExtractFromScopeOrNull(scope);
			if (lib != null)
			{
				return lib;
			}
			string msg = ScriptRuntime.GetMessage0("msg.XML.not.available");
			throw Context.ReportRuntimeError(msg);
		}

		protected internal XMLLib BindToScope(Scriptable scope)
		{
			ScriptableObject so = ScriptRuntime.GetLibraryScopeOrNull(scope);
			if (so == null)
			{
				// standard library should be initialized at this point
				throw new InvalidOperationException();
			}
			return (XMLLib)so.AssociateValue(XML_LIB_KEY, this);
		}

		public abstract bool IsXMLName(Context cx, object name);

		public abstract Ref NameRef(Context cx, object name, Scriptable scope, int memberTypeFlags);

		public abstract Ref NameRef(Context cx, object @namespace, object name, Scriptable scope, int memberTypeFlags);

		/// <summary>Escapes the reserved characters in a value of an attribute.</summary>
		/// <remarks>Escapes the reserved characters in a value of an attribute.</remarks>
		/// <param name="value">Unescaped text</param>
		/// <returns>The escaped text</returns>
		public abstract string EscapeAttributeValue(object value);

		/// <summary>Escapes the reserved characters in a value of a text node.</summary>
		/// <remarks>Escapes the reserved characters in a value of a text node.</remarks>
		/// <param name="value">Unescaped text</param>
		/// <returns>The escaped text</returns>
		public abstract string EscapeTextValue(object value);

		/// <summary>Construct namespace for default xml statement.</summary>
		/// <remarks>Construct namespace for default xml statement.</remarks>
		public abstract object ToDefaultXmlNamespace(Context cx, object uriValue);

		public virtual void SetIgnoreComments(bool b)
		{
			throw new NotSupportedException();
		}

		public virtual void SetIgnoreWhitespace(bool b)
		{
			throw new NotSupportedException();
		}

		public virtual void SetIgnoreProcessingInstructions(bool b)
		{
			throw new NotSupportedException();
		}

		public virtual void SetPrettyPrinting(bool b)
		{
			throw new NotSupportedException();
		}

		public virtual void SetPrettyIndent(int i)
		{
			throw new NotSupportedException();
		}

		public virtual bool IsIgnoreComments()
		{
			throw new NotSupportedException();
		}

		public virtual bool IsIgnoreProcessingInstructions()
		{
			throw new NotSupportedException();
		}

		public virtual bool IsIgnoreWhitespace()
		{
			throw new NotSupportedException();
		}

		public virtual bool IsPrettyPrinting()
		{
			throw new NotSupportedException();
		}

		public virtual int GetPrettyIndent()
		{
			throw new NotSupportedException();
		}
	}
}
