﻿using System;
using System.Collections.Generic;
using System.Web;
using System.Globalization;

public static class DateTimeExt {
	#region 轉成民國日期格式(民國yyy年mm月dd日) +static string ToLongTwDate(this DateTime datetime)
	/// <summary>
	/// 轉成民國日期格式(民國yyy年mm月dd日)
	/// </summary>
	public static string ToLongTwDate(this DateTime datetime) {
		return datetime.ToLongTwDate(true);
	}
	#endregion

	#region 轉成民國日期格式(民國yyy年mm月dd日) +static string ToLongTwDate(this DateTime datetime, bool padZero)
	/// <summary>
	/// 轉成民國日期格式(民國yyy年mm月dd日)
	/// </summary>
	public static string ToLongTwDate(this DateTime datetime, bool padZero) {
		TaiwanCalendar taiwanCalendar = new TaiwanCalendar();

		if (padZero) {
			return string.Format("民國{0}年{1}月{2}日",
				taiwanCalendar.GetYear(datetime).ToString().PadLeft(3, '0'),
				datetime.Month.ToString().PadLeft(2, '0'),
				datetime.Day.ToString().PadLeft(2, '0'));
		} else {
			return string.Format("民國{0}年{1}月{2}日",
				taiwanCalendar.GetYear(datetime).ToString(),
				datetime.Month.ToString(),
				datetime.Day.ToString());
		}
	}
	#endregion

	#region 轉成民國日期格式(yyy/mm/dd) +static string ToShortTwDate(this DateTime datetime)
	/// <summary>
	/// 轉成民國日期格式(yyy/mm/dd)
	/// </summary>
	public static string ToShortTwDate(this DateTime datetime) {
		return datetime.ToShortTwDate(true);
	}
	#endregion

	#region 轉成民國日期格式(yyy/mm/dd) +static string ToShortTwDate(this DateTime datetime, bool padZero)
	/// <summary>
	/// 轉成民國日期格式(yyy/mm/dd)
	/// </summary>
	public static string ToShortTwDate(this DateTime datetime, bool padZero) {
		TaiwanCalendar taiwanCalendar = new TaiwanCalendar();

		if (padZero) {
			return string.Format("{0}/{1}/{2}",
				taiwanCalendar.GetYear(datetime).ToString().PadLeft(3, '0'),
				datetime.Month.ToString().PadLeft(2, '0'),
				datetime.Day.ToString().PadLeft(2, '0'));
		} else {
			return string.Format("{0}/{1}/{2}",
				taiwanCalendar.GetYear(datetime).ToString(),
				datetime.Month.ToString(),
				datetime.Day.ToString());
		}
	}
	#endregion

	#region 取得民國年 +static int GetTwYear(this DateTime datetime)
	/// <summary>
	/// 取得民國年
	/// </summary>
	public static int GetTwYear(this DateTime datetime) {
		TaiwanCalendar taiwanCalendar = new TaiwanCalendar();

		return taiwanCalendar.GetYear(datetime);
	}
	#endregion

	#region 轉成西元年99/99/9999(月/日/年) 給informix SmallDateTime使用 +static string ToD9Date(this string dateString)
	/// <summary>
	/// 轉成西元年99/99/9999(月/日/年) 給informix SmallDateTime 使用
	/// </summary>
	public static string ToD9Date(this string dateString) {
		string RtnVal = "";
		DateTime dt = new DateTime();
		if (DateTime.TryParse(dateString, out dt)) {
			RtnVal = dt.ToString("MM/dd/yyyy");
		} else {
			RtnVal = "";
		}
		return RtnVal;
	}
	#endregion
}