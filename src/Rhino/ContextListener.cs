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
	[System.ObsoleteAttribute(@"Embeddings that wish to customize newly createdContext instances should implementListener .")]
	public interface ContextListener : ContextFactory.Listener
	{
		// API class
		[System.ObsoleteAttribute(@"Rhino runtime never calls the method.")]
		void ContextEntered(Context cx);

		[System.ObsoleteAttribute(@"Rhino runtime never calls the method.")]
		void ContextExited(Context cx);
	}
}
