using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace Tabsan.EduSphere.Mobile.Core;

/// <summary>
/// What the user is looking at, shared by the Timetable, Courses, Homework, Attendance and Results tabs: the child (parents),
/// the class (all classes, or one such as "Class 9"), and the institute and campus (teachers and admins with several).
/// Courses tell each offering's class and campus, so lists that carry an offering id are filtered with <see cref="Includes"/>.
/// </summary>
public sealed partial class Selection(EduSphereClient client) : ObservableObject
{
    private List<CourseSummary> _courses = [];
    private ChildSummary? _child;
    private string? _className;
    private ScopeItem? _scope;

    public string Role => client.Session?.Role ?? "";
    public bool IsParent => Role.Equals("Parent", StringComparison.OrdinalIgnoreCase);
    public bool IsTeacher => Role.Equals("Faculty", StringComparison.OrdinalIgnoreCase);
    public bool IsAdmin => Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) || Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);

    public ObservableCollection<ChildSummary> Children { get; } = [];
    /// <summary>Class names of the courses in view (and of results and attendance added with <see cref="AddClasses"/>), in class order.</summary>
    public ObservableCollection<string> Classes { get; } = [];
    public ObservableCollection<ScopeItem> Scopes { get; } = [];
    public IReadOnlyList<CourseSummary> Courses => _courses;

    /// <summary>Parents: the child in view.</summary>
    public ChildSummary? Child => _child;
    public Guid? ChildId => _child?.StudentProfileId;
    /// <summary>Null: all classes.</summary>
    public string? ClassName => _className;
    /// <summary>Null: all campuses.</summary>
    public ScopeItem? Scope => _scope;

    /// <summary>Raised after the child, class or campus changes (pages reload or filter).</summary>
    public event EventHandler? Changed;

    /// <summary>Loads children (parents), campuses (staff) and the courses that give the classes.</summary>
    public async Task LoadAsync()
    {
        if (IsParent)
        {
            var children = await client.GetChildrenAsync();
            Children.Clear();
            foreach (var c in children) Children.Add(c);
            _child = children.FirstOrDefault(c => c.StudentProfileId == _child?.StudentProfileId) ?? children.FirstOrDefault();
        }
        if (IsTeacher || IsAdmin)
        {
            var scopes = await client.GetScopesAsync();
            Scopes.Clear();
            foreach (var s in scopes) Scopes.Add(s);
            if (_scope is not null) _scope = scopes.FirstOrDefault(s => s.CampusId == _scope.CampusId);
        }
        await LoadCoursesAsync();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private async Task LoadCoursesAsync()
    {
        _courses = IsParent && _child is null ? [] : await client.GetCoursesAsync(IsParent ? ChildId : null);
        Classes.Clear();
        AddClasses(InScope(_courses).Select(c => c.SemesterName));
        if (_className is not null && !Classes.Contains(_className)) _className = null;
    }

    public async Task SetChildAsync(ChildSummary? child)
    {
        if (child?.StudentProfileId == _child?.StudentProfileId) return;
        _child = child;
        _className = null;
        await LoadCoursesAsync();
        OnPropertyChanged(nameof(Child));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetClass(string? className)
    {
        className = string.IsNullOrWhiteSpace(className) ? null : className;
        if (className == _className) return;
        _className = className;
        OnPropertyChanged(nameof(ClassName));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetScope(ScopeItem? scope)
    {
        if (scope?.CampusId == _scope?.CampusId) return;
        _scope = scope;
        Classes.Clear();
        AddClasses(InScope(_courses).Select(c => c.SemesterName));
        if (_className is not null && !Classes.Contains(_className)) _className = null;
        OnPropertyChanged(nameof(Scope));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Adds class names found in other data (results and attendance of earlier classes), keeping class order.</summary>
    public void AddClasses(IEnumerable<string?> names)
    {
        var all = Classes.Concat(names.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n!)).Distinct().OrderBy(n => n, ClassOrder.Instance).ToList();
        if (all.SequenceEqual(Classes)) return;
        Classes.Clear();
        foreach (var n in all) Classes.Add(n);
    }

    /// <summary>Whether something of this course is in view: its class matches (or all classes) and it is on the chosen campus.</summary>
    public bool Includes(Guid? offeringId, string? className = null)
    {
        var course = offeringId is { } id ? _courses.FirstOrDefault(c => c.OfferingId == id) : null;
        var name = className ?? course?.SemesterName;
        if (_className is not null && !string.Equals(name, _className, StringComparison.OrdinalIgnoreCase)) return false;
        return _scope is null || course is null || course.CampusId == _scope.CampusId;
    }

    private IEnumerable<CourseSummary> InScope(IEnumerable<CourseSummary> courses)
        => _scope is null ? courses : courses.Where(c => c.CampusId == _scope.CampusId);

    /// <summary>"Class 2" before "Class 10"; then by name.</summary>
    public sealed partial class ClassOrder : IComparer<string>
    {
        public static readonly ClassOrder Instance = new();

        public int Compare(string? x, string? y)
        {
            int Number(string? s) => s is not null && FirstNumber().Match(s) is { Success: true } m ? int.Parse(m.Value) : int.MaxValue;
            var byNumber = Number(x).CompareTo(Number(y));
            return byNumber != 0 ? byNumber : string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
        }

        [GeneratedRegex(@"\d+")]
        private static partial Regex FirstNumber();
    }
}
