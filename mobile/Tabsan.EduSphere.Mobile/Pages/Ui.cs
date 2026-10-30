using System.Globalization;
using Microsoft.Maui.Controls.Shapes;
using Tabsan.EduSphere.Mobile.Core;

namespace Tabsan.EduSphere.Mobile.Pages;

/// <summary>
/// The app's look, shared by every screen: Tabsan navy and teal (the portal's colours, also used in the user manuals),
/// rounded cards, a banner at the top of the main screens, chips for states and framed input fields. Light and dark.
/// </summary>
internal static class Ui
{
	/// <summary>The institution's colour (the build's AppColor, Tabsan navy by default); set at start-up.</summary>
	public static Color Brand { get; set; } = Color.FromArgb("#0F2A4A");
	public static readonly Color Accent = Color.FromArgb("#1590A8");
	public static readonly Color Sky = Color.FromArgb("#2F80C8");
	public static readonly Color Good = Color.FromArgb("#18865F");
	public static readonly Color Warn = Color.FromArgb("#B97912");
	public static readonly Color Bad = Color.FromArgb("#B63E4D");

	private static readonly Color InkLight = Color.FromArgb("#1F2937"), InkDark = Color.FromArgb("#E5EDF7");
	private static readonly Color MutedLight = Color.FromArgb("#64748B"), MutedDark = Color.FromArgb("#A9B6C8");
	private static readonly Color SurfaceLight = Colors.White, SurfaceDark = Color.FromArgb("#131C2E");
	private static readonly Color LineLight = Color.FromArgb("#E2E8F0"), LineDark = Color.FromArgb("#23314A");
	private static readonly Color HeadingDark = Color.FromArgb("#F8FAFC");

	public static string T(string key) => AppText.Get(key);

	// ── text ──────────────────────────────────────────────────────────────────────────────────

	/// <summary>Section heading.</summary>
	public static Label Heading(string text)
	{
		var label = new Label { Text = text, FontSize = 18, FontFamily = "OpenSansSemibold", Margin = new Thickness(2, 18, 2, 4) };
		label.SetAppThemeColor(Label.TextColorProperty, Brand, HeadingDark);
		return label;
	}

	/// <summary>A card's main line.</summary>
	public static Label Title(string? text = null, double size = 15)
	{
		var label = new Label { Text = text, FontSize = size, FontFamily = "OpenSansSemibold" };
		label.SetAppThemeColor(Label.TextColorProperty, InkLight, InkDark);
		return label;
	}

	public static Label Body(string? text = null)
	{
		var label = new Label { Text = text, FontSize = 14, LineHeight = 1.15 };
		label.SetAppThemeColor(Label.TextColorProperty, InkLight, InkDark);
		return label;
	}

	public static Label Small(string? text = null)
	{
		var label = new Label { Text = text, FontSize = 13 };
		label.SetAppThemeColor(Label.TextColorProperty, MutedLight, MutedDark);
		return label;
	}

	/// <summary>White text on the banner.</summary>
	public static Label OnBanner(string? text, double size, bool bold = false, double opacity = 1)
		=> new() { Text = text, FontSize = size, FontFamily = bold ? "OpenSansSemibold" : "OpenSansRegular", TextColor = Colors.White, Opacity = opacity };

	// ── messages ──────────────────────────────────────────────────────────────────────────────

	/// <summary>The page's error, in a red box (hidden when there is none).</summary>
	public static View Status(PageViewModel vm) => Message(nameof(PageViewModel.Error), Bad, "#FDECEE", "#3A1C22", "#F4A3AE");

	/// <summary>The page's notice (saved, posted …), in a green box.</summary>
	public static View Notice() => Message(nameof(PageViewModel.Notice), Good, "#E7F6EF", "#12352A", "#7DD3B0");

