using Examples;
using SharpD2D;
using SharpD2D.Windows;
using Color = SharpD2D.Drawing.Color;
using Rectangle = SharpD2D.Drawing.Rectangle;

TimerService.EnableHighPrecisionTimers();
WindowHelper.DisableScalingGlobal();
OverlayExample();

static void OverlayExample()
{
    var wind = new OverlayWindow(Rectangle.Create(0, 0, 800, 600))
    {
        FPS = 60
    };
    var ex = new Example(wind, new Color(0x33, 0x36, 0x3F, 128));
    wind.Initialize();
    wind.MessageLoop();
}
