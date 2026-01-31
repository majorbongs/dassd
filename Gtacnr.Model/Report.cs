using System;

namespace Gtacnr.Model;

public class Report
{
	public string Id { get; set; }

	public string ReporterUserId { get; set; }

	public string ReportedUserId { get; set; }

	public ReportReason Reason { get; set; }

	public string Details { get; set; }

	public DateTime DateTime { get; set; }

	public string ServerId { get; set; }

	public string ResponderUserId { get; set; }

	public ReportState State { get; set; }

	public string ClosingResponse { get; set; }

	public string ReporterUserName { get; set; }

	public string ReportedUserName { get; set; }

	public string ResponderUserName { get; set; }
}
