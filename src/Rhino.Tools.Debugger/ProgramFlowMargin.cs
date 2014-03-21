using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace Rhino.Tools.Debugger
{
	internal class ProgramFlowMargin : AbstractMargin
	{
		private readonly IProgramFlowInfo flowInfo;
		private int? _currentLine;

		public ProgramFlowMargin(IProgramFlowInfo flowInfo)
		{
			this.flowInfo = flowInfo;
		}

		private new const int Margin = 20;

		protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
		{
			return new PointHitTestResult(this, hitTestParameters.HitPoint);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			return new Size(Margin, 0);
		}

		protected override void OnRender(DrawingContext cx)
		{
			TextView textView = TextView;
			if (textView == null || !textView.VisualLinesValid)
				return;

			foreach (VisualLine visualLine in textView.VisualLines)
			{
				double y = visualLine.VisualTop + visualLine.TextLines[0].Height/2 - textView.VerticalOffset;
				var lineNumber = visualLine.FirstDocumentLine.LineNumber;

				if (flowInfo.IsBreakPoint(lineNumber))
				{
					var brush = new SolidColorBrush(Colors.Maroon);
					cx.DrawEllipse(brush, new Pen(brush, 1), new Point(10, y), 6, 6);
				}
				if (lineNumber == flowInfo.CurrentPosition)
				{
					var g = new StreamGeometry();
					using (var gc = g.Open())
					{
						Point[] points = CreateArrow(5, y, 5);
						gc.BeginFigure(points[0], true, true);
						gc.PolyLineTo(points.Skip(1).ToList(), true, true);
					}

					var colorBrush = new SolidColorBrush(Colors.Yellow);
					cx.DrawGeometry(colorBrush, new Pen(new SolidColorBrush(Colors.Black), 0.5), g);
				}
			}
		}

		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			var textView = TextView;
			if (textView == null || !textView.VisualLinesValid)
				return;
			Point position = e.GetPosition(textView);
			position.X = 0.0;
			position.Y += textView.VerticalOffset;
			var visualLine = textView.GetVisualLineFromVisualTop(position.Y);
			int lineNumber = visualLine.FirstDocumentLine.LineNumber;
			flowInfo.ToggleBreakPoint(lineNumber);
			InvalidateVisual();
			base.OnMouseLeftButtonUp(e);
		}

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			e.Handled = true;
			base.OnMouseLeftButtonDown(e);
		}

		/// <summary>
		///     Called when the <see cref="P:ICSharpCode.AvalonEdit.Editing.AbstractMargin.TextView" /> is changing.
		/// </summary>
		protected override void OnTextViewChanged(TextView oldTextView, TextView newTextView)
		{
			if (oldTextView != null)
				oldTextView.VisualLinesChanged -= TextViewVisualLinesChanged;
			base.OnTextViewChanged(oldTextView, newTextView);
			if (newTextView != null)
				newTextView.VisualLinesChanged += TextViewVisualLinesChanged;
			InvalidateVisual();
		}

		private void TextViewVisualLinesChanged(object sender, EventArgs e)
		{
			InvalidateVisual();
		}

		private static Point[] CreateArrow(double x, double y, int ascent)
		{
			var arrow = new List<Point>();
			double dx = x;
			y += ascent - 10;
			double dy = y;
			arrow.Add(new Point(dx, dy + 3));
			arrow.Add(new Point(dx + 5, dy + 3));
			for (x = dx + 5; x <= dx + 10; x++, y++)
			{
				arrow.Add(new Point(x, y));
			}
			for (x = dx + 9; x >= dx + 5; x--, y++)
			{
				arrow.Add(new Point(x, y));
			}
			arrow.Add(new Point(dx + 5, dy + 7));
			arrow.Add(new Point(dx, dy + 7));
			return arrow.ToArray();
		}
	}
}