using System;
using System.Collections.Generic;

namespace Gtacnr.Client.Anticheat;

public class DetectionThresholdManager
{
	private int detectionThreshold;

	private TimeSpan timeWindow;

	private List<DateTime> detectionTimestamps = new List<DateTime>();

	public bool IsThresholdExceeded => detectionTimestamps.Count >= detectionThreshold;

	public DetectionThresholdManager(int detectionThreshold, TimeSpan timeWindow)
	{
		this.detectionThreshold = detectionThreshold;
		this.timeWindow = timeWindow;
	}

	public void AddDetection()
	{
		detectionTimestamps.Add(DateTime.UtcNow);
		detectionTimestamps.RemoveAll((DateTime timestamp) => DateTime.UtcNow - timestamp > timeWindow);
	}
}
