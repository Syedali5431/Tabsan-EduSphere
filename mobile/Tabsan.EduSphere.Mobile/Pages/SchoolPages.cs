using System.Collections.Specialized;
using System.ComponentModel;
using Tabsan.EduSphere.Mobile.Core;

namespace Tabsan.EduSphere.Mobile.Pages;

// School news, events and forms: what the school sends families and students, replies to events, and forms to fill in
// (a parent once for each child); wellbeing: a student's check-in, and the notes and plans the school shared.

/// <summary>The School tab: News, Events, Forms and (students and parents) Wellbeing, one at a time, with how many are new or waiting.</summary>
public sealed class SchoolPage : ContentPage
{
	private readonly SchoolViewModel _vm;

	public SchoolPage(SchoolViewModel vm, EduSphereClient client)
	{
		_vm = vm;
		BindingContext = vm;
		Title = Ui.T("School");

		var newsButton = Ui.Secondary("");
		var eventsButton = Ui.Secondary("");
		var formsButton = Ui.Secondary("");
		var wellbeingButton = Ui.Secondary(Ui.T("Wellbeing"));
		wellbeingButton.AutomationId = "school-wellbeing";
		wellbeingButton.IsVisible = vm.ShowsWellbeing;
		wellbeingButton.Clicked += (_, _) => vm.Section = SchoolViewModel.WellbeingSection;
		newsButton.AutomationId = "school-news";
		eventsButton.AutomationId = "school-events";
		formsButton.AutomationId = "school-forms";
		newsButton.Clicked += (_, _) => vm.Section = SchoolViewModel.NewsSection;
		eventsButton.Clicked += (_, _) => vm.Section = SchoolViewModel.EventsSection;
		formsButton.Clicked += (_, _) => vm.Section = SchoolViewModel.FormsSection;
		var segments = new Grid
		{
			Padding = new Thickness(16, 10, 16, 4),
			ColumnSpacing = 8,
			ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }
		};
		segments.Add(newsButton, 0);
		segments.Add(eventsButton, 1);
		segments.Add(formsButton, 2);
		if (vm.ShowsWellbeing)
		{
			segments.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			segments.Add(wellbeingButton, 3);
		}

		var news = List(vm.News, Ui.T("NoNews"), NewsTemplate);
		news.SelectionChanged += async (_, e) =>
		{
			if (e.CurrentSelection.FirstOrDefault() is not NewsItem n) return;
			news.SelectedItem = null;
			vm.MarkRead(n);
			await Navigation.PushAsync(new NewsPostPage(new NewsPostViewModel(client), n.Id));
		};
		var events = List(vm.Events, Ui.T("NoEvents"), () => EventTemplate(client));
		var forms = List(vm.Forms, Ui.T("NoForms"), FormTemplate);
		forms.SelectionChanged += async (_, e) =>
		{
			if (e.CurrentSelection.FirstOrDefault() is not FormToFillItem f) return;
			forms.SelectedItem = null;
			await Navigation.PushAsync(new FormPage(new FormViewModel(client), f.FormId, f.StudentProfileId));
		};

		var wellbeing = List(vm.WellbeingNotes, Ui.T("NothingSharedYet"), WellbeingNoteTemplate);
		wellbeing.SelectionMode = SelectionMode.None;
		wellbeing.Header = WellbeingHeader();

		void Show()
		{
			wellbeing.IsVisible = vm.Section == SchoolViewModel.WellbeingSection;
			news.IsVisible = vm.Section == SchoolViewModel.NewsSection;
			events.IsVisible = vm.Section == SchoolViewModel.EventsSection;
			forms.IsVisible = vm.Section == SchoolViewModel.FormsSection;
			newsButton.Text = vm.NewNews > 0 ? $"{Ui.T("News")} ({vm.NewNews})" : Ui.T("News");
			eventsButton.Text = Ui.T("Events");
			formsButton.Text = vm.FormsWaiting > 0 ? $"{Ui.T("Forms")} ({vm.FormsWaiting})" : Ui.T("Forms");
			foreach (var (button, on) in new[] { (newsButton, news.IsVisible), (eventsButton, events.IsVisible), (formsButton, forms.IsVisible), (wellbeingButton, wellbeing.IsVisible) })
			{
				button.BackgroundColor = on ? Ui.Brand : Colors.Transparent;
				button.TextColor = on ? Colors.White : Ui.Brand;
			}
		}
		vm.PropertyChanged += (_, e) => { if (e.PropertyName is nameof(SchoolViewModel.Section) or nameof(SchoolViewModel.NewNews) or nameof(SchoolViewModel.FormsWaiting)) Show(); };
		Show();

