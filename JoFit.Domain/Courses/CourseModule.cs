using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Courses;

/// <summary>One module of a course syllabus, in teaching order.</summary>
public class CourseModule
{
    public const int MaxPerCourse = 200;

    public long Id { get; set; }
    public long CourseId { get; set; }
    public int SortOrder { get; set; }
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string? SummaryEn { get; set; }
    public string? SummaryAr { get; set; }
    public int? DurationMinutes { get; set; }

    /// <summary>Checks a submitted module list; errors are keyed like modules[2].title.en.</summary>
    public static List<Error> Validate(IReadOnlyList<CourseModuleInput> modules)
    {
        var errors = new List<Error>();
        if (modules.Count > MaxPerCourse)
        {
            errors.Add(CourseErrors.Field("modules", $"A course can have at most {MaxPerCourse} modules."));
            return errors;
        }

        for (var i = 0; i < modules.Count; i++)
        {
            var m = modules[i];
            var key = $"modules[{i}]";
            if (m.SortOrder < 0) errors.Add(CourseErrors.Field($"{key}.sortOrder", "The sort order cannot be negative."));

            var titleEn = (m.TitleEn ?? string.Empty).Trim();
            var titleAr = (m.TitleAr ?? string.Empty).Trim();
            if (titleEn.Length == 0) errors.Add(CourseErrors.Field($"{key}.title.en", "The English module title is required."));
            else if (titleEn.Length > 200) errors.Add(CourseErrors.Field($"{key}.title.en", "A module title cannot exceed 200 characters."));
            if (titleAr.Length == 0) errors.Add(CourseErrors.Field($"{key}.title.ar", "The Arabic module title is required."));
            else if (titleAr.Length > 200) errors.Add(CourseErrors.Field($"{key}.title.ar", "A module title cannot exceed 200 characters."));

            if ((m.SummaryEn?.Trim().Length ?? 0) > 1000) errors.Add(CourseErrors.Field($"{key}.summary.en", "A module summary cannot exceed 1000 characters."));
            if ((m.SummaryAr?.Trim().Length ?? 0) > 1000) errors.Add(CourseErrors.Field($"{key}.summary.ar", "A module summary cannot exceed 1000 characters."));
            if (m.DurationMinutes is < 0 or > 100000) errors.Add(CourseErrors.Field($"{key}.durationMinutes", "Duration must be a whole number of minutes from 0 to 100000."));
        }

        return errors;
    }

    public static CourseModule From(long courseId, CourseModuleInput m) => new()
    {
        CourseId = courseId,
        SortOrder = m.SortOrder,
        TitleEn = m.TitleEn.Trim(),
        TitleAr = m.TitleAr.Trim(),
        SummaryEn = string.IsNullOrWhiteSpace(m.SummaryEn) ? null : m.SummaryEn.Trim(),
        SummaryAr = string.IsNullOrWhiteSpace(m.SummaryAr) ? null : m.SummaryAr.Trim(),
        DurationMinutes = m.DurationMinutes
    };
}

/// <summary>A module as the back-office form submits it.</summary>
public sealed record CourseModuleInput(
    int SortOrder,
    string TitleEn,
    string TitleAr,
    string? SummaryEn,
    string? SummaryAr,
    int? DurationMinutes);
