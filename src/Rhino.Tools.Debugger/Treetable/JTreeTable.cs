/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino.Tools.Debugger.Treetable;
using Sharpen;

namespace Rhino.Tools.Debugger.Treetable
{
	/// <summary>
	/// This example shows how to create a simple JTreeTable component,
	/// by using a JTree as a renderer (and editor) for the cells in a
	/// particular column in the JTable.
	/// </summary>
	/// <remarks>
	/// This example shows how to create a simple JTreeTable component,
	/// by using a JTree as a renderer (and editor) for the cells in a
	/// particular column in the JTable.
	/// </remarks>
	/// <version>1.2 10/27/98</version>
	/// <author>Philip Milne</author>
	/// <author>Scott Violet</author>
	[System.Serializable]
	public class JTreeTable : JTable
	{
		/// <summary>A subclass of JTree.</summary>
		/// <remarks>A subclass of JTree.</remarks>
		protected internal JTreeTable.TreeTableCellRenderer tree;

		public JTreeTable(TreeTableModel treeTableModel) : base()
		{
			// Create the tree. It will be used as a renderer and editor.
			tree = new JTreeTable.TreeTableCellRenderer(this, treeTableModel);
			// Install a tableModel representing the visible rows in the tree.
			base.SetModel(new TreeTableModelAdapter(treeTableModel, tree));
			// Force the JTable and JTree to share their row selection models.
			JTreeTable.ListToTreeSelectionModelWrapper selectionWrapper = new JTreeTable.ListToTreeSelectionModelWrapper(this);
			tree.SetSelectionModel(selectionWrapper);
			SetSelectionModel(selectionWrapper.GetListSelectionModel());
			// Install the tree editor renderer and editor.
			SetDefaultRenderer(typeof(TreeTableModel), tree);
			SetDefaultEditor(typeof(TreeTableModel), new JTreeTable.TreeTableCellEditor(this));
			// No grid.
			SetShowGrid(false);
			// No intercell spacing
			SetIntercellSpacing(new Dimension(0, 0));
			// And update the height of the trees row to match that of
			// the table.
			if (tree.GetRowHeight() < 1)
			{
				// Metal looks better like this.
				SetRowHeight(18);
			}
		}

		/// <summary>Overridden to message super and forward the method to the tree.</summary>
		/// <remarks>
		/// Overridden to message super and forward the method to the tree.
		/// Since the tree is not actually in the component hierarchy it will
		/// never receive this unless we forward it in this manner.
		/// </remarks>
		public override void UpdateUI()
		{
			base.UpdateUI();
			if (tree != null)
			{
				tree.UpdateUI();
			}
			// Use the tree's default foreground and background colors in the
			// table.
			LookAndFeel.InstallColorsAndFont(this, "Tree.background", "Tree.foreground", "Tree.font");
		}

		public override int GetEditingRow()
		{
			return (GetColumnClass(editingColumn) == typeof(TreeTableModel)) ? -1 : editingRow;
		}

		/// <summary>Overridden to pass the new rowHeight to the tree.</summary>
		/// <remarks>Overridden to pass the new rowHeight to the tree.</remarks>
		public override void SetRowHeight(int rowHeight)
		{
			base.SetRowHeight(rowHeight);
			if (tree != null && tree.GetRowHeight() != rowHeight)
			{
				tree.SetRowHeight(GetRowHeight());
			}
		}

		/// <summary>Returns the tree that is being shared between the model.</summary>
		/// <remarks>Returns the tree that is being shared between the model.</remarks>
		public virtual JTree GetTree()
		{
			return tree;
		}

		/// <summary>A TreeCellRenderer that displays a JTree.</summary>
		/// <remarks>A TreeCellRenderer that displays a JTree.</remarks>
		[System.Serializable]
		public class TreeTableCellRenderer : JTree, TableCellRenderer
		{
			/// <summary>Last table/tree row asked to renderer.</summary>
			/// <remarks>Last table/tree row asked to renderer.</remarks>
			protected internal int visibleRow;

			public TreeTableCellRenderer(JTreeTable _enclosing, TreeModel model) : base(model)
			{
				this._enclosing = _enclosing;
			}

			/// <summary>
			/// updateUI is overridden to set the colors of the Tree's renderer
			/// to match that of the table.
			/// </summary>
			/// <remarks>
			/// updateUI is overridden to set the colors of the Tree's renderer
			/// to match that of the table.
			/// </remarks>
			public override void UpdateUI()
			{
				base.UpdateUI();
				// Make the tree's cell renderer use the table's cell selection
				// colors.
				TreeCellRenderer tcr = this.GetCellRenderer();
				if (tcr is DefaultTreeCellRenderer)
				{
					DefaultTreeCellRenderer dtcr = ((DefaultTreeCellRenderer)tcr);
					// For 1.1 uncomment this, 1.2 has a bug that will cause an
					// exception to be thrown if the border selection color is
					// null.
					// dtcr.setBorderSelectionColor(null);
					dtcr.SetTextSelectionColor(UIManager.GetColor("Table.selectionForeground"));
					dtcr.SetBackgroundSelectionColor(UIManager.GetColor("Table.selectionBackground"));
				}
			}

