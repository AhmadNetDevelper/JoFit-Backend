using JoFit.Domain.Courses;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Infrastructure.Data.Seeders;

/// <summary>Seeds the coach skills list (the registration form's "المهارة" picker, backed by Courses).</summary>
public class CourseSeeder(JoFitDbContext context)
{
    private static readonly (string NameAr, string NameEn)[] Courses =
    [
        ("الميكانيكا الحيوية", "Biomechanics"),
        ("فسيولوجيا التمارين", "Exercise Physiology"),
        ("القوة والتكييف", "Strength and Conditioning"),
        ("التغذية الرياضية", "Sports Nutrition"),
        ("الوقاية من الإصابات وإعادة التأهيل", "Injury Prevention and Rehabilitation"),
        ("تحليل الأداء", "Performance Analysis"),
        ("فحص الحركة الوظيفية", "Functional Movement Screening"),
        ("التخطيط التكتيكي", "Tactical Planning"),
        ("تدريب تقنيات رياضية متخصصة", "Specialized Sports Technique Coaching"),
        ("إدارة الفريق", "Team Management"),
        ("التحفيز وتطوير الرياضيين", "Athlete Motivation and Development"),
        ("تصميم البرامج التدريبية (فردية وجماعية)", "Training Program Design (Individual and Group)"),
        ("التواصل مع الرياضيين", "Communicating with Athletes"),
        ("تعليم أنماط الحركة", "Teaching Movement Patterns"),
        ("التكيف مع الفئات الخاصة", "Adapting for Special Populations"),
        ("استراتيجيات اكتساب المهارات", "Skill Acquisition Strategies"),
        ("تصميم الإحماء والتهدئة", "Warm-up and Cool-down Design"),
        ("إدارة السلوك (خاصة للأطفال والناشئين)", "Behavior Management (Especially for Children and Youth)"),
        ("تنمية الصلابة الذهنية", "Mental Toughness Development"),
        ("تقييم الإصابات", "Injury Assessment"),
        ("تقنيات التدليك الرياضي", "Sports Massage Techniques"),
        ("أساليب الاستشفاء (حمامات الثلج، الضغط، إلخ)", "Recovery Methods (Ice Baths, Compression, etc.)"),
        ("برامج المرونة والحركة", "Flexibility and Mobility Programs"),
        ("تصحيح الوضعية", "Posture Correction"),
        ("فهم الإحالة الطبية", "Understanding Medical Referral"),
        ("الإسعافات الأولية والإنعاش القلبي الرئوي", "First Aid and CPR"),
        ("تخطيط وتنظيم الفعاليات", "Event Planning and Organization"),
        ("المعرفة بعلم النفس الرياضي", "Knowledge of Sports Psychology"),
        ("التدريب قبل وبعد الولادة", "Prenatal and Postnatal Training"),
        ("جمع البيانات وتفسيرها", "Data Collection and Interpretation"),
        ("استخدام تقنيات التتبع وأجهزة القياس", "Using Tracking Technologies and Measurement Devices"),
        ("استخدام أدوات متابعة الأداء", "Using Performance Monitoring Tools"),
        ("إدارة الميزانية", "Budget Management"),
        ("استراتيجيات التسويق والترويج", "Marketing and Promotion Strategies"),
        ("إدارة وسائل التواصل الاجتماعي", "Social Media Management"),
        ("بناء العلامة التجارية والعلاقات العامة", "Branding and Public Relations"),
        ("إدارة المنشآت الرياضية", "Sports Facility Management"),
        ("الامتثال القانوني والأخلاقي", "Legal and Ethical Compliance"),
        ("إدارة المخاطر في البيئات الرياضية", "Risk Management in Sports Environments"),
        ("القيادة", "Leadership"),
        ("العمل الجماعي", "Teamwork"),
        ("الذكاء العاطفي", "Emotional Intelligence"),
        ("حل النزاعات", "Conflict Resolution"),
        ("إدارة الوقت", "Time Management"),
        ("الكفاءة الثقافية في الفرق المتنوعة", "Cultural Competence in Diverse Teams"),
    ];

    public async Task SeedAsync()
    {
        var existing = await context.Courses
            .Where(x => !x.IsDeleted)
            .Select(x => x.NameAr)
            .ToListAsync();

        var added = 0;
        foreach (var (nameAr, nameEn) in Courses)
        {
            if (existing.Contains(nameAr))
                continue;

            var result = Course.Create(nameAr, nameEn, isActive: true);
            if (result.IsError)
                throw new InvalidOperationException($"Failed to create Course '{nameEn}': {result.Errors}");

            context.Courses.Add(result.Value!);
            added++;
        }

        if (added == 0)
        {
            Console.WriteLine("⏭ Courses already seeded.");
            return;
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"  ✅ {added} Course rows seeded.");
    }
}
