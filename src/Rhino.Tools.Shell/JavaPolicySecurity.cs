/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using System.Text;
using Rhino;
using Rhino.Tools.Shell;
using Sharpen;

namespace Rhino.Tools.Shell
{
	public class JavaPolicySecurity : SecurityProxy
	{
		public override Type GetStaticSecurityDomainClassInternal()
		{
			return typeof(ProtectionDomain);
		}

		private class Loader : ClassLoader, GeneratedClassLoader
		{
			private ProtectionDomain domain;

			internal Loader(ClassLoader parent, ProtectionDomain domain) : base(parent != null ? parent : GetSystemClassLoader())
			{
				this.domain = domain;
			}

			public virtual Type DefineClass(string name, byte[] data)
			{
				return base.DefineClass(name, data, 0, data.Length, domain);
			}

			public virtual void LinkClass(Type cl)
			{
				ResolveClass(cl);
			}
		}

		[System.Serializable]
		private class ContextPermissions : PermissionCollection
		{
			internal const long serialVersionUID = -1721494496320750721L;

			internal ContextPermissions(ProtectionDomain staticDomain)
			{
				// Construct PermissionCollection that permits an action only
				// if it is permitted by staticDomain and by security context of Java stack on
				// the moment of constructor invocation
				_context = AccessController.GetContext();
				if (staticDomain != null)
				{
					_statisPermissions = staticDomain.GetPermissions();
				}
				SetReadOnly();
			}

			public override void Add(Permission permission)
			{
				throw new Exception("NOT IMPLEMENTED");
			}

			public override bool Implies(Permission permission)
			{
				if (_statisPermissions != null)
				{
					if (!_statisPermissions.Implies(permission))
					{
						return false;
					}
				}
				try
				{
					_context.CheckPermission(permission);
					return true;
				}
				catch (AccessControlException)
				{
					return false;
				}
			}

			public override Enumeration<Permission> Elements()
			{
				return new _Enumeration_82();
			}

			private sealed class _Enumeration_82 : Enumeration<Permission>
			{
				public _Enumeration_82()
				{
				}

				public bool MoveNext()
				{
					return false;
				}

				public Permission Current
				{
					get
					{
						return null;
					}
				}
			}

			public override string ToString()
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(GetType().FullName);
				sb.Append('@');
				sb.Append(Sharpen.Extensions.ToHexString(Runtime.IdentityHashCode(this)));
				sb.Append(" (context=");
				sb.Append(_context);
				sb.Append(", static_permitions=");
				sb.Append(_statisPermissions);
				sb.Append(')');
				return sb.ToString();
			}

			internal AccessControlContext _context;

			internal PermissionCollection _statisPermissions;
		}

		public JavaPolicySecurity()
		{
			// To trigger error on jdk-1.1 with lazy load
			new CodeSource(null, (Certificate[])null);
		}

		protected internal override void CallProcessFileSecure(Context cx, Scriptable scope, string filename)
		{
			AccessController.DoPrivileged(new _PrivilegedAction_117(this, filename, cx, scope));
		}

		private sealed class _PrivilegedAction_117 : PrivilegedAction<object>
		{
			public _PrivilegedAction_117(JavaPolicySecurity _enclosing, string filename, Context cx, Scriptable scope)
			{
				this._enclosing = _enclosing;
				this.filename = filename;
				this.cx = cx;
				this.scope = scope;
			}

			public object Run()
			{
				Uri url = this._enclosing.GetUrlObj(filename);
				ProtectionDomain staticDomain = this._enclosing.GetUrlDomain(url);
				try
				{
					Program.ProcessFileSecure(cx, scope, url.ToExternalForm(), staticDomain);
				}
				catch (IOException ioex)
				{
					throw new Exception(ioex);
				}
				return null;
			}

			private readonly JavaPolicySecurity _enclosing;

			private readonly string filename;

			private readonly Context cx;

			private readonly Scriptable scope;
		}

