using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Mp3Gain2026;

internal class DarkToolStripRenderer : ToolStripProfessionalRenderer
{
	private static readonly Color BgToolbar = Color.FromArgb(22, 22, 32);

	private static readonly Color BgHover = Color.FromArgb(45, 45, 65);

	private static readonly Color BgPressed = Color.FromArgb(60, 60, 85);

	private static readonly Color Accent = Color.FromArgb(99, 102, 241);

	private static readonly Color BorderClr = Color.FromArgb(55, 55, 75);

	private static readonly Color TextClr = Color.FromArgb(237, 237, 245);

	private static readonly Color SepColor = Color.FromArgb(50, 50, 70);

	protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
	{
		e.Graphics.Clear(BgToolbar);
	}

	protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
	{
		Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);
		Color color;
		if (e.Item.Pressed)
		{
			color = BgPressed;
		}
		else
		{
			if (!e.Item.Selected)
			{
				return;
			}
			color = BgHover;
		}
		using SolidBrush brush = new SolidBrush(color);
		using GraphicsPath path = RoundedRect(bounds, 6);
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		e.Graphics.FillPath(brush, path);
	}

	protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
	{
		if (e.Item.IsOnDropDown)
		{
			Rectangle rect = new Rectangle(0, 0, e.Item.Width, e.Item.Height);
			using SolidBrush brush = new SolidBrush(Color.FromArgb(40, 40, 40));
			e.Graphics.FillRectangle(brush, rect);
			using Pen pen = new Pen(SepColor);
			e.Graphics.DrawLine(pen, 30, e.Item.Height / 2, e.Item.Width - 10, e.Item.Height / 2);
			return;
		}
		int num = e.Item.Width / 2;
		using Pen pen2 = new Pen(SepColor);
		e.Graphics.DrawLine(pen2, num, 4, num, e.Item.Height - 4);
	}

	protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
	{
		Rectangle rect = new Rectangle(Point.Empty, e.Item.Size);
		if (e.Item.IsOnDropDown)
		{
			Color color = (e.Item.Selected ? BgHover : Color.FromArgb(40, 40, 40));
			using SolidBrush brush = new SolidBrush(color);
			e.Graphics.FillRectangle(brush, rect);
			return;
		}
		if (e.Item.Pressed)
		{
			using (SolidBrush brush2 = new SolidBrush(BgPressed))
			{
				e.Graphics.FillRectangle(brush2, rect);
				return;
			}
		}
		if (e.Item.Selected)
		{
			using (SolidBrush brush3 = new SolidBrush(BgHover))
			{
				e.Graphics.FillRectangle(brush3, rect);
				return;
			}
		}
		using SolidBrush brush4 = new SolidBrush(BgToolbar);
		e.Graphics.FillRectangle(brush4, rect);
	}

	protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
	{
		e.TextColor = (e.Item.IsOnDropDown ? Color.WhiteSmoke : TextClr);
		base.OnRenderItemText(e);
	}

	protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
	{
		if (e.ToolStrip.IsDropDown)
		{
			using (Pen pen = new Pen(BorderClr))
			{
				e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1));
			}
		}
	}

	private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
	{
		GraphicsPath graphicsPath = new GraphicsPath();
		int num = radius * 2;
		Rectangle rect = new Rectangle(bounds.Location, new Size(num, num));
		graphicsPath.AddArc(rect, 180f, 90f);
		rect.X = bounds.Right - num;
		graphicsPath.AddArc(rect, 270f, 90f);
		rect.Y = bounds.Bottom - num;
		graphicsPath.AddArc(rect, 0f, 90f);
		rect.X = bounds.Left;
		graphicsPath.AddArc(rect, 90f, 90f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}
}
