using Marten.Events.Aggregation;

namespace UserManager;

public class ReportProjection : SingleStreamProjection<Report>
{
    public void Apply(Events.ReportCreated created, Report report)
    {
        report.Id = created.Id;
        report.ReportedOn = created.ReportedOn;
        report.ReportedBy = created.ReportedBy;
        report.UserId = created.UserId;
    }
}