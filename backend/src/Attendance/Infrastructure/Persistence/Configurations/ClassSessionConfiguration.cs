using Attendance.Domain.ClassSessionAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Infrastructure.Persistence.Configurations;

public class ClassSessionConfiguration : IEntityTypeConfiguration<ClassSession>
{
    public void Configure(EntityTypeBuilder<ClassSession> builder)
    {
        builder.ToTable("class_sessions");

        // 主キー (SessionId値オブジェクト)
        builder.HasKey(cs => cs.Id);
        builder.Property(cs => cs.Id)
            .HasColumnName("session_id")
            .HasConversion(
                id => id.Value,
                value => new SessionId(value))
            .ValueGeneratedNever(); // 手動で生成

        // OfferingId
        builder.Property(cs => cs.OfferingId)
            .HasColumnName("offering_id")
            .IsRequired();

        // SessionNumber
        builder.Property(cs => cs.SessionNumber)
            .HasColumnName("session_number")
            .IsRequired();

        // SessionDate
        builder.Property(cs => cs.SessionDate)
            .HasColumnName("session_date")
            .IsRequired();

        // StartTime
        builder.Property(cs => cs.StartTime)
            .HasColumnName("start_time")
            .IsRequired();

        // EndTime
        builder.Property(cs => cs.EndTime)
            .HasColumnName("end_time")
            .IsRequired();

        // Location
        builder.Property(cs => cs.Location)
            .HasColumnName("location")
            .HasMaxLength(50);

        // Topic
        builder.Property(cs => cs.Topic)
            .HasColumnName("topic")
            .HasMaxLength(200);

        // Status
        builder.Property(cs => cs.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        // CancellationReason
        builder.Property(cs => cs.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasMaxLength(200);

        // ユニーク制約: OfferingId + SessionNumber
        builder.HasIndex(cs => new { cs.OfferingId, cs.SessionNumber })
            .IsUnique()
            .HasDatabaseName("ix_class_sessions_offering_session");

        // インデックス
        builder.HasIndex(cs => cs.OfferingId)
            .HasDatabaseName("ix_class_sessions_offering_id");

        builder.HasIndex(cs => cs.SessionDate)
            .HasDatabaseName("ix_class_sessions_session_date");

        builder.HasIndex(cs => cs.Status)
            .HasDatabaseName("ix_class_sessions_status");

        // ドメインイベントは永続化しない
        builder.Ignore(cs => cs.DomainEvents);
    }
}