	private static View Message(string property, Color text, string light, string dark, string darkText)
	{
		var label = new Label { FontSize = 14 };
		label.SetAppThemeColor(Label.TextColorProperty, text, Color.FromArgb(darkText));
		label.SetBinding(Label.TextProperty, property);
		var box = new Border { Content = label, Padding = new Thickness(14, 10), StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 12 } };
		box.SetAppThemeColor(VisualElement.BackgroundColorProperty, Color.FromArgb(light), Color.FromArgb(dark));
		box.SetBinding(VisualElement.IsVisibleProperty, property, converter: new NotEmpty());
		return box;
	}

	public static ActivityIndicator Busy()
	{
		var busy = new ActivityIndicator { Color = Accent, HeightRequest = 32 };
		busy.SetBinding(ActivityIndicator.IsRunningProperty, nameof(PageViewModel.IsBusy));
		busy.SetBinding(VisualElement.IsVisibleProperty, nameof(PageViewModel.IsBusy));
		return busy;
	}

	// ── containers ────────────────────────────────────────────────────────────────────────────

	public static Border Card(View content, double padding = 14)
	{
		var card = new Border
		{
			Content = content, Padding = padding, Margin = new Thickness(0, 4), StrokeThickness = 1,
			StrokeShape = new RoundRectangle { CornerRadius = 14 },
			Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.06f, Radius = 10, Offset = new Point(0, 3) }
		};
		card.SetAppThemeColor(VisualElement.BackgroundColorProperty, SurfaceLight, SurfaceDark);
		card.SetAppTheme<Brush>(Border.StrokeProperty, new SolidColorBrush(LineLight), new SolidColorBrush(LineDark));
		return card;
	}

	/// <summary>Navy-to-blue banner at the top of the main screens, with the Tabsan mark.</summary>
	public static Border Banner(View content)
	{
		var mark = new Image { Source = "tabsan_mark.png", HeightRequest = 54, Opacity = 0.9, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Start };
		var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 12 };
		grid.Add(content, 0);
		grid.Add(mark, 1);
		return new Border
		{
			Content = grid, Padding = new Thickness(18, 18, 18, 20), Margin = new Thickness(0, 4, 0, 6), StrokeThickness = 0,
			StrokeShape = new RoundRectangle { CornerRadius = 18 }, Background = BannerBrush(),
			Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.14f, Radius = 14, Offset = new Point(0, 5) }
		};
	}

	public static Brush BannerBrush() => new LinearGradientBrush(
		[new GradientStop(Brand, 0), new GradientStop(Color.FromArgb("#1D5C99"), 1)], new Point(0, 0), new Point(1, 1));

	/// <summary>A small rounded label (a state, a count, a date).</summary>
	public static Border Chip(string text, Color color, bool onBanner = false)
	{
		var label = new Label { Text = text, FontSize = 12, FontFamily = "OpenSansSemibold", TextColor = onBanner ? Colors.White : color };
		return new Border
		{
			Content = label, Padding = new Thickness(10, 4), StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 11 },
			BackgroundColor = onBanner ? Colors.White.WithAlpha(0.18f) : color.WithAlpha(0.13f),
			HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Center
		};
	}

	/// <summary>A figure with its caption, for the row of figures under the banner.</summary>
	public static Border Stat(string label, string value, Color color)
	{
		var number = new Label { Text = value, FontSize = 22, FontFamily = "OpenSansSemibold", TextColor = color, LineBreakMode = LineBreakMode.TailTruncation };
		var caption = Small(label);
		caption.FontSize = 12;
		caption.MaxLines = 2;
		return Card(new VerticalStackLayout { Spacing = 2, Children = { number, caption } }, padding: 12);
	}

	public static Grid Stats(params View[] tiles)
	{
		var grid = new Grid { ColumnSpacing = 10 };
		for (var i = 0; i < tiles.Length; i++)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			grid.Add(tiles[i], i);
		}
		return grid;
	}

	/// <summary>A round badge with initials (people, courses).</summary>
	public static Border Avatar(Label initials, Color color, double size = 42)
	{
		initials.FontFamily = "OpenSansSemibold";
		initials.FontSize = size * 0.36;
		initials.TextColor = color;
		initials.HorizontalOptions = initials.VerticalOptions = LayoutOptions.Center;
		return new Border
		{
			Content = initials, WidthRequest = size, HeightRequest = size, StrokeThickness = 0, VerticalOptions = LayoutOptions.Center,
			StrokeShape = new RoundRectangle { CornerRadius = size / 2 }, BackgroundColor = color.WithAlpha(0.14f)
		};
	}

	public static Border Avatar(string? text, Color color, double size = 42) => Avatar(new Label { Text = Initials(text) }, color, size);

	public static string Initials(string? text)
	{
		// "English (Class 9)" → "EN", "Computer Science" → "CS", "tmsgul.maths" → "TM": words that start with a letter, brackets left out.
		var words = System.Text.RegularExpressions.Regex.Replace(text ?? "", @"\(.*?\)", " ")
			.Split([' ', '.', '-', '_'], StringSplitOptions.RemoveEmptyEntries)
			.Where(w => char.IsLetter(w[0])).ToArray();
		return words.Length switch
		{
			0 => "?",
			1 => words[0][..Math.Min(2, words[0].Length)].ToUpperInvariant(),
			_ => string.Concat(words[0][0], words[1][0]).ToUpperInvariant()
		};
	}

	/// <summary>Leading view, then the main column, then an optional trailing view — the layout of most list rows.</summary>
	public static Grid Row(View? leading, View main, View? trailing = null)
	{
		var grid = new Grid { ColumnSpacing = 12 };
		foreach (var view in new[] { leading, main, trailing })
		{
			if (view is null) continue;
			grid.ColumnDefinitions.Add(new ColumnDefinition(view == main ? GridLength.Star : GridLength.Auto));
			grid.Add(view, grid.ColumnDefinitions.Count - 1);
		}
		return grid;
	}

	/// <summary>The "opens more" arrow at the end of a row (pointing the other way in Urdu and Arabic).</summary>
	public static Label Chevron()
	{
		var label = Small(AppText.IsRtl ? "‹" : "›");
		label.FontSize = 22;
		label.VerticalOptions = LayoutOptions.Center;
		return label;
	}

	public static VerticalStackLayout Lines(params View[] views)
	{
		var stack = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
		foreach (var v in views) stack.Add(v);
		return stack;
	}

	/// <summary>A class in the day: start and end time in a teal box, then the subject and where.</summary>
	public static Border ClassCard(Label start, Label end, Label subject, Label details)
	{
		start.FontFamily = "OpenSansSemibold";
		start.FontSize = 15;
		start.TextColor = Accent;
		end.FontSize = 12;
		start.HorizontalOptions = end.HorizontalOptions = LayoutOptions.Center;
		var time = new Border
		{
			Content = new VerticalStackLayout { Spacing = 0, Children = { start, end } }, Padding = new Thickness(10, 8), WidthRequest = 76,
			StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 10 }, BackgroundColor = Accent.WithAlpha(0.11f), VerticalOptions = LayoutOptions.Center
		};
		return Card(Row(time, Lines(subject, details)), padding: 10);
	}

	public static Border ClassCard(ClassSlot c) => ClassCard(new Label { Text = c.Start }, Small(c.End), Title(c.Subject), Small(c.Details));

	public static Border AnnouncementCard(AnnouncementItem a, bool withCourse)
	{
		var meta = withCourse && !string.IsNullOrWhiteSpace(a.CourseTitle) ? $"{a.CourseTitle} · {Date(a.PostedAt)}" : Date(a.PostedAt);
		var body = Body(a.Body);
		body.MaxLines = 5;
		body.LineBreakMode = LineBreakMode.TailTruncation;
		var bar = new BoxView { WidthRequest = 4, CornerRadius = 2, Color = Sky, BackgroundColor = Colors.Transparent, VerticalOptions = LayoutOptions.Fill };
		return Card(Row(bar, Lines(Title(a.Title), Small(meta), body)));
	}

	/// <summary>A quiet box for "nothing here".</summary>
	public static Border Empty(string text)
	{
		var label = Small(text);
		label.HorizontalTextAlignment = TextAlignment.Center;
		var box = new Border
		{
			Content = label, Padding = new Thickness(16, 18), Margin = new Thickness(0, 4), StrokeThickness = 1, StrokeDashArray = [4, 3],
			StrokeShape = new RoundRectangle { CornerRadius = 14 }, BackgroundColor = Colors.Transparent
		};
		box.SetAppTheme<Brush>(Border.StrokeProperty, new SolidColorBrush(Color.FromArgb("#CBD5E1")), new SolidColorBrush(LineDark));
		return box;
	}

	/// <summary>An input in a rounded frame (entries, editors, pickers).</summary>
	public static Border Field(View input)
	{
		var frame = new Border { Content = input, Padding = new Thickness(12, 0), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 12 } };
		frame.SetAppThemeColor(VisualElement.BackgroundColorProperty, SurfaceLight, SurfaceDark);
		frame.SetAppTheme<Brush>(Border.StrokeProperty, new SolidColorBrush(Color.FromArgb("#CBD5E1")), new SolidColorBrush(LineDark));
		return frame;
	}

	// ── buttons ───────────────────────────────────────────────────────────────────────────────

	public static Button Primary(string text) => new() { Text = text };

	public static Button Secondary(string text)
	{
		var button = new Button { Text = text, BorderWidth = 1 };
		button.SetAppThemeColor(VisualElement.BackgroundColorProperty, SurfaceLight, SurfaceDark);
		button.SetAppThemeColor(Button.TextColorProperty, Brand, InkDark);
		button.SetAppThemeColor(Button.BorderColorProperty, Color.FromArgb("#CBD5E1"), LineDark);
		return button;
	}

	public static Button Danger(string text)
	{
		var button = new Button { Text = text, BorderWidth = 1, BorderColor = Bad, TextColor = Bad };
		button.SetAppThemeColor(VisualElement.BackgroundColorProperty, SurfaceLight, SurfaceDark);
		return button;
	}

	// ── pages ─────────────────────────────────────────────────────────────────────────────────

	public static ContentPage Page(string title, PageViewModel vm, params View[] children)
	{
		var stack = new VerticalStackLayout { Padding = 16, Spacing = 10 };
		stack.Add(Busy());
		stack.Add(Status(vm));
		stack.Add(Notice());
		foreach (var c in children) stack.Add(c);
		return new ContentPage { Title = title, BindingContext = vm, Content = new ScrollView { Content = stack } };
	}

	/// <summary>Colour for a percentage: green from 75, amber from 50, red below.</summary>
	public static Color PercentColor(decimal? percent) => percent switch { null => Sky, >= 75 => Good, >= 50 => Warn, _ => Bad };

	public static string Date(DateTime? d) => d is { } v ? HomeViewModel.ShortDate(v, DateTime.UtcNow) : "";

	/// <summary>Today's date in the app's language ("Tuesday 27 October").</summary>
	public static string Today()
	{
		CultureInfo culture;
		try { culture = CultureInfo.GetCultureInfo(AppText.Language); }
		catch (CultureNotFoundException) { culture = CultureInfo.InvariantCulture; }
		return DateTime.Now.ToString("dddd d MMMM", culture);
	}

	public static string RoleName(string? role) => role?.ToLowerInvariant() switch
	{
		"student" => T("Student"),
		"faculty" => T("Teacher"),
		"parent" => T("Parent"),
		_ => role ?? ""
	};

	public static void ApplyDirection(Page page) => page.FlowDirection = AppText.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
}

internal sealed class BoldWhenTrue : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is true ? FontAttributes.Bold : FontAttributes.None;
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

internal sealed class NotEmpty : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is string s && s.Length > 0;
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Initials for a bound name or course code.</summary>
internal sealed class InitialsOf : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => Ui.Initials(value as string);
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>A bound percentage (0–100) as progress (0–1).</summary>
internal sealed class PercentToProgress : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> value is decimal d ? (double)Math.Clamp(d, 0, 100) / 100 : 0d;
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>A bound percentage's colour (see <see cref="Ui.PercentColor"/>).</summary>
internal sealed class PercentToColor : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => Ui.PercentColor(value as decimal?);
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>A bound UTC time as the short local date the app shows everywhere.</summary>
internal sealed class ShortLocalDate : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is DateTime d ? Ui.Date(d) : "";
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

internal sealed class Not : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