		var lists = new Grid { Children = { news, events, forms, wellbeing } };
		var refresh = new RefreshView { Content = lists, Command = vm.LoadCommand };
		refresh.SetBinding(RefreshView.IsRefreshingProperty, nameof(PageViewModel.IsBusy));
		Content = Ui.ListLayout(vm, new VerticalStackLayout { Children = { segments, new ContentView { Padding = new Thickness(16, 0), Content = Ui.Notice() } } }, refresh);
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.LoadAsync();
	}

	private static CollectionView List(System.Collections.IEnumerable items, string empty, Func<View> template) => new()
	{
		ItemsSource = items,
		SelectionMode = SelectionMode.Single,
		Footer = new ContentView { HeightRequest = 16 },
		EmptyView = new ContentView { Padding = 16, Content = Ui.Empty(empty) },
		ItemTemplate = new DataTemplate(() => new ContentView { Padding = new Thickness(16, 6, 16, 0), Content = template() })
	};

	private static View NewsTemplate()
	{
		var title = Ui.Title();
		var summary = Ui.Small();
		summary.MaxLines = 3;
		summary.LineBreakMode = LineBreakMode.TailTruncation;
		var meta = Ui.Small();
		meta.FontSize = 12;
		var pinned = Ui.Chip(Ui.T("Pinned"), Ui.Sky);
		var fresh = Ui.Chip(Ui.T("New"), Ui.Accent);
		var card = Ui.Card(Ui.Row(null, Ui.Lines(title, summary, meta), new VerticalStackLayout { Spacing = 4, Children = { pinned, fresh } }), padding: 12);
		card.BindingContextChanged += (_, _) =>
		{
			if (card.BindingContext is not NewsItem n) return;
			title.Text = n.Title;
			title.FontFamily = n.IsNew ? "OpenSansSemibold" : "OpenSansRegular";
			summary.Text = n.Summary;
			summary.IsVisible = !string.IsNullOrWhiteSpace(n.Summary);
			meta.Text = $"{n.AuthorName} · {Ui.Date(n.PublishAt)}";
			pinned.IsVisible = n.IsPinned;
			fresh.IsVisible = n.IsNew;
		};
		return card;
	}

	private View EventTemplate(EduSphereClient client)
	{
		var title = Ui.Title();
		var when = Ui.Small();
		var description = Ui.Small();
		description.MaxLines = 4;
		description.LineBreakMode = LineBreakMode.TailTruncation;
		var cancelled = Ui.Chip(Ui.T("Cancelled"), Ui.Bad);
		var counts = Ui.Small();
		counts.FontSize = 12;
		var mine = Ui.Small();
		mine.FontFamily = "OpenSansSemibold";
		var closed = Ui.Small(Ui.T("RepliesClosed"));
		var guests = new Picker { Title = Ui.T("Guests"), WidthRequest = 90 };
		var going = Ui.Primary(Ui.T("Going"));
		var maybe = Ui.Secondary(Ui.T("Maybe"));
		var notGoing = Ui.Secondary(Ui.T("NotGoing"));
		var buttons = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, Children = { going, maybe, notGoing, guests } };
		foreach (var b in new View[] { going, maybe, notGoing, guests }) b.Margin = new Thickness(0, 4, 8, 0);
		var form = Ui.Secondary(Ui.T("FillInForm"));
		var card = Ui.Card(new VerticalStackLayout { Spacing = 4, Children = { Ui.Row(null, title, cancelled), when, description, counts, mine, closed, buttons, form } }, padding: 12);
		SchoolEventItem? current = null;
		async Task Reply(string answer)
		{
			if (current is null) return;
			await _vm.ReplyAsync(current, answer, guests.SelectedIndex < 0 ? 0 : guests.SelectedIndex);
		}
		going.Clicked += async (_, _) => await Reply("Going");
		maybe.Clicked += async (_, _) => await Reply("Maybe");
		notGoing.Clicked += async (_, _) => await Reply("NotGoing");
		form.Clicked += async (_, _) =>
		{
			if (current?.FormId is { } id) await Navigation.PushAsync(new FormPage(new FormViewModel(client), id, null));
		};
		card.BindingContextChanged += (_, _) =>
		{
			if (card.BindingContext is not SchoolEventItem e) return;
			current = e;
			title.Text = e.Title;
			when.Text = e.When;
			description.Text = e.IsCancelled ? e.CancelReason : e.Description;
			description.IsVisible = !string.IsNullOrWhiteSpace(description.Text);
			cancelled.IsVisible = e.IsCancelled;
			counts.Text = e.CountsText;
			counts.IsVisible = e.RepliesOpen && !e.IsCancelled && e.CountsText is not null;
			mine.Text = e.MyReply;
			mine.IsVisible = e.MyReply is not null;
			closed.IsVisible = e.RepliesOpen && !e.CanReply && !e.IsCancelled;
			buttons.IsVisible = e.CanReply;
			guests.IsVisible = e.CanReply && e.MaxGuests > 0;
			guests.ItemsSource = Enumerable.Range(0, e.MaxGuests + 1).Select(n => n.ToString()).ToList();
			guests.SelectedIndex = Math.Min(e.MyGuests, e.MaxGuests);
			form.IsVisible = e.FormId is not null && !e.IsCancelled;
		};
		return card;
	}

	/// <summary>Above the shared notes: a student's check-in (when the school has switched it on) and the support plans.</summary>
	private View WellbeingHeader()
	{
		var header = new VerticalStackLayout { Spacing = 8, Padding = new Thickness(16, 6, 16, 4) };

		// The check-in: five moods, an optional note, asking to talk.
		int? mood = null;
		var moods = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
		var moodButtons = new List<Button>();
		var note = new Editor { Placeholder = Ui.T("CheckInNote"), AutoSize = EditorAutoSizeOption.TextChanges, MaxLength = 500, MinimumHeightRequest = 60 };
		var talk = new CheckBox { Color = Ui.Brand };
		var talkLabel = Ui.Body(Ui.T("WantToTalk"));
		talkLabel.VerticalOptions = LayoutOptions.Center;
		var send = Ui.Primary(Ui.T("CheckIn"));
		send.AutomationId = "wellbeing-check-in";
		send.IsEnabled = false;
		for (var m = 1; m <= 5; m++)
		{
			var value = m;
			var button = Ui.Secondary($"{Face(value)} {SchoolViewModel.MoodWord(value)}");
			button.AutomationId = $"wellbeing-mood-{value}";
			button.Margin = new Thickness(0, 4, 8, 0);
			button.Clicked += (_, _) =>
			{
				mood = value;
				send.IsEnabled = true;
				for (var i = 0; i < moodButtons.Count; i++)
				{
					moodButtons[i].BackgroundColor = i + 1 == value ? Ui.Brand : Colors.Transparent;
					moodButtons[i].TextColor = i + 1 == value ? Colors.White : Ui.Brand;
				}
			};
			moodButtons.Add(button);
			moods.Children.Add(button);
		}
		send.Clicked += async (_, _) =>
		{
			if (mood is not { } chosen) return;
			if (await _vm.CheckInAsync(chosen, note.Text, talk.IsChecked))
			{
				note.Text = "";
				talk.IsChecked = false;
			}
		};
		var last = Ui.Small();
		last.SetBinding(Label.TextProperty, nameof(SchoolViewModel.LatestCheckInText));
		var checkIn = Ui.Card(new VerticalStackLayout
		{
			Spacing = 6,
			Children = { Ui.Title(Ui.T("HowAreYouFeeling")), last, moods, Ui.Field(note), new HorizontalStackLayout { Spacing = 4, Children = { talk, talkLabel } }, send, Ui.Small(Ui.T("CheckInPrivacy")) }
		}, padding: 12);
		header.Add(checkIn);

		var plans = new VerticalStackLayout { Spacing = 6 };
		header.Add(plans);
		header.Add(Ui.Title(Ui.T("FromSchool")));

		void Refresh()
		{
			checkIn.IsVisible = _vm.IsStudent && _vm.CheckInsEnabled;
			last.IsVisible = _vm.LatestCheckIn is not null;
			plans.Clear();
			if (_vm.WellbeingPlans.Count == 0) return;
			plans.Add(Ui.Title(Ui.T("SupportPlans")));
			foreach (var p in _vm.WellbeingPlans)
				plans.Add(Ui.Card(Ui.Lines(Ui.Title(p.Title), Ui.Body(p.Strategies), Ui.Small(p.OwnerName)), padding: 12));
		}
		_vm.PropertyChanged += (_, e) => { if (e.PropertyName is nameof(SchoolViewModel.CheckInsEnabled) or nameof(SchoolViewModel.LatestCheckIn)) Refresh(); };
		_vm.WellbeingPlans.CollectionChanged += (_, _) => Dispatcher.Dispatch(Refresh);
		Refresh();
		return header;
	}

	private static string Face(int mood) => mood switch { 1 => "😢", 2 => "🙁", 3 => "😐", 4 => "🙂", _ => "😄" };

	private static View WellbeingNoteTemplate()
	{
		var heading = Ui.Title();
		var summary = Ui.Body();
		var details = Ui.Small();
		var meta = Ui.Small();
		meta.FontSize = 12;
		var chip = Ui.Chip("", Ui.Good);
		var chipText = (Label)chip.Content!;
		var card = Ui.Card(Ui.Row(null, Ui.Lines(heading, summary, details, meta), chip), padding: 12);
		card.BindingContextChanged += (_, _) =>
		{
			if (card.BindingContext is not WellbeingNoteItem n) return;
			heading.Text = n.Heading;
			summary.Text = n.Summary;
			summary.IsVisible = !string.IsNullOrWhiteSpace(n.Category);
			details.Text = n.Details;
			details.IsVisible = !string.IsNullOrWhiteSpace(n.Details);
			meta.Text = n.Meta;
			chip.IsVisible = n.Tone is "Positive" or "Concern";
			chipText.Text = n.IsPraise ? Ui.T("Praise") : Ui.T("Concern");
			chip.BackgroundColor = (n.IsPraise ? Ui.Good : Ui.Warn).WithAlpha(0.15f);
		};
		return card;
	}

	private static View FormTemplate()
	{
		var title = Ui.Title();
		var who = Ui.Small();
		var due = Ui.Small();
		due.FontSize = 12;
		var chip = Ui.Chip("", Ui.Warn);
		var state = (Label)chip.Content!;
		var card = Ui.Card(Ui.Row(null, Ui.Lines(title, who, due), chip), padding: 12);
		card.BindingContextChanged += (_, _) =>
		{
			if (card.BindingContext is not FormToFillItem f) return;
			title.Text = f.Title;
			who.Text = f.For;
			who.IsVisible = f.For is not null;
			due.Text = f.DueAt is { } d ? $"{Ui.T("Due")} {Ui.Date(d)}" : null;
			due.IsVisible = f.DueAt is not null;
			state.Text = f.State;
			chip.BackgroundColor = (f.Answered ? Ui.Good : f.Open ? Ui.Warn : Ui.Sky).WithAlpha(0.15f);
		};
		return card;
	}
}

