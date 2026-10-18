using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Quizzes;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

// Query-filter note (Plan M2): questions may belong to a quiz OR to a question bank. Filters that
// used to be "q.Quiz.IsActive" are now "q.QuizId == null || q.Quiz.IsActive" so bank questions
// (and answers / options / attempt items that reference them) are not hidden.

/// <summary>EF Core configuration for the <see cref="Quiz"/> entity.</summary>
internal sealed class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{
    public void Configure(EntityTypeBuilder<Quiz> builder)
    {
        builder.ToTable("quizzes");

        builder.HasKey(q => q.Id);
        builder.Property(q => q.RowVersion).IsRowVersion();

        builder.Property(q => q.Title).HasMaxLength(300).IsRequired();
        builder.Property(q => q.Instructions).HasMaxLength(4000);

        // Plan M2 delivery settings
        builder.Property(q => q.AccessCode).HasMaxLength(100);
        builder.Property(q => q.AllowedIpRanges).HasMaxLength(1000);
        // Existing quizzes get ScoreOnly via the migration's column default; no model default (EF sentinel rules).
        builder.Property(q => q.ReviewVisibility).HasConversion<string>().HasMaxLength(30);

        // Store IsActive; filter on active quizzes by default
        builder.HasQueryFilter(q => q.IsActive);

        builder.HasIndex(q => q.CourseOfferingId);
        builder.HasIndex(q => new { q.CourseOfferingId, q.IsPublished });
    }
}

