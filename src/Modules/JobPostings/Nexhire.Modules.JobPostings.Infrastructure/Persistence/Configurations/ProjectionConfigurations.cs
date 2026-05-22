using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.JobPostings.Core.Domain.Aggregates;
using Nexhire.Modules.JobPostings.Core.Domain.Repositories;

namespace Nexhire.Modules.JobPostings.Infrastructure.Persistence.Configurations;

public sealed class EmployerStandingConfiguration : IEntityTypeConfiguration<EmployerStanding>
{
    public void Configure(EntityTypeBuilder<EmployerStanding> builder)
    {
        builder.ToTable("employer_standing");
        builder.HasKey(x => x.EmployerId);
        builder.Property(x => x.IsVerified).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.UpdatedOnUtc).IsRequired();
    }
}

public sealed class PostingMetricsConfiguration : IEntityTypeConfiguration<PostingMetrics>
{
    public void Configure(EntityTypeBuilder<PostingMetrics> builder)
    {
        builder.ToTable("posting_metrics");
        builder.HasKey(x => x.JobPostingId);
        builder.Property(x => x.ApplicationsCount).IsRequired();
        builder.Property(x => x.MatchesCount).IsRequired();
        builder.Property(x => x.ViewsCount).IsRequired();
        builder.Property(x => x.UpdatedOnUtc).IsRequired();
    }
}