			/// <summary>
			/// Sets the row height of the tree, and forwards the row height to
			/// the table.
			/// </summary>
			/// <remarks>
			/// Sets the row height of the tree, and forwards the row height to
			/// the table.
			/// </remarks>
			public override void SetRowHeight(int rowHeight)
			{
				if (rowHeight > 0)
				{
					base.SetRowHeight(rowHeight);
					if (this._enclosing != null && this._enclosing.GetRowHeight() != rowHeight)
					{
						this._enclosing.SetRowHeight(this.GetRowHeight());
					}
				}
			}

			/// <summary>This is overridden to set the height to match that of the JTable.</summary>
			/// <remarks>This is overridden to set the height to match that of the JTable.</remarks>
			public override void SetBounds(int x, int y, int w, int h)
			{
				base.SetBounds(x, 0, w, this._enclosing.GetHeight());
			}

			/// <summary>
			/// Sublcassed to translate the graphics such that the last visible
			/// row will be drawn at 0,0.
			/// </summary>
			/// <remarks>
			/// Sublcassed to translate the graphics such that the last visible
			/// row will be drawn at 0,0.
			/// </remarks>
			public override void Paint(Graphics g)
			{
				g.Translate(0, -this.visibleRow * this.GetRowHeight());
				base.Paint(g);
			}

			/// <summary>TreeCellRenderer method.</summary>
			/// <remarks>TreeCellRenderer method. Overridden to update the visible row.</remarks>
			public virtual Component GetTableCellRendererComponent(JTable table, object value, bool isSelected, bool hasFocus, int row, int column)
			{
				if (isSelected)
				{
					this.SetBackground(table.GetSelectionBackground());
				}
				else
				{
					this.SetBackground(table.GetBackground());
				}
				this.visibleRow = row;
				return this;
			}

			private readonly JTreeTable _enclosing;
		}

		/// <summary>TreeTableCellEditor implementation.</summary>
		/// <remarks>
		/// TreeTableCellEditor implementation. Component returned is the
		/// JTree.
		/// </remarks>
		public class TreeTableCellEditor : AbstractCellEditor, TableCellEditor
		{
			public virtual Component GetTableCellEditorComponent(JTable table, object value, bool isSelected, int r, int c)
			{
				return this._enclosing.tree;
			}

			/// <summary>
			/// Overridden to return false, and if the event is a mouse event
			/// it is forwarded to the tree.<p>
			/// The behavior for this is debatable, and should really be offered
			/// as a property.
			/// </summary>
			/// <remarks>
			/// Overridden to return false, and if the event is a mouse event
			/// it is forwarded to the tree.<p>
			/// The behavior for this is debatable, and should really be offered
			/// as a property. By returning false, all keyboard actions are
			/// implemented in terms of the table. By returning true, the
			/// tree would get a chance to do something with the keyboard
			/// events. For the most part this is ok. But for certain keys,
			/// such as left/right, the tree will expand/collapse where as
			/// the table focus should really move to a different column. Page
			/// up/down should also be implemented in terms of the table.
			/// By returning false this also has the added benefit that clicking
			/// outside of the bounds of the tree node, but still in the tree
			/// column will select the row, whereas if this returned true
			/// that wouldn't be the case.
			/// <p>By returning false we are also enforcing the policy that
			/// the tree will never be editable (at least by a key sequence).
			/// </remarks>
			public override bool IsCellEditable(EventObject e)
			{
				if (e is MouseEvent)
				{
					for (int counter = this._enclosing.GetColumnCount() - 1; counter >= 0; counter--)
					{
						if (this._enclosing.GetColumnClass(counter) == typeof(TreeTableModel))
						{
							MouseEvent me = (MouseEvent)e;
							MouseEvent newME = new MouseEvent(this._enclosing.tree, me.GetID(), me.GetWhen(), me.GetModifiers(), me.GetX() - this._enclosing.GetCellRect(0, counter, true).x, me.GetY(), me.GetClickCount(), me.IsPopupTrigger());
							this._enclosing.tree.DispatchEvent(newME);
							break;
						}
					}
				}
				return false;
			}

			internal TreeTableCellEditor(JTreeTable _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly JTreeTable _enclosing;
		}

