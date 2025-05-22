using System.Drawing;

public class ThemeColors
{
    public string Name { get; set; }
    public Color FormBackColor { get; set; }
    public Color FormForeColor { get; set; }
    public Color ButtonBackColor { get; set; }
    public Color ButtonForeColor { get; set; }
    public Color ButtonBorderColor { get; set; }
    public Color ButtonMouseOverBackColor { get; set; }
    public Color ButtonMouseDownBackColor { get; set; }
    public Color AccentButtonBackColor { get; set; }
    public Color AccentButtonForeColor { get; set; }
    public Color AccentButtonBorderColor { get; set; }
    public Color AccentButtonMouseOverBackColor { get; set; }
    public Color AccentButtonMouseDownBackColor { get; set; }
    public Color InputBackColor { get; set; }
    public Color InputForeColor { get; set; }
    public Color InputBorderColor { get; set; }
    public Color LabelForeColor { get; set; }
    public Color InfoNotificationBackColor { get; set; }
    public Color InfoNotificationForeColor { get; set; }
    public Color WarningNotificationBackColor { get; set; }
    public Color WarningNotificationForeColor { get; set; }
    public Color ErrorNotificationBackColor { get; set; }
    public Color ErrorNotificationForeColor { get; set; }
    public Color SuccessNotificationBackColor { get; set; }
    public Color SuccessNotificationForeColor { get; set; }
    public Color LogBackColor { get; set; }
    public Color LogForeColor { get; set; }

    public static ThemeColors LightTheme = new ThemeColors
    {
        Name = "ライト",
        FormBackColor = Color.FromArgb(240, 240, 240),
        FormForeColor = Color.FromArgb(40, 40, 40),
        ButtonBackColor = Color.FromArgb(220, 220, 220),
        ButtonForeColor = Color.FromArgb(40, 40, 40),
        ButtonBorderColor = Color.FromArgb(200, 200, 200),
        ButtonMouseOverBackColor = Color.FromArgb(200, 200, 200),
        ButtonMouseDownBackColor = Color.FromArgb(180, 180, 180),
        AccentButtonBackColor = Color.FromArgb(0, 122, 204),
        AccentButtonForeColor = Color.White,
        AccentButtonBorderColor = Color.FromArgb(0, 100, 180),
        AccentButtonMouseOverBackColor = Color.FromArgb(0, 142, 224),
        AccentButtonMouseDownBackColor = Color.FromArgb(0, 80, 160),
        InputBackColor = Color.White,
        InputForeColor = Color.FromArgb(40, 40, 40),
        InputBorderColor = Color.FromArgb(150, 150, 150),
        LabelForeColor = Color.FromArgb(40, 40, 40),
        InfoNotificationBackColor = Color.FromArgb(217, 237, 247),
        InfoNotificationForeColor = Color.FromArgb(49, 112, 143),
        WarningNotificationBackColor = Color.FromArgb(252, 248, 227),
        WarningNotificationForeColor = Color.FromArgb(138, 109, 59),
        ErrorNotificationBackColor = Color.FromArgb(242, 222, 222),
        ErrorNotificationForeColor = Color.FromArgb(169, 68, 66),
        SuccessNotificationBackColor = Color.FromArgb(223, 240, 216),
        SuccessNotificationForeColor = Color.FromArgb(60, 118, 61),
        LogBackColor = Color.WhiteSmoke,
        LogForeColor = Color.Black,
    };

    public static ThemeColors DarkTheme = new ThemeColors
    {
        Name = "ダーク",
        FormBackColor = Color.FromArgb(45, 45, 48),
        FormForeColor = Color.FromArgb(220, 220, 220),
        ButtonBackColor = Color.FromArgb(63, 63, 70),
        ButtonForeColor = Color.FromArgb(220, 220, 220),
        ButtonBorderColor = Color.FromArgb(80, 80, 80),
        ButtonMouseOverBackColor = Color.FromArgb(80, 80, 85),
        ButtonMouseDownBackColor = Color.FromArgb(100, 100, 105),
        AccentButtonBackColor = Color.FromArgb(0, 122, 204),
        AccentButtonForeColor = Color.White,
        AccentButtonBorderColor = Color.FromArgb(0, 100, 180),
        AccentButtonMouseOverBackColor = Color.FromArgb(20, 142, 224),
        AccentButtonMouseDownBackColor = Color.FromArgb(0, 80, 160),
        InputBackColor = Color.FromArgb(30, 30, 30),
        InputForeColor = Color.FromArgb(220, 220, 220),
        InputBorderColor = Color.FromArgb(80, 80, 80),
        LabelForeColor = Color.FromArgb(220, 220, 220),
        InfoNotificationBackColor = Color.FromArgb(50, 70, 90),
        InfoNotificationForeColor = Color.FromArgb(217, 237, 247),
        WarningNotificationBackColor = Color.FromArgb(90, 80, 50),
        WarningNotificationForeColor = Color.FromArgb(252, 248, 227),
        ErrorNotificationBackColor = Color.FromArgb(90, 50, 50),
        ErrorNotificationForeColor = Color.FromArgb(242, 222, 222),
        SuccessNotificationBackColor = Color.FromArgb(50, 90, 50),
        SuccessNotificationForeColor = Color.FromArgb(223, 240, 216),
        LogBackColor = Color.FromArgb(20, 20, 20),
        LogForeColor = Color.LightGray,
    };
}