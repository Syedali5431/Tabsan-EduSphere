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

	/// <summary>The top of the sign-in and server pages: the Tabsan wordmark, the given line and what the app is for.</summary>
	public static Border Header(View title)
	{
		var tagline = OnBanner(T("AppTagline"), 14, opacity: 0.85);
		tagline.HorizontalTextAlignment = TextAlignment.Center;
		return new Border
		{
			StrokeThickness = 0, Background = BannerBrush(), Padding = new Thickness(24, 56, 24, 76),
			StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(0, 0, 32, 32) },
			Content = new VerticalStackLayout
			{
				Spacing = 10,
				Children = { new Image { Source = "tabsan_wordmark.png", HeightRequest = 60, Aspect = Aspect.AspectFit, Margin = new Thickness(0, 0, 0, 10) }, title, tagline }
			}
		};
	}

	/// <summary>"Powered by Tabsan EduSphere · version" under the sign-in and server pages.</summary>
	public static HorizontalStackLayout Footer(string version) => new()
	{
		Spacing = 8, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 22, 0, 28),
		Children = { new Image { Source = "tabsan_mark.png", HeightRequest = 20 }, Small($"{T("PoweredBy")} · {version}") }
	};

	/// <summary>
	/// "Server: edu.myschool.com", a warning when the connection is plain http, and a "Change server" link when
	/// <paramref name="change"/> is given (not on a build locked to its institution's server).
	/// </summary>
	public static View Server(string? server, Action? change)
	{
		var stack = new VerticalStackLayout { Spacing = 2 };
		var line = Small($"{T("Server")}: {ServerAddress.Display(server)}");
		line.HorizontalTextAlignment = TextAlignment.Center;
		stack.Add(line);
		if (ServerAddress.IsUnencrypted(server))
		{
			var warning = Small(T("NotEncrypted"));
			warning.TextColor = Warn;
			warning.HorizontalTextAlignment = TextAlignment.Center;
			stack.Add(warning);
		}
		if (change is not null)
		{
			var link = new Label
			{
				Text = T("ChangeServer"), FontSize = 14, FontFamily = "OpenSansSemibold", TextColor = Accent, Padding = new Thickness(8, 6),
				HorizontalOptions = LayoutOptions.Center, AutomationId = "changeserver"
			};
			link.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(change) });
			stack.Add(link);
		}
		return stack;
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

	/// <summary>The "nothing here" box of a list that <paramref name="vm"/> loads. It is hidden while the page shows an error:
	/// a load that failed (no connection, say) must not also claim there is nothing.</summary>
	public static Border Empty(string text, PageViewModel vm)
	{
		var box = Empty(text);
		box.SetBinding(VisualElement.IsVisibleProperty, new Binding(nameof(PageViewModel.Error), source: vm, converter: new IsBlank()));
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

	// ── child, class and campus pickers ───────────────────────────────────────────────────────

	/// <summary>
	/// The pickers at the top of the Timetable, Courses, Homework, Attendance, Results and (staff) Home tabs: child (parents), class (all classes or one) and institute and campus (teachers and admins with several). Hidden when there is
	/// no class or child yet. All tabs share the same <see cref="Selection"/>, so a class chosen once applies everywhere.
	/// </summary>
	/// <summary>
	/// A list page: the error line, the child/class/campus lists, then the list. The lists sit above the CollectionView, not in
	/// its Header, so they stay in view while the list scrolls or shows its EmptyView.
	/// </summary>
	public static Grid ListLayout(PageViewModel vm, View bar, View list)
	{
		var grid = new Grid { RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) }, Children = { Status(vm), bar, list } };
		Grid.SetRow(bar, 1);
		Grid.SetRow(list, 2);
		return grid;
	}

	public static View ScopeBar(Selection sel)
	{
		var child = new Picker { Title = T("Child") };
		var cls = new Picker { Title = T("Class") };
		var campus = new Picker { Title = T("Campus") };
		// Each picker gets its box once: putting a picker into a new box while the old one still holds it crashes Android.
		var childBox = Labelled(T("Child"), child);
		var clsBox = Labelled(T("Class"), cls);
		var top = new Grid { ColumnSpacing = 10, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) } };
		top.Add(childBox, 0);
		top.Add(clsBox, 1);
		var campusBox = Labelled(T("Campus"), campus);
		var stack = new VerticalStackLayout { Spacing = 8, Children = { top, campusBox } };
		var card = Card(stack, padding: 12);
		card.Margin = new Thickness(16, 10, 16, 2);
		var updating = false;

		void Refresh()
		{
			updating = true;
			child.ItemsSource = sel.Children.Select(c => c.Name).ToList();
			child.SelectedIndex = sel.Child is { } c ? sel.Children.ToList().FindIndex(x => x.StudentProfileId == c.StudentProfileId) : -1;
			cls.ItemsSource = new[] { T("AllClasses") }.Concat(sel.Classes).ToList();
			cls.SelectedIndex = sel.ClassName is null ? 0 : 1 + sel.Classes.IndexOf(sel.ClassName);
			campus.ItemsSource = new[] { T("AllCampuses") }.Concat(sel.Scopes.Select(s => s.Label)).ToList();
			campus.SelectedIndex = sel.Scope is null ? 0 : 1 + sel.Scopes.ToList().FindIndex(s => s.CampusId == sel.Scope.CampusId);
			var showChild = sel.IsParent && sel.Children.Count > 0;
			var showCls = sel.Classes.Count > 0;
			childBox.IsVisible = showChild;
			clsBox.IsVisible = showCls;
			Grid.SetColumnSpan(childBox, showCls ? 1 : 2);
			Grid.SetColumn(clsBox, showChild ? 1 : 0);
			Grid.SetColumnSpan(clsBox, showChild ? 1 : 2);
			top.IsVisible = showChild || showCls;
			campusBox.IsVisible = sel.Scopes.Count > 1;
			card.IsVisible = top.IsVisible || campusBox.IsVisible;
			updating = false;
		}

		child.SelectedIndexChanged += async (_, _) =>
		{
			if (!updating && child.SelectedIndex >= 0 && child.SelectedIndex < sel.Children.Count) await sel.SetChildAsync(sel.Children[child.SelectedIndex]);
		};
		cls.SelectedIndexChanged += (_, _) =>
		{
			if (!updating && cls.SelectedIndex >= 0) sel.SetClass(cls.SelectedIndex == 0 ? null : sel.Classes[cls.SelectedIndex - 1]);
		};
		campus.SelectedIndexChanged += (_, _) =>
		{
			if (!updating && campus.SelectedIndex >= 0) sel.SetScope(campus.SelectedIndex == 0 ? null : sel.Scopes[campus.SelectedIndex - 1]);
		};
		sel.Children.CollectionChanged += (_, _) => Refresh();
		sel.Classes.CollectionChanged += (_, _) => Refresh();
		sel.Scopes.CollectionChanged += (_, _) => Refresh();
		sel.Changed += (_, _) => Refresh();
		Refresh();
		return card;
	}

	private static VerticalStackLayout Labelled(string caption, View input)
	{
		var label = Small(caption);
		label.FontSize = 12;
		return new VerticalStackLayout { Spacing = 4, Children = { label, Field(input) } };
	}

	/// <summary>Colour of an attendance status (Present, Absent, Late, Excused).</summary>
	public static Color StatusColor(string? status) => status switch { "Absent" => Bad, "Late" => Warn, "Excused" => Sky, _ => Good };

	/// <summary>An attendance status in the app's words.</summary>
	public static string StatusText(string? status) => status is "Absent" or "Late" or "Excused" ? T(status) : T("Present");

	// ── pages ─────────────────────────────────────────────────────────────────────────────────

	public static ContentPage Page(string title, PageViewModel vm, params View[] children)
	{
		var stack = new VerticalStackLayout { Padding = 16, Spacing = 10 };
		foreach (var c in children) stack.Add(c);
		// The busy sign and the result of an action ("Attendance saved.", "Grade saved.", or the error) stay above the
		// scrolling content. The buttons that cause them are often at the bottom of a long list (a class register, the
		// submissions to grade), and a message at the top of the scrolled content was out of sight.
		var messages = new VerticalStackLayout { Padding = new Thickness(16, 8, 16, 0), Spacing = 6, Children = { Busy(), Status(vm), Notice() } };
		var grid = new Grid { RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) } };
		grid.Add(messages, 0, 0);
		grid.Add(new ScrollView { Content = stack }, 0, 1);
		return new ContentPage { Title = title, BindingContext = vm, Content = grid };
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

/// <summary>True for no text (null or empty).</summary>
internal sealed class IsBlank : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not string s || s.Length == 0;
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