		/// <summary>
		/// ListToTreeSelectionModelWrapper extends DefaultTreeSelectionModel
		/// to listen for changes in the ListSelectionModel it maintains.
		/// </summary>
		/// <remarks>
		/// ListToTreeSelectionModelWrapper extends DefaultTreeSelectionModel
		/// to listen for changes in the ListSelectionModel it maintains. Once
		/// a change in the ListSelectionModel happens, the paths are updated
		/// in the DefaultTreeSelectionModel.
		/// </remarks>
		[System.Serializable]
		public class ListToTreeSelectionModelWrapper : DefaultTreeSelectionModel
		{
			/// <summary>Set to true when we are updating the ListSelectionModel.</summary>
			/// <remarks>Set to true when we are updating the ListSelectionModel.</remarks>
			protected internal bool updatingListSelectionModel;

			public ListToTreeSelectionModelWrapper(JTreeTable _enclosing) : base()
			{
				this._enclosing = _enclosing;
				this.GetListSelectionModel().AddListSelectionListener(this.CreateListSelectionListener());
			}

			/// <summary>Returns the list selection model.</summary>
			/// <remarks>
			/// Returns the list selection model. ListToTreeSelectionModelWrapper
			/// listens for changes to this model and updates the selected paths
			/// accordingly.
			/// </remarks>
			public virtual ListSelectionModel GetListSelectionModel()
			{
				return this.listSelectionModel;
			}

			/// <summary>
			/// This is overridden to set <code>updatingListSelectionModel</code>
			/// and message super.
			/// </summary>
			/// <remarks>
			/// This is overridden to set <code>updatingListSelectionModel</code>
			/// and message super. This is the only place DefaultTreeSelectionModel
			/// alters the ListSelectionModel.
			/// </remarks>
			public override void ResetRowSelection()
			{
				if (!this.updatingListSelectionModel)
				{
					this.updatingListSelectionModel = true;
					try
					{
						base.ResetRowSelection();
					}
					finally
					{
						this.updatingListSelectionModel = false;
					}
				}
			}

			// Notice how we don't message super if
			// updatingListSelectionModel is true. If
			// updatingListSelectionModel is true, it implies the
			// ListSelectionModel has already been updated and the
			// paths are the only thing that needs to be updated.
			/// <summary>Creates and returns an instance of ListSelectionHandler.</summary>
			/// <remarks>Creates and returns an instance of ListSelectionHandler.</remarks>
			protected internal virtual ListSelectionListener CreateListSelectionListener()
			{
				return new JTreeTable.ListToTreeSelectionModelWrapper.ListSelectionHandler(this);
			}

			/// <summary>
			/// If <code>updatingListSelectionModel</code> is false, this will
			/// reset the selected paths from the selected rows in the list
			/// selection model.
			/// </summary>
			/// <remarks>
			/// If <code>updatingListSelectionModel</code> is false, this will
			/// reset the selected paths from the selected rows in the list
			/// selection model.
			/// </remarks>
			protected internal virtual void UpdateSelectedPathsFromSelectedRows()
			{
				if (!this.updatingListSelectionModel)
				{
					this.updatingListSelectionModel = true;
					try
					{
						// This is way expensive, ListSelectionModel needs an
						// enumerator for iterating.
						int min = this.listSelectionModel.GetMinSelectionIndex();
						int max = this.listSelectionModel.GetMaxSelectionIndex();
						this.ClearSelection();
						if (min != -1 && max != -1)
						{
							for (int counter = min; counter <= max; counter++)
							{
								if (this.listSelectionModel.IsSelectedIndex(counter))
								{
									TreePath selPath = this._enclosing.tree.GetPathForRow(counter);
									if (selPath != null)
									{
										this.AddSelectionPath(selPath);
									}
								}
							}
						}
					}
					finally
					{
						this.updatingListSelectionModel = false;
					}
				}
			}

			/// <summary>
			/// Class responsible for calling updateSelectedPathsFromSelectedRows
			/// when the selection of the list changse.
			/// </summary>
			/// <remarks>
			/// Class responsible for calling updateSelectedPathsFromSelectedRows
			/// when the selection of the list changse.
			/// </remarks>
			internal class ListSelectionHandler : ListSelectionListener
			{
				public virtual void ValueChanged(ListSelectionEvent e)
				{
					this._enclosing.UpdateSelectedPathsFromSelectedRows();
				}

				internal ListSelectionHandler(ListToTreeSelectionModelWrapper _enclosing)
				{
					this._enclosing = _enclosing;
				}

				private readonly ListToTreeSelectionModelWrapper _enclosing;
			}

			private readonly JTreeTable _enclosing;
		}
	}
}