		private Uri GetUrlObj(string url)
		{
			Uri urlObj;
			try
			{
				urlObj = new Uri(url);
			}
			catch (UriFormatException)
			{
				// Assume as Main.processFileSecure it is file, need to build its
				// URL
				string curDir = Runtime.GetProperty("user.dir");
				curDir = curDir.Replace('\\', '/');
				if (!curDir.EndsWith("/"))
				{
					curDir = curDir + '/';
				}
				try
				{
					Uri curDirURL = new Uri("file:" + curDir);
					urlObj = new Uri(curDirURL, url);
				}
				catch (UriFormatException ex2)
				{
					throw new Exception("Can not construct file URL for '" + url + "':" + ex2.Message);
				}
			}
			return urlObj;
		}

		private ProtectionDomain GetUrlDomain(Uri url)
		{
			CodeSource cs;
			cs = new CodeSource(url, (Certificate[])null);
			PermissionCollection pc = Policy.GetPolicy().GetPermissions(cs);
			return new ProtectionDomain(cs, pc);
		}

		public override GeneratedClassLoader CreateClassLoader(ClassLoader parentLoader, object securityDomain)
		{
			ProtectionDomain domain = (ProtectionDomain)securityDomain;
			return AccessController.DoPrivileged(new _PrivilegedAction_170(parentLoader, domain));
		}

		private sealed class _PrivilegedAction_170 : PrivilegedAction<JavaPolicySecurity.Loader>
		{
			public _PrivilegedAction_170(ClassLoader parentLoader, ProtectionDomain domain)
			{
				this.parentLoader = parentLoader;
				this.domain = domain;
			}

			public JavaPolicySecurity.Loader Run()
			{
				return new JavaPolicySecurity.Loader(parentLoader, domain);
			}

			private readonly ClassLoader parentLoader;

			private readonly ProtectionDomain domain;
		}

		public override object GetDynamicSecurityDomain(object securityDomain)
		{
			ProtectionDomain staticDomain = (ProtectionDomain)securityDomain;
			return GetDynamicDomain(staticDomain);
		}

		private ProtectionDomain GetDynamicDomain(ProtectionDomain staticDomain)
		{
			JavaPolicySecurity.ContextPermissions p = new JavaPolicySecurity.ContextPermissions(staticDomain);
			ProtectionDomain contextDomain = new ProtectionDomain(null, p);
			return contextDomain;
		}

		public override object CallWithDomain(object securityDomain, Context cx, Callable callable, Scriptable scope, Scriptable thisObj, object[] args)
		{
			ProtectionDomain staticDomain = (ProtectionDomain)securityDomain;
			// There is no direct way in Java to intersect permissions according
			// stack context with additional domain.
			// The following implementation first constructs ProtectionDomain
			// that allows actions only allowed by both staticDomain and current
			// stack context, and then constructs AccessController for this dynamic
			// domain.
			// If this is too slow, alternative solution would be to generate
			// class per domain with a proxy method to call to infect
			// java stack.
			// Another optimization in case of scripts coming from "world" domain,
			// that is having minimal default privileges is to construct
			// one AccessControlContext based on ProtectionDomain
			// with least possible privileges and simply call
			// AccessController.doPrivileged with this untrusted context
			ProtectionDomain dynamicDomain = GetDynamicDomain(staticDomain);
			ProtectionDomain[] tmp = new ProtectionDomain[] { dynamicDomain };
			AccessControlContext restricted = new AccessControlContext(tmp);
			PrivilegedAction<object> action = new _PrivilegedAction_218(callable, cx, scope, thisObj, args);
			return AccessController.DoPrivileged(action, restricted);
		}

		private sealed class _PrivilegedAction_218 : PrivilegedAction<object>
		{
			public _PrivilegedAction_218(Callable callable, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
			{
				this.callable = callable;
				this.cx = cx;
				this.scope = scope;
				this.thisObj = thisObj;
				this.args = args;
			}

			public object Run()
			{
				return callable.Call(cx, scope, thisObj, args);
			}

			private readonly Callable callable;

			private readonly Context cx;

			private readonly Scriptable scope;

			private readonly Scriptable thisObj;

			private readonly object[] args;
		}
	}
}
