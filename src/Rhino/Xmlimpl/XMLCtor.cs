/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino.Utils;
#if XML
using System;
using Rhino;
using Rhino.XmlImpl;
using Sharpen;

namespace Rhino.XmlImpl
{
	[System.Serializable]
	internal class XMLCtor : IdFunctionObject
	{
		private static readonly object XMLCTOR_TAG = "XMLCtor";

		private XmlProcessor options;

		internal XMLCtor(XML xml, object tag, int id, int arity) : base(xml, tag, id, arity)
		{
			//    private XMLLibImpl lib;
			//        this.lib = xml.lib;
			this.options = xml.GetProcessor();
			ActivatePrototypeMap(MAX_FUNCTION_ID);
		}

		private void WriteSetting(Scriptable target)
		{
			for (int i = 1; i <= MAX_INSTANCE_ID; ++i)
			{
				int id = base.GetMaxInstanceId() + i;
				string name = GetInstanceIdName(id);
				object value = GetInstanceIdValue(id);
				ScriptableObject.PutProperty(target, name, value);
			}
		}

		private void ReadSettings(Scriptable source)
		{
			for (int i = 1; i <= MAX_INSTANCE_ID; ++i)
			{
				int id = base.GetMaxInstanceId() + i;
				string name = GetInstanceIdName(id);
				object value = ScriptableObject.GetProperty(source, name);
				if (value == ScriptableConstants.NOT_FOUND)
				{
					continue;
				}
				switch (i)
				{
					case Id_ignoreComments:
					case Id_ignoreProcessingInstructions:
					case Id_ignoreWhitespace:
					case Id_prettyPrinting:
					{
						if (!(value is bool))
						{
							continue;
						}
						break;
					}

					case Id_prettyIndent:
					{
						if (!(value.IsNumber()))
						{
							continue;
						}
						break;
					}

					default:
					{
						throw new InvalidOperationException();
					}
				}
				SetInstanceIdValue(id, value);
			}
		}

		private const int Id_ignoreComments = 1;

		private const int Id_ignoreProcessingInstructions = 2;

		private const int Id_ignoreWhitespace = 3;

		private const int Id_prettyIndent = 4;

		private const int Id_prettyPrinting = 5;

		private const int MAX_INSTANCE_ID = 5;

		// #string_id_map#
		protected internal override int GetMaxInstanceId()
		{
			return base.GetMaxInstanceId() + MAX_INSTANCE_ID;
		}

		protected internal override int FindInstanceIdInfo(string s)
		{
			int id;
			// #generated# Last update: 2007-08-20 09:01:10 EDT
			id = 0;
			string X = null;
			int c;
			switch (s.Length)
			{
				case 12:
				{
					X = "prettyIndent";
					id = Id_prettyIndent;
					goto L_break;
				}

				case 14:
				{
					c = s[0];
					if (c == 'i')
					{
						X = "ignoreComments";
						id = Id_ignoreComments;
					}
					else
					{
						if (c == 'p')
						{
							X = "prettyPrinting";
							id = Id_prettyPrinting;
						}
					}
					goto L_break;
				}

				case 16:
				{
					X = "ignoreWhitespace";
					id = Id_ignoreWhitespace;
					goto L_break;
				}

				case 28:
				{
					X = "ignoreProcessingInstructions";
					id = Id_ignoreProcessingInstructions;
					goto L_break;
				}
			}
L_break: ;
			if (X != null && X != s && !X.Equals(s))
			{
				id = 0;
			}
			goto L0_break;
L0_break: ;
			// #/generated#
			if (id == 0)
			{
				return base.FindInstanceIdInfo(s);
			}
			PropertyAttributes attr;
			switch (id)
			{
				case Id_ignoreComments:
				case Id_ignoreProcessingInstructions:
				case Id_ignoreWhitespace:
				case Id_prettyIndent:
				case Id_prettyPrinting:
				{
					attr = PropertyAttributes.PERMANENT | PropertyAttributes.DONTENUM;
					break;
				}

				default:
				{
					throw new InvalidOperationException();
				}
			}
			return InstanceIdInfo(attr, base.GetMaxInstanceId() + id);
		}

		// #/string_id_map#
		protected internal override string GetInstanceIdName(int id)
		{
			switch (id - base.GetMaxInstanceId())
			{
				case Id_ignoreComments:
				{
					return "ignoreComments";
				}

				case Id_ignoreProcessingInstructions:
				{
					return "ignoreProcessingInstructions";
				}

				case Id_ignoreWhitespace:
				{
					return "ignoreWhitespace";
				}

				case Id_prettyIndent:
				{
					return "prettyIndent";
				}

				case Id_prettyPrinting:
				{
					return "prettyPrinting";
				}
			}
			return base.GetInstanceIdName(id);
		}

