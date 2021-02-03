open System
open Gtk
open SkiaSharp
open SkiaSharp.Views.Desktop
open SkiaSharp.Views.Gtk

module WindowStuff =
    type PaintFunc = SKPaintSurfaceEventArgs -> unit
    type DeleteFunc = DeleteEventArgs -> unit
    type ButtonPressFunc = ButtonPressEventArgs -> unit
    type ButtonReleaseFunc = ButtonReleaseEventArgs -> unit
    type MotionNotifyFunc = MotionNotifyEventArgs -> unit

    type MainWindow =
        inherit Window

        new(title: string,
            canvas: SKDrawingArea,
            deleteFunc: DeleteFunc,
            paintFunc: PaintFunc,
            pressFunc: ButtonPressFunc,
            releaseFunc: ButtonReleaseFunc,
            motionFunc: MotionNotifyFunc) as this =
            { inherit Window(title) }
            then
                this.DeleteEvent.Add(deleteFunc)
                this.ButtonPressEvent.Add(pressFunc)
                this.ButtonReleaseEvent.Add(releaseFunc)
                this.MotionNotifyEvent.Add(motionFunc)

                // technically we could wire this up outside, but might as well do it here for consistency
                canvas.PaintSurface.Add(paintFunc)
                canvas.Show()

                this.Child <- canvas
                this.SetDefaultSize(800, 600)

type WindowState =
    { Canvas: SKDrawingArea
      MouseIsDown: bool
      Points: List<SKPoint> }

let mutable appState =
    { Canvas = null
      MouseIsDown = false
      Points = List.empty }

let linePaint =
    new SKPaint(Color = SKColors.Red, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 4f)

let textPaint =
    new SKPaint(Color = SKColors.White,
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                TextAlign = SKTextAlign.Center,
                TextSize = 24f)

let simplePaintFunc (e: SKPaintSurfaceEventArgs) =
    let canvas = e.Surface.Canvas
    
    // === scaling stuff plucked verbatim from the SkiaSharp sample - I assume for DPI scaling?
    
    // get the screen density for scaling
    let scale = 1f

    let scaledSize =
        SKSize(float32 e.Info.Width / scale, float32 e.Info.Height / scale)

    // handle the device screen density
    canvas.Scale(scale)
    
    // === end scaling stuff =====

    canvas.Clear(SKColors.Black)

    // draw all the points
    for points in Seq.pairwise appState.Points do
        let pStart = fst points
        let pEnd = snd points
        canvas.DrawLine(pStart.X, pStart.Y, pEnd.X, pEnd.Y, linePaint)

    // draw some text
    let coord =
        SKPoint(scaledSize.Width / 2f, (scaledSize.Height + textPaint.TextSize) / 2f)

    canvas.DrawText("Drag to Paint!", coord, textPaint)


let pressFunc (e: ButtonPressEventArgs) =
    appState <-
        { appState with
              MouseIsDown = true
              Points =
                  SKPoint(float32 e.Event.X, float32 e.Event.Y)
                  :: appState.Points }

    appState.Canvas.QueueDraw() // repaint

let releaseFunc (e: ButtonReleaseEventArgs) =
    appState <- { appState with MouseIsDown = false }

let motionFunc (e: MotionNotifyEventArgs) =
    if appState.MouseIsDown then
        appState <-
            { appState with
                  Points =
                      SKPoint(float32 e.Event.X, float32 e.Event.Y)
                      :: appState.Points }

        appState.Canvas.QueueDraw() // repaint

[<EntryPoint>]
[<STAThread>]
let main argv =
    Application.Init()

    let app =
        new Application("com.companyname.skiasharpexample", GLib.ApplicationFlags.None)

    app.Register(GLib.Cancellable.Current) |> ignore

    appState <-
        { appState with
              Canvas = new SKDrawingArea() }

    let win =
        new WindowStuff.MainWindow("Hello Minimal Example!",
                                   appState.Canvas,
                                   (fun _ -> Application.Quit()),
                                   simplePaintFunc,
                                   pressFunc,
                                   releaseFunc,
                                   motionFunc)

    app.AddWindow(win)

    win.Show()
    Application.Run()

    0 // return an integer exit code
