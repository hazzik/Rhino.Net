/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino.Tools.Debugger;
using Sharpen;

namespace Rhino.Tools.Debugger
{
	/// <summary>Interface for communication between the debugger and its GUI.</summary>
	/// <remarks>
	/// Interface for communication between the debugger and its GUI.  This
	/// should be implemented by the GUI.
	/// </remarks>
	public interface GuiCallback
	{
		/// <summary>Called when the source text of some script has been changed.</summary>
		/// <remarks>Called when the source text of some script has been changed.</remarks>
		void UpdateSourceText(Dim.SourceInfo sourceInfo);

		/// <summary>Called when the interrupt loop has been entered.</summary>
		/// <remarks>Called when the interrupt loop has been entered.</remarks>
		void EnterInterrupt(Dim.StackFrame lastFrame, string threadTitle, string alertMessage);

		/// <summary>Returns whether the current thread is the GUI's event thread.</summary>
		/// <remarks>
		/// Returns whether the current thread is the GUI's event thread.
		/// This information is required to avoid blocking the event thread
		/// from the debugger.
		/// </remarks>
		bool IsGuiEventThread();

		/// <summary>Processes the next GUI event.</summary>
		/// <remarks>
		/// Processes the next GUI event.  This manual pumping of GUI events
		/// is necessary when the GUI event thread itself has been stopped.
		/// </remarks>
		/// <exception cref="System.Exception"></exception>
		void DispatchNextGuiEvent();
	}
}
