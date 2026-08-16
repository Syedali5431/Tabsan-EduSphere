namespace Tabsan.EduSphere.Web.Helpers;

public static class DepartmentDisplayHelper
{
    public static string FormatLabel(string name, int? institutionType) => institutionType switch
    {
        0 => $"{name} (University)",
        1 => $"{name} (School)",
        2 => $"{name} (College)",
        _ => name
    };
}