/// <summary>A news post: title, who and when, the picture, and its paragraphs.</summary>
public sealed class NewsPostPage : ContentPage
{
	private readonly NewsPostViewModel _vm;
	private readonly Guid _id;
	private readonly VerticalStackLayout _body = new() { Spacing = 10 };

	public NewsPostPage(NewsPostViewModel vm, Guid id)
	{
		_vm = vm;
		_id = id;
		BindingContext = vm;
		Title = Ui.T("News");
		var page = Ui.Page(Ui.T("News"), vm, _body);
		Content = page.Content;
		vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(NewsPostViewModel.Post)) Render(); };
		Ui.ApplyDirection(this);
	}

	private void Render()
	{
		_body.Clear();
		if (_vm.Post is not { } p) return;
		_body.Add(Ui.Title(p.Title, size: 20));
		_body.Add(Ui.Small($"{p.AuthorName} · {Ui.Date(p.PublishAt)}"));
		if (Uri.TryCreate(p.ImageUrl, UriKind.Absolute, out var image) && image.Scheme == Uri.UriSchemeHttps)
			_body.Add(new Image { Source = ImageSource.FromUri(image), Aspect = Aspect.AspectFill, HeightRequest = 200 });
		if (!string.IsNullOrWhiteSpace(p.Summary)) _body.Add(Ui.Body(p.Summary));
		foreach (var paragraph in _vm.Paragraphs) _body.Add(Ui.Body(paragraph));
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (_vm.Post is null) await _vm.LoadAsync(_id);
	}
}

