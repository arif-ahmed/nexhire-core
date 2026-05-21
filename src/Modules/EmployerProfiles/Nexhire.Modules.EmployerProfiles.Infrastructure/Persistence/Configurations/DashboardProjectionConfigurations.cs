using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexhire.Modules.EmployerProfiles.Core.Domain.Projections;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure.Persistence.Configurations;

public class DashboardPostingConfiguration : IEntityTypeConfiguration<DashboardPosting>
{
    public void Configure(EntityTypeBuilder<DashboardPosting> builder)
    {
        builder.ToTable("dashboard_postings");
        builder.HasKey(dp => dp.PostingId);

        builder.Property(dp => dp.EmployerUserId).IsRequired();
        builder.Property(dp => dp.Title).IsRequired().HasMaxLength(250);
        builder.Property(dp => dp.Status).IsRequired().HasMaxLength(50);
        builder.Property(dp => dp.LastEventOnUtc).IsRequired();
    }
}

public class DashboardApplicationConfiguration : IEntityTypeConfiguration<DashboardApplication>
{
    public void Configure(EntityTypeBuilder<DashboardApplication> builder)
    {
        builder.ToTable("dashboard_applications");
        builder.HasKey(da => da.ApplicationId);

        builder.Property(da => da.EmployerUserId).IsRequired();
        builder.Property(da => da.PostingId).IsRequired();
        builder.Property(da => da.JobSeekerId).IsRequired();
        builder.Property(da => da.SubmittedOnUtc).IsRequired();
    }
}

public class DashboardMatchedCandidateConfiguration : IEntityTypeConfiguration<DashboardMatchedCandidate>
{
    public void Configure(EntityTypeBuilder<DashboardMatchedCandidate> builder)
    {
        builder.ToTable("dashboard_matched_candidates");
        builder.HasKey(dmc => dmc.Id);

        builder.Property(dmc => dmc.EmployerUserId).IsRequired();
        builder.Property(dmc => dmc.PostingId).IsRequired();
        builder.Property(dmc => dmc.CandidateUserId).IsRequired();
        builder.Property(dmc => dmc.MatchScore).IsRequired();
        builder.Property(dmc => dmc.GeneratedOnUtc).IsRequired();
    }
}
