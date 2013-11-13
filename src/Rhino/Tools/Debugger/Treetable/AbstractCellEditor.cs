/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Javax.Swing;
using Javax.Swing.Event;
using Rhino.Tools.Debugger.Treetable;
using Sharpen;

namespace Rhino.Tools.Debugger.Treetable
{
	public class AbstractCellEditor : CellEditor
	{
		protected internal EventListenerList listenerList = new EventListenerList();

		public virtual object GetCellEditorValue()
		{
			return null;
		}

		public virtual bool IsCellEditable(EventObject e)
		{
			return true;
		}

		public virtual bool ShouldSelectCell(EventObject anEvent)
		{
			return false;
		}

		public virtual bool StopCellEditing()
		{
			return true;
		}

		public virtual void CancelCellEditing()
		{
		}

		public virtual void AddCellEditorListener(CellEditorListener l)
		{
			listenerList.Add<CellEditorListener>(l);
		}

		public virtual void RemoveCellEditorListener(CellEditorListener l)
		{
			listenerList.Remove<CellEditorListener>(l);
		}

		protected internal virtual void FireEditingStopped()
		{
			// Guaranteed to return a non-null array
			object[] listeners = listenerList.GetListenerList();
			// Process the listeners last to first, notifying
			// those that are interested in this event
			for (int i = listeners.Length - 2; i >= 0; i -= 2)
			{
				if (listeners[i] == typeof(CellEditorListener))
				{
					((CellEditorListener)listeners[i + 1]).EditingStopped(new ChangeEvent(this));
				}
			}
		}

		protected internal virtual void FireEditingCanceled()
		{
			// Guaranteed to return a non-null array
			object[] listeners = listenerList.GetListenerList();
			// Process the listeners last to first, notifying
			// those that are interested in this event
			for (int i = listeners.Length - 2; i >= 0; i -= 2)
			{
				if (listeners[i] == typeof(CellEditorListener))
				{
					((CellEditorListener)listeners[i + 1]).EditingCanceled(new ChangeEvent(this));
				}
			}
		}
	}
}
