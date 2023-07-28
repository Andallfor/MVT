using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Time
{
    public double julian {get; private set;}
    public double julianCentury {get {return (this.julian - 2451545.0) / 36525.0;}}
    public DateTime date {get; private set;}
    public event EventHandler onChange;

    private bool sendEvent;

    public Time(double julian, bool sendEvent = false)
    {
        this.julian = julian;
        this.date = julianToDate(julian);
        this.sendEvent = sendEvent;
        onChange = delegate {};
    }

    public Time(DateTime date, bool sendEvent = false)
    {
        this.julian = dateToJulian(date);
        this.date = date;
        this.sendEvent = sendEvent;
        onChange = delegate {};
    }

    public void addJulianTime(double value, bool silent = false)
    {
        this.julian += value;
        this.date = julianToDate(this.julian);

        if (this.sendEvent && !silent) onChange(null, EventArgs.Empty);
    }

    public void addDateTime(TimeSpan value, bool silent = false)
    {
        this.date.Add(value);
        this.julian = dateToJulian(this.date);

        if (this.sendEvent && !silent) onChange(null, EventArgs.Empty);
    }

    public static double dateToJulian(DateTime date)
    {
        double Y = date.Year;
        double M = date.Month;
        double D = date.Day;
        double H = date.Hour;
        double Min = date.Minute;
        double S = date.Second;

        double JDN = 367 * Y - (int)((7 * (Y + (int)((M + 9) / 12.0))) / 4.0) + (int)((275 * M) / 9.0) + D + 1721013.5 +
          (H + Min / 60.0 + S / Math.Pow(60, 2)) / 24.0 - 0.5 * copySign(1, (100 * Y + M - 190002.5)) + 0.5;

        return JDN;
    }

    public static DateTime julianToDate(double julian)
    {
        // https://en.wikipedia.org/wiki/Julian_day#Julian_or_Gregorian_calendar_from_Julian_day_number
        double J = (double) ((int) julian);
        double f = J + 1401 + Math.Floor((Math.Floor((4.0 * J + 274277.0) / 146097) * 3.0) / 4.0) -38;
        double e = 4.0 * f + 3.0;
        double g = Math.Floor((e % 1461.0) / 4.0);
        double h = 5.0 * g + 2.0;

        double day = Math.Floor((h % 153.0) / 5.0) + 1;
        double month = ((Math.Floor(h / 153.0) + 2.0) % 12.0) + 1.0;
        double year = Math.Floor(e / 1461.0) - 4716.0 + Math.Floor((12.0 + 2.0 - month) / 12.0);

        double s = (julian - J) * 86400.0;
        double hours = Math.Floor(s / 3600.0);
        double minutes = Math.Floor((s - (hours * 3600.0)) / 60.0);
        double seconds = s - (hours * 3600.0 + minutes * 60.0);

        DateTime d = new DateTime((int) year, (int) month, (int) day, (int) hours, (int) minutes, (int) seconds);
        d = d.AddHours(12);

        return d;
    }

    public static double strDateToJulian(string date)
    {

        string[] splitDate = date.Split(new Char[] {'_', ':', ' ', '-'} , System.StringSplitOptions.RemoveEmptyEntries);
        
        double Y = Double.Parse(splitDate[0]); ;
        double M = Double.Parse(splitDate[1]); ;
        double D = Double.Parse(splitDate[2]); ;
        double H = Double.Parse(splitDate[3]); ;
        double Min = Double.Parse(splitDate[4]);
        double S = Double.Parse(splitDate[5]); ;

        double JDN = 367 * Y - (int)((7 * (Y + (int)((M + 9) / 12.0))) / 4.0) + (int)((275 * M) / 9.0) + D + 1721013.5 +
          (H + Min / 60.0 + S / Math.Pow(60, 2)) / 24.0 - 0.5 * copySign(1, (100 * Y + M - 190002.5)) + 0.5;

        return JDN;
    }

    public static double percentPastMidnight(DateTime d) => (d.Hour * 3600.0 + d.Minute * 60 + d.Second) / 86400.0;
    private static double copySign(double n, double s) => Math.Sign(s) * n;

    public override int GetHashCode() => (int) (this.julian * 1000);
    public override bool Equals(object obj)
    {
        if (!(obj is Time)) return false;
        Time t = (Time) obj;
        return t.julian == this.julian;
    }
    public override string ToString() => $"{this.date.ToString("ddd, dd MMM yyyy HH':'mm':'ss 'UTC'")} ({Math.Round(this.julian, 4)})";

    public static bool operator<(Time t1, Time t2) => t1.julian < t2.julian;
    public static bool operator>(Time t1, Time t2) => t1.julian > t2.julian;
    public static bool operator<=(Time t1, Time t2) => t1.julian <= t2.julian;
    public static bool operator>=(Time t1, Time t2) => t1.julian >= t2.julian;
    public static bool operator==(Time t1, Time t2) => t1.julian == t2.julian;
    public static bool operator!=(Time t1, Time t2) => t1.julian != t2.julian;
}
