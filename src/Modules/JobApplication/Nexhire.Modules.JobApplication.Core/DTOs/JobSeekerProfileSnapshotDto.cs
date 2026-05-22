using System;
using System.Collections.Generic;

namespace Nexhire.Modules.JobApplication.Core.DTOs;

public record JobSeekerProfileSnapshotDto(
    Guid JobSeekerProfileId,
    Guid UserId,
    string FullName,
    string Email,
    string Mobile,
    string CurrentLocation,
    string EducationSummary,
    string ExperienceSummary,
    List<string> Skills,
    bool IsLevel2Complete,
    string Visibility);
