Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Friend Class DarkToolStripRenderer
    Inherits ToolStripProfessionalRenderer

    Private Shared ReadOnly BgToolbar As Color = Color.FromArgb(22, 22, 32)
    Private Shared ReadOnly BgHover As Color = Color.FromArgb(45, 45, 65)
    Private Shared ReadOnly BgPressed As Color = Color.FromArgb(60, 60, 85)
    Private Shared ReadOnly Accent As Color = Color.FromArgb(99, 102, 241)
    Private Shared ReadOnly BorderClr As Color = Color.FromArgb(55, 55, 75)
    Private Shared ReadOnly TextClr As Color = Color.FromArgb(237, 237, 245)
    Private Shared ReadOnly SepColor As Color = Color.FromArgb(50, 50, 70)

    Protected Overrides Sub OnRenderToolStripBackground(e As ToolStripRenderEventArgs)
        e.Graphics.Clear(BgToolbar)
    End Sub

    Protected Overrides Sub OnRenderButtonBackground(e As ToolStripItemRenderEventArgs)
        Dim bounds As New Rectangle(Point.Empty, e.Item.Size)
        Dim bg As Color

        If e.Item.Pressed Then
            bg = BgPressed
        ElseIf e.Item.Selected Then
            bg = BgHover
        Else
            Return ' transparent
        End If

        Using brush As New SolidBrush(bg)
            Using path = RoundedRect(bounds, 6)
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                e.Graphics.FillPath(brush, path)
            End Using
        End Using
    End Sub

    Protected Overrides Sub OnRenderSeparator(e As ToolStripSeparatorRenderEventArgs)
        If e.Item.IsOnDropDown Then
            Dim rect As New Rectangle(0, 0, e.Item.Width, e.Item.Height)
            Using bgBrush As New SolidBrush(Color.FromArgb(40, 40, 40))
                e.Graphics.FillRectangle(bgBrush, rect)
            End Using
            Using pen As New Pen(SepColor)
                e.Graphics.DrawLine(pen, 30, e.Item.Height \ 2, e.Item.Width - 10, e.Item.Height \ 2)
            End Using
        Else
            Dim x As Integer = e.Item.Width \ 2
            Using pen As New Pen(SepColor)
                e.Graphics.DrawLine(pen, x, 4, x, e.Item.Height - 4)
            End Using
        End If
    End Sub

    Protected Overrides Sub OnRenderMenuItemBackground(e As ToolStripItemRenderEventArgs)
        Dim rect As New Rectangle(Point.Empty, e.Item.Size)
        If e.Item.IsOnDropDown Then
            Dim bg As Color = If(e.Item.Selected, BgHover, Color.FromArgb(40, 40, 40))
            Using brush As New SolidBrush(bg)
                e.Graphics.FillRectangle(brush, rect)
            End Using
        Else
            If e.Item.Pressed Then
                Using brush As New SolidBrush(BgPressed)
                    e.Graphics.FillRectangle(brush, rect)
                End Using
            ElseIf e.Item.Selected Then
                Using brush As New SolidBrush(BgHover)
                    e.Graphics.FillRectangle(brush, rect)
                End Using
            Else
                Using brush As New SolidBrush(BgToolbar)
                    e.Graphics.FillRectangle(brush, rect)
                End Using
            End If
        End If
    End Sub

    Protected Overrides Sub OnRenderItemText(e As ToolStripItemTextRenderEventArgs)
        e.TextColor = If(e.Item.IsOnDropDown, Color.WhiteSmoke, TextClr)
        MyBase.OnRenderItemText(e)
    End Sub

    Protected Overrides Sub OnRenderToolStripBorder(e As ToolStripRenderEventArgs)
        If e.ToolStrip.IsDropDown Then
            Using pen As New Pen(BorderClr)
                e.Graphics.DrawRectangle(pen, New Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1))
            End Using
        End If
    End Sub

    Private Shared Function RoundedRect(bounds As Rectangle, radius As Integer) As GraphicsPath
        Dim path As New GraphicsPath()
        Dim d As Integer = radius * 2
        Dim arc As New Rectangle(bounds.Location, New Size(d, d))

        path.AddArc(arc, 180, 90)
        arc.X = bounds.Right - d
        path.AddArc(arc, 270, 90)
        arc.Y = bounds.Bottom - d
        path.AddArc(arc, 0, 90)
        arc.X = bounds.Left
        path.AddArc(arc, 90, 90)
        path.CloseFigure()
        Return path
    End Function

End Class
