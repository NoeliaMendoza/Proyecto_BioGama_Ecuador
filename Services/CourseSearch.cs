using BioGamaEcuador.Models.Sales;
using Microsoft.EntityFrameworkCore;

namespace BioGamaEcuador.Services;

public static class CourseSearch
{
    public static string ContainsPattern(string value) => "%" + value.Trim()
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal) + "%";

    public static IQueryable<Course> Filter(IQueryable<Course> courses, string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return courses;
        var pattern = ContainsPattern(text);
        return courses.Where(course =>
            EF.Functions.ILike(course.Title, pattern, "\\") ||
            EF.Functions.ILike(course.Description, pattern, "\\") ||
            EF.Functions.ILike(course.Instructor, pattern, "\\") ||
            EF.Functions.ILike(course.Syllabus, pattern, "\\"));
    }
}
