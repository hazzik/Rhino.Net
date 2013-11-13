/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>
	/// Exception thrown by
	/// <see cref="Context.ExecuteScriptWithContinuations(Script, Scriptable)">Context.ExecuteScriptWithContinuations(Script, Scriptable)</see>
	/// and
	/// <see cref="Context.CallFunctionWithContinuations(Callable, Scriptable, object[])">Context.CallFunctionWithContinuations(Callable, Scriptable, object[])</see>
	/// when execution encounters a continuation captured by
	/// <see cref="Context.CaptureContinuation()">Context.CaptureContinuation()</see>
	/// .
	/// Exception will contain the captured state needed to restart the continuation
	/// with
	/// <see cref="Context.ResumeContinuation(object, Scriptable, object)">Context.ResumeContinuation(object, Scriptable, object)</see>
	/// .
	/// </summary>
	/// <author>Norris Boyd</author>
	[System.Serializable]
	public class ContinuationPending : Exception
	{
		private const long serialVersionUID = 4956008116771118856L;

		private NativeContinuation continuationState;

		private object applicationState;

		/// <summary>Construct a ContinuationPending exception.</summary>
		/// <remarks>
		/// Construct a ContinuationPending exception. Internal call only;
		/// users of the API should get continuations created on their behalf by
		/// calling
		/// <see cref="Context.ExecuteScriptWithContinuations(Script, Scriptable)">Context.ExecuteScriptWithContinuations(Script, Scriptable)</see>
		/// and
		/// <see cref="Context.CallFunctionWithContinuations(Callable, Scriptable, object[])">Context.CallFunctionWithContinuations(Callable, Scriptable, object[])</see>
		/// </remarks>
		/// <param name="continuationState">Internal Continuation object</param>
		internal ContinuationPending(NativeContinuation continuationState)
		{
			this.continuationState = continuationState;
		}

		/// <summary>Get continuation object.</summary>
		/// <remarks>
		/// Get continuation object. The only
		/// use for this object is to be passed to
		/// <see cref="Context.ResumeContinuation(object, Scriptable, object)">Context.ResumeContinuation(object, Scriptable, object)</see>
		/// .
		/// </remarks>
		/// <returns>continuation object</returns>
		public virtual object GetContinuation()
		{
			return continuationState;
		}

		/// <returns>internal continuation state</returns>
		internal virtual NativeContinuation GetContinuationState()
		{
			return continuationState;
		}

		/// <summary>
		/// Store an arbitrary object that applications can use to associate
		/// their state with the continuation.
		/// </summary>
		/// <remarks>
		/// Store an arbitrary object that applications can use to associate
		/// their state with the continuation.
		/// </remarks>
		/// <param name="applicationState">arbitrary application state</param>
		public virtual void SetApplicationState(object applicationState)
		{
			this.applicationState = applicationState;
		}

		/// <returns>arbitrary application state</returns>
		public virtual object GetApplicationState()
		{
			return applicationState;
		}
	}
}
