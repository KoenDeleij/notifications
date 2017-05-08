using System;

namespace Acr.Notifications {
	public static class DateTimeExtensions {
		public static long ToEpochMills(this DateTime date) {
			var utc = date.ToUniversalTime();
			var epochDiff = (new DateTime(1970, 1, 1) - DateTime.MinValue).TotalSeconds;
			var utcAlarmTimeInMillis = utc.AddSeconds(-epochDiff).Ticks / 10000;
			return utcAlarmTimeInMillis;
		}
	}
}