		protected internal override object GetInstanceIdValue(int id)
		{
			switch (id - base.GetMaxInstanceId())
			{
				case Id_ignoreComments:
				{
					return options.IsIgnoreComments();
				}

				case Id_ignoreProcessingInstructions:
				{
					return options.IsIgnoreProcessingInstructions();
				}

				case Id_ignoreWhitespace:
				{
					return options.IsIgnoreWhitespace();
				}

				case Id_prettyIndent:
				{
					return options.GetPrettyIndent();
				}

				case Id_prettyPrinting:
				{
					return options.IsPrettyPrinting();
				}
			}
			return base.GetInstanceIdValue(id);
		}

		protected internal override void SetInstanceIdValue(int id, object value)
		{
			switch (id - base.GetMaxInstanceId())
			{
				case Id_ignoreComments:
				{
					options.SetIgnoreComments(ScriptRuntime.ToBoolean(value));
					return;
				}

				case Id_ignoreProcessingInstructions:
				{
					options.SetIgnoreProcessingInstructions(ScriptRuntime.ToBoolean(value));
					return;
				}

				case Id_ignoreWhitespace:
				{
					options.SetIgnoreWhitespace(ScriptRuntime.ToBoolean(value));
					return;
				}

				case Id_prettyIndent:
				{
					options.SetPrettyIndent(ScriptRuntime.ToInt32(value));
					return;
				}

				case Id_prettyPrinting:
				{
					options.SetPrettyPrinting(ScriptRuntime.ToBoolean(value));
					return;
				}
			}
			base.SetInstanceIdValue(id, value);
		}

		private const int Id_defaultSettings = 1;

		private const int Id_settings = 2;

		private const int Id_setSettings = 3;

		private const int MAX_FUNCTION_ID = 3;

		// #string_id_map#
		protected internal override int FindPrototypeId(string s)
		{
			int id;
			// #generated# Last update: 2007-08-20 09:01:10 EDT
			id = 0;
			string X = null;
			int s_length = s.Length;
			if (s_length == 8)
			{
				X = "settings";
				id = Id_settings;
			}
			else
			{
				if (s_length == 11)
				{
					X = "setSettings";
					id = Id_setSettings;
				}
				else
				{
					if (s_length == 15)
					{
						X = "defaultSettings";
						id = Id_defaultSettings;
					}
				}
			}
			if (X != null && X != s && !X.Equals(s))
			{
				id = 0;
			}
			goto L0_break;
L0_break: ;
			// #/generated#
			return id;
		}

		// #/string_id_map#
		protected internal override void InitPrototypeId(int id)
		{
			string s;
			int arity;
			switch (id)
			{
				case Id_defaultSettings:
				{
					arity = 0;
					s = "defaultSettings";
					break;
				}

				case Id_settings:
				{
					arity = 0;
					s = "settings";
					break;
				}

				case Id_setSettings:
				{
					arity = 1;
					s = "setSettings";
					break;
				}

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
			InitPrototypeMethod(XMLCTOR_TAG, id, s, arity);
		}

		public override object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!f.HasTag(XMLCTOR_TAG))
			{
				return base.ExecIdCall(f, cx, scope, thisObj, args);
			}
			int id = f.MethodId();
			switch (id)
			{
				case Id_defaultSettings:
				{
					options.SetDefault();
					Scriptable obj = cx.NewObject(scope);
					WriteSetting(obj);
					return obj;
				}

				case Id_settings:
				{
					Scriptable obj = cx.NewObject(scope);
					WriteSetting(obj);
					return obj;
				}

				case Id_setSettings:
				{
					if (args.Length == 0 || args[0] == null || args[0] == Undefined.instance)
					{
						options.SetDefault();
					}
					else
					{
						var scriptable = args[0] as Scriptable;
						if (scriptable != null)
						{
							ReadSettings(scriptable);
						}
					}
					return Undefined.instance;
				}
			}
			throw new ArgumentException(id.ToString());
		}

		/// <summary>hasInstance for XML objects works differently than other objects; see ECMA357 13.4.3.10.</summary>
		/// <remarks>hasInstance for XML objects works differently than other objects; see ECMA357 13.4.3.10.</remarks>
		public override bool HasInstance(Scriptable instance)
		{
			return (instance is XML || instance is XMLList);
		}
	}
}
#endif