/// <summary>Filling in a form: each question as text, a date, a number or ticks; the answers already given; Send.</summary>
public sealed class FormPage : ContentPage
{
	private readonly FormViewModel _vm;
	private readonly Guid _id;
	private readonly Guid? _child;
	private readonly VerticalStackLayout _questions = new() { Spacing = 14 };
	private readonly Label _title = Ui.Title(size: 20);
	private readonly Label _subtitle = Ui.Small();
	private readonly Label _description = Ui.Body();

	public FormPage(FormViewModel vm, Guid id, Guid? studentProfileId)
	{
		_vm = vm;
		_id = id;
		_child = studentProfileId;
		BindingContext = vm;
		Title = Ui.T("Forms");
		var send = Ui.Primary(Ui.T("SendAnswers"));
		send.AutomationId = "form-send";
		send.Command = vm.SendCommand;
		send.SetBinding(IsVisibleProperty, nameof(FormViewModel.CanSend));
		var page = Ui.Page(Ui.T("Forms"), vm, _title, _subtitle, _description, _questions, send);
		Content = page.Content;
		vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(FormViewModel.Form)) Header(); };
		vm.Questions.CollectionChanged += (_, e) => { if (e.Action is NotifyCollectionChangedAction.Reset or NotifyCollectionChangedAction.Add) Dispatcher.Dispatch(Render); };
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (_vm.Form is null) await _vm.LoadAsync(_id, _child);
	}

	private void Header()
	{
		_title.Text = _vm.Form?.Title;
		_subtitle.Text = _vm.Subtitle;
		_description.Text = _vm.Form?.Description;
		_description.IsVisible = !string.IsNullOrWhiteSpace(_vm.Form?.Description);
	}

	private void Render()
	{
		_questions.Clear();
		var locked = !_vm.CanSend;
		foreach (var q in _vm.Questions)
		{
			if (q.IsInformation)
			{
				_questions.Add(Ui.Card(Ui.Lines(Ui.Title(q.Field.Label), Ui.Small(q.Field.Help)), padding: 10));
				continue;
			}
			var box = new VerticalStackLayout { Spacing = 4, Children = { Ui.Title(q.Label) } };
			if (q.IsChoice)
			{
				foreach (var choice in q.Choices)
				{
					var tick = new CheckBox { IsChecked = choice.IsChecked, IsEnabled = !locked, Color = Ui.Brand };
					tick.CheckedChanged += (_, e) => q.Pick(choice, e.Value);
					choice.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(FormChoice.IsChecked) && tick.IsChecked != choice.IsChecked) tick.IsChecked = choice.IsChecked; };
					var label = Ui.Body(choice.Label);
					label.VerticalOptions = LayoutOptions.Center;
					box.Add(new HorizontalStackLayout { Spacing = 4, Children = { tick, label } });
				}
			}
			else
			{
				View input = q.Field.Type switch
				{
					"LongText" => new Editor { AutoSize = EditorAutoSizeOption.TextChanges, MinimumHeightRequest = 90 },
					"Date" => DateInput(q, locked),
					"Number" => new Entry { Keyboard = Keyboard.Numeric },
					_ => new Entry()
				};
				if (input is InputView text)
				{
					text.BindingContext = q;
					text.SetBinding(InputView.TextProperty, nameof(FormAnswerInput.Value), BindingMode.TwoWay);
					text.IsReadOnly = locked;
				}
				box.Add(Ui.Field(input));
				if (q.Field.Type == "Signature") box.Add(Ui.Small(Ui.T("TypeFullName")));
			}
			if (!string.IsNullOrWhiteSpace(q.Field.Help)) box.Add(Ui.Small(q.Field.Help));
			_questions.Add(box);
		}
	}

	private static View DateInput(FormAnswerInput q, bool locked)
	{
		var picker = new DatePicker { Format = "d MMM yyyy", IsEnabled = !locked };
		if (DateTime.TryParseExact(q.Value, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var d)) picker.Date = d;
		picker.DateSelected += (_, e) => q.Value = e.NewDate.ToString("yyyy-MM-dd");
		return picker;
	}
}