/// <summary>EF Core configuration for the <see cref="QuizQuestion"/> entity.</summary>
internal sealed class QuizQuestionConfiguration : IEntityTypeConfiguration<QuizQuestion>
{
    public void Configure(EntityTypeBuilder<QuizQuestion> builder)
    {
        builder.ToTable("quiz_questions", t => t.HasCheckConstraint(
            "CK_quiz_questions_owner",
            "([QuizId] IS NOT NULL AND [QuestionBankId] IS NULL) OR ([QuizId] IS NULL AND [QuestionBankId] IS NOT NULL)"));

        builder.HasKey(q => q.Id);
        builder.Property(q => q.RowVersion).IsRowVersion();

        builder.Property(q => q.Text).HasMaxLength(2000).IsRequired();
        builder.Property(q => q.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(q => q.Marks).HasColumnType("decimal(8,2)");
        builder.Property(q => q.ConfigJson).HasColumnType("nvarchar(max)");
        builder.Property(q => q.Tags).HasMaxLength(500);
        builder.Ignore(q => q.IsBankQuestion);

        builder.HasOne(q => q.Quiz)
             .WithMany(q => q.Questions)
               .HasForeignKey(q => q.QuizId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(q => q.QuestionBank)
               .WithMany(b => b.Questions)
               .HasForeignKey(q => q.QuestionBankId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(q => q.QuizId == null || q.Quiz!.IsActive);

        builder.HasIndex(q => new { q.QuizId, q.OrderIndex });
        builder.HasIndex(q => new { q.QuestionBankId, q.IsArchived });
    }
}

/// <summary>EF Core configuration for the <see cref="QuizOption"/> entity.</summary>
internal sealed class QuizOptionConfiguration : IEntityTypeConfiguration<QuizOption>
{
    public void Configure(EntityTypeBuilder<QuizOption> builder)
    {
        builder.ToTable("quiz_options");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.RowVersion).IsRowVersion();

        builder.Property(o => o.Text).HasMaxLength(1000).IsRequired();

        builder.HasOne(o => o.Question)
               .WithMany(q => q.Options)
               .HasForeignKey(o => o.QuizQuestionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(o => o.Question.QuizId == null || o.Question.Quiz!.IsActive);

        builder.HasIndex(o => new { o.QuizQuestionId, o.OrderIndex });
    }
}

/// <summary>EF Core configuration for the <see cref="QuizAttempt"/> entity.</summary>
internal sealed class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public void Configure(EntityTypeBuilder<QuizAttempt> builder)
    {
        builder.ToTable("quiz_attempts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.RowVersion).IsRowVersion();

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.TotalScore).HasColumnType("decimal(10,2)");
        builder.Property(a => a.StartedFromIp).HasMaxLength(64);

        builder.HasOne(a => a.Quiz)
               .WithMany()
               .HasForeignKey(a => a.QuizId)
               .OnDelete(DeleteBehavior.Restrict);

        // One in-progress attempt per student per quiz
        builder.HasIndex(a => new { a.QuizId, a.StudentProfileId, a.Status });
        builder.HasIndex(a => a.StudentProfileId);

         // Stage 5.2 — analytics aggregates frequently group by quiz and completion status.
         builder.HasIndex(a => new { a.QuizId, a.Status })
             .HasDatabaseName("IX_quiz_attempts_quiz_status");

         // Final-Touches Phase 29 Stage 29.1 — support per-student and per-quiz attempt history ordered by recency.
         builder.HasIndex(a => new { a.StudentProfileId, a.StartedAt })
             .HasDatabaseName("IX_quiz_attempts_student_started_at");

         builder.HasIndex(a => new { a.QuizId, a.StudentProfileId, a.StartedAt })
             .HasDatabaseName("IX_quiz_attempts_quiz_student_started_at");

        // Match principal Quiz active filter to avoid required-relationship filter warnings.
        builder.HasQueryFilter(a => a.Quiz.IsActive);
    }
}

/// <summary>Plan M2.4: per-attempt question snapshot.</summary>
internal sealed class QuizAttemptItemConfiguration : IEntityTypeConfiguration<QuizAttemptItem>
{
    public void Configure(EntityTypeBuilder<QuizAttemptItem> builder)
    {
        builder.ToTable("quiz_attempt_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.RowVersion).IsRowVersion();
        builder.Property(i => i.Marks).HasColumnType("decimal(8,2)");
        builder.Property(i => i.PresentationJson).HasColumnType("nvarchar(max)");
        builder.Property(i => i.GeneratedValuesJson).HasMaxLength(2000);

        builder.HasOne(i => i.Attempt)
               .WithMany(a => a.Items)
               .HasForeignKey(i => i.QuizAttemptId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Question)
               .WithMany()
               .HasForeignKey(i => i.QuizQuestionId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(i => i.Attempt.Quiz.IsActive);

        builder.HasIndex(i => new { i.QuizAttemptId, i.QuizQuestionId }).IsUnique();
        builder.HasIndex(i => i.QuizQuestionId);
    }
}

/// <summary>EF Core configuration for the <see cref="QuizAnswer"/> entity.</summary>
internal sealed class QuizAnswerConfiguration : IEntityTypeConfiguration<QuizAnswer>
{
    public void Configure(EntityTypeBuilder<QuizAnswer> builder)
    {
        builder.ToTable("quiz_answers");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.RowVersion).IsRowVersion();

        builder.Property(a => a.TextResponse).HasColumnType("nvarchar(max)"); // Plan M2: essays
        builder.Property(a => a.ResponseJson).HasColumnType("nvarchar(max)");
        builder.Property(a => a.Feedback).HasMaxLength(4000);
        builder.Property(a => a.MarksAwarded).HasColumnType("decimal(8,2)");
        builder.Ignore(a => a.SelectedOptionIds);

        builder.HasOne(a => a.Attempt)
               .WithMany(at => at.Answers)
               .HasForeignKey(a => a.QuizAttemptId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Question)
               .WithMany()
               .HasForeignKey(a => a.QuizQuestionId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Selections)
               .WithOne()
               .HasForeignKey(s => s.QuizAnswerId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(a => a.Selections).UsePropertyAccessMode(PropertyAccessMode.Field).HasField("_selections");

         // Match principal filters (QuizAttempt + QuizQuestion) to avoid required-relationship filter warnings.
         builder.HasQueryFilter(a => a.Attempt.Quiz.IsActive);

        // One answer per question per attempt
        builder.HasIndex(a => new { a.QuizAttemptId, a.QuizQuestionId }).IsUnique();
    }
}

/// <summary>Plan M2.1: selected options of an answer (multi-select).</summary>
internal sealed class QuizAnswerSelectionConfiguration : IEntityTypeConfiguration<QuizAnswerSelection>
{
    public void Configure(EntityTypeBuilder<QuizAnswerSelection> builder)
    {
        builder.ToTable("quiz_answer_selections");
        builder.HasKey(s => new { s.QuizAnswerId, s.QuizOptionId });

        builder.HasOne<QuizOption>()
               .WithMany()
               .HasForeignKey(s => s.QuizOptionId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.QuizOptionId);
    }
}

/// <summary>Plan M2.2: question banks.</summary>
internal sealed class QuestionBankConfiguration : IEntityTypeConfiguration<QuestionBank>
{
    public void Configure(EntityTypeBuilder<QuestionBank> builder)
    {
        builder.ToTable("question_banks");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.RowVersion).IsRowVersion();
        builder.Property(b => b.Title).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Description).HasMaxLength(2000);

        builder.HasQueryFilter(b => !b.IsDeleted);

        builder.HasIndex(b => new { b.TenantId, b.CourseOfferingId });
        builder.HasIndex(b => new { b.TenantId, b.DepartmentId });
    }
}

/// <summary>Plan M2.2: "pick N from bank" groups.</summary>
internal sealed class QuizQuestionGroupConfiguration : IEntityTypeConfiguration<QuizQuestionGroup>
{
    public void Configure(EntityTypeBuilder<QuizQuestionGroup> builder)
    {
        builder.ToTable("quiz_question_groups");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.RowVersion).IsRowVersion();
        builder.Property(g => g.Title).HasMaxLength(200).IsRequired();
        builder.Property(g => g.MarksPerQuestion).HasColumnType("decimal(8,2)");
        builder.Property(g => g.TagFilter).HasMaxLength(100);

        builder.HasOne(g => g.Quiz)
               .WithMany(q => q.QuestionGroups)
               .HasForeignKey(g => g.QuizId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.QuestionBank)
               .WithMany()
               .HasForeignKey(g => g.QuestionBankId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(g => g.Quiz.IsActive);

        builder.HasIndex(g => g.QuizId);
    }
}

/// <summary>Plan M2.5: accommodations.</summary>
internal sealed class QuizAccommodationConfiguration : IEntityTypeConfiguration<QuizAccommodation>
{
    public void Configure(EntityTypeBuilder<QuizAccommodation> builder)
    {
        builder.ToTable("quiz_accommodations");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.RowVersion).IsRowVersion();
        builder.Property(a => a.TimeMultiplier).HasColumnType("decimal(4,2)").HasDefaultValue(1m);
        builder.Property(a => a.Note).HasMaxLength(500);

        builder.HasIndex(a => new { a.CourseOfferingId, a.QuizId, a.StudentProfileId }).IsUnique().HasFilter(null);
        builder.HasIndex(a => a.StudentProfileId);
    }
}

/// <summary>Plan M2.5: per-student availability windows.</summary>
internal sealed class QuizAvailabilityOverrideConfiguration : IEntityTypeConfiguration<QuizAvailabilityOverride>
{
    public void Configure(EntityTypeBuilder<QuizAvailabilityOverride> builder)
    {
        builder.ToTable("quiz_availability_overrides");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.RowVersion).IsRowVersion();
        builder.Property(o => o.Note).HasMaxLength(500);

        builder.HasOne<Quiz>()
               .WithMany()
               .HasForeignKey(o => o.QuizId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => new { o.QuizId, o.StudentProfileId }).IsUnique();
    }
}
