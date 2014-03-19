/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>
	/// This class reflects a single Java constructor into the JavaScript
	/// environment.
	/// </summary>
	/// <remarks>
	/// This class reflects a single Java constructor into the JavaScript
	/// environment.  It satisfies a request for an overloaded constructor,
	/// as introduced in LiveConnect 3.
	/// All NativeJavaConstructors behave as JSRef `bound' methods, in that they
	/// always construct the same NativeJavaClass regardless of any reparenting
	/// that may occur.
	/// </remarks>
	/// <author>Frank Mitchell</author>
	/// <seealso cref="NativeJavaMethod">NativeJavaMethod</seealso>
	/// <seealso cref="NativeJavaPackage">NativeJavaPackage</seealso>
	/// <seealso cref="NativeJavaClass">NativeJavaClass</seealso>
	[System.Serializable]
	public class NativeJavaConstructor : BaseFunction
	{
		internal MemberBox ctor;

		public NativeJavaConstructor(MemberBox ctor)
		{
			this.ctor = ctor;
		}

		public override object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			return NativeJavaClass.ConstructSpecific(cx, scope, args, ctor);
		}

		public override string FunctionName
		{
			get
			{
				string sig = JavaMembers.LiveConnectSignature(ctor.argTypes);
				return System.String.Concat("<init>", sig);
			}
		}

		public override string ToString()
		{
			return "[JavaConstructor " + ctor.GetName() + "]";
		}
	}
}
