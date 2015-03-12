using UnityEngine;
using System;
using System.Globalization;
using System.Collections.Generic;

public class QuarterScheduler {

    List<DateTime> quarterEndTimes = new List<DateTime>();
    bool initialized = false;

    public void Initialize()
    {
        initialized = RetrieveSchedule();
    }

    private bool RetrieveSchedule()
    {
        List<Quarter> quarters = ScheduleInfo.Inst.GetQuarters(BizSimManager.gameID); // gameID isn't set on construction, so need delayed init.
        if (quarters == null || quarters.Count == 0)
            return false;
        foreach (Quarter q in quarters)
        {
            ResizeList(quarterEndTimes, q.number);
            quarterEndTimes[q.number] = GetTimeFromStr(q.endTime);
        }
        return true;
    }

    private void ResizeList(List<DateTime> list, int size)
    {
        while (size >= list.Count)
            list.Add(DateTime.UtcNow);
    }

    private DateTime GetTimeFromStr(string timeStr)
    {
        try
        {
            string[] expectedFormats = { "M/d/yy H:mm:ss", "M/d/yy H:mm", "M/d/yy H:mm", "M/d/yy HH:mm:ss" };
            DateTime quarterEndTime = DateTime.ParseExact(timeStr, expectedFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal & DateTimeStyles.AllowWhiteSpaces);
            return quarterEndTime;
        }
        catch (FormatException)
        {
            Debug.LogError("Date string " + timeStr + " is not in the correct date format");
        }
        catch (ArgumentNullException)
        {
            Debug.LogError("Time String is null");
        }
        return DateTime.UtcNow.AddMonths(1);
    }

    public TimeSpan GetTimeLeftInQuarter(int quarter)
    {
        if (!Initialized)
            Initialize();
        if (!HasDataForQuarter(quarter))
        {
            Debug.LogError("Quarter " + quarter + " is out of range for the QuarterScheduler");
            return new TimeSpan();
        }
        return quarterEndTimes[quarter].Subtract(DateTime.UtcNow);
    }

    public bool HasDataForQuarter(int quarter)
    {
        if (!Initialized)
            Initialize();
        return quarter >= 0 && quarter < quarterEndTimes.Count;
    }

    public void Reload()
    {
        ScheduleInfo.Destroy();
        Initialize();
    }

    // Accessors
    public bool Initialized { get { return initialized; } }
}
