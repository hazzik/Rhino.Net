/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Javax.Swing;
using Javax.Swing.Event;
using Javax.Swing.Table;
using Javax.Swing.Tree;
using Rhino.Tools.Debugger.Treetable;
using Sharpen;

namespace Rhino.Tools.Debugger.Treetable
{
	/// <summary>
	/// This is a wrapper class takes a TreeTableModel and implements
	/// the table model interface.
	/// </summary>
	/// <remarks>
	/// This is a wrapper class takes a TreeTableModel and implements
	/// the table model interface. The implementation is trivial, with
	/// all of the event dispatching support provided by the superclass:
	/// the AbstractTableModel.
	/// </remarks>
	/// <version>1.2 10/27/98</version>
	/// <author>Philip Milne</author>
	/// <author>Scott Violet</author>
	[System.Serializable]
	public class TreeTableModelAdapter : AbstractTableModel
	{
		private const long serialVersionUID = 48741114609209052L;

		internal JTree tree;

		internal TreeTableModel treeTableModel;

		public TreeTableModelAdapter(TreeTableModel treeTableModel, JTree tree)
		{
			this.tree = tree;
			this.treeTableModel = treeTableModel;
			tree.AddTreeExpansionListener(new _TreeExpansionListener_65(this));
			// Don't use fireTableRowsInserted() here; the selection model
			// would get updated twice.
			// Install a TreeModelListener that can update the table when
			// tree changes. We use delayedFireTableDataChanged as we can
			// not be guaranteed the tree will have finished processing
			// the event before us.
			treeTableModel.AddTreeModelListener(new _TreeModelListener_80(this));
		}

		private sealed class _TreeExpansionListener_65 : TreeExpansionListener
		{
			public _TreeExpansionListener_65(TreeTableModelAdapter _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public void TreeExpanded(TreeExpansionEvent @event)
			{
				this._enclosing.FireTableDataChanged();
			}

			public void TreeCollapsed(TreeExpansionEvent @event)
			{
				this._enclosing.FireTableDataChanged();
			}

			private readonly TreeTableModelAdapter _enclosing;
		}

		private sealed class _TreeModelListener_80 : TreeModelListener
		{
			public _TreeModelListener_80(TreeTableModelAdapter _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public void TreeNodesChanged(TreeModelEvent e)
			{
				this._enclosing.DelayedFireTableDataChanged();
			}

			public void TreeNodesInserted(TreeModelEvent e)
			{
				this._enclosing.DelayedFireTableDataChanged();
			}

			public void TreeNodesRemoved(TreeModelEvent e)
			{
				this._enclosing.DelayedFireTableDataChanged();
			}

			public void TreeStructureChanged(TreeModelEvent e)
			{
				this._enclosing.DelayedFireTableDataChanged();
			}

			private readonly TreeTableModelAdapter _enclosing;
		}

		// Wrappers, implementing TableModel interface.
		public override int GetColumnCount()
		{
			return treeTableModel.GetColumnCount();
		}

		public override string GetColumnName(int column)
		{
			return treeTableModel.GetColumnName(column);
		}

		public override Type GetColumnClass(int column)
		{
			return treeTableModel.GetColumnClass(column);
		}

		public override int GetRowCount()
		{
			return tree.GetRowCount();
		}

		protected internal virtual object NodeForRow(int row)
		{
			TreePath treePath = tree.GetPathForRow(row);
			return treePath.GetLastPathComponent();
		}

		public override object GetValueAt(int row, int column)
		{
			return treeTableModel.GetValueAt(NodeForRow(row), column);
		}

		public override bool IsCellEditable(int row, int column)
		{
			return treeTableModel.IsCellEditable(NodeForRow(row), column);
		}

		public override void SetValueAt(object value, int row, int column)
		{
			treeTableModel.SetValueAt(value, NodeForRow(row), column);
		}

		/// <summary>
		/// Invokes fireTableDataChanged after all the pending events have been
		/// processed.
		/// </summary>
		/// <remarks>
		/// Invokes fireTableDataChanged after all the pending events have been
		/// processed. SwingUtilities.invokeLater is used to handle this.
		/// </remarks>
		protected internal virtual void DelayedFireTableDataChanged()
		{
			SwingUtilities.InvokeLater(new _Runnable_143(this));
		}

		private sealed class _Runnable_143 : Runnable
		{
			public _Runnable_143(TreeTableModelAdapter _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public void Run()
			{
				this._enclosing.FireTableDataChanged();
			}

			private readonly TreeTableModelAdapter _enclosing;
		}
	}
}
