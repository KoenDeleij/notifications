using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Java.IO;


namespace Acr.Notifications
{

    public class NotificationsImpl : AbstractNotificationsImpl
    {
        readonly AlarmManager alarmManager;
        public int AppIconResourceId { get; set; }


        public NotificationsImpl()
        {
            this.AppIconResourceId = Application.Context.Resources.GetIdentifier("icon", "drawable", Application.Context.PackageName);
            this.alarmManager = (AlarmManager)Application.Context.GetSystemService(Context.AlarmService);
        }


        public override string Send(Notification notification)
        {
			int id = notification.Id.HasValue ? notification.Id.Value : NotificationSettings.Instance.CreateScheduleId();

            if (notification.IsScheduled)
            {
				var triggerMs = notification.SendTime.ToEpochMills();
                var pending = notification.ToPendingIntent(id);

				if (notification.Interval == NotificationInterval.None) {
					this.alarmManager.Set(
						AlarmType.RtcWakeup,
						Convert.ToInt64(triggerMs),
						pending
						);
				}
				else {
					//depending on the interval, calculate the next trigger
					DateTime secondTrigger = notification.SendTime.AddDays(notification.Interval == NotificationInterval.Daily ? 1 : 7);

					//substract the current trigger to get the inteval
					var intervalMs = secondTrigger.ToEpochMills()-triggerMs;
					this.alarmManager.SetRepeating(AlarmType.Rtc, triggerMs, intervalMs, pending);
				}

                return id.ToString();
            }

            var launchIntent = Application.Context.PackageManager.GetLaunchIntentForPackage(Application.Context.PackageName);
            launchIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);

            var builder = new NotificationCompat
                .Builder(Application.Context)
                .SetAutoCancel(true)
                .SetContentTitle(notification.Title)
                .SetContentText(notification.Message)
                .SetSmallIcon(this.AppIconResourceId)
                .SetContentIntent(TaskStackBuilder
                    .Create(Application.Context)
                    .AddNextIntent(launchIntent)
                    .GetPendingIntent(id, (int)PendingIntentFlags.UpdateCurrent)
                );

            if (notification.Vibrate)
            {
                builder.SetVibrate(new long[] { 500, 500 });
            }

            if (notification.Sound != null)
            {
                var file = new File(notification.Sound);
                var uri = Android.Net.Uri.FromFile(file);
                builder.SetSound(uri);
            }

			if (notification.BadgeCount.HasValue) {
				this.Badge = notification.BadgeCount.Value;
			}

            var not = builder.Build();
            NotificationManagerCompat
                .From(Application.Context)
                .Notify(id, not);
            return id.ToString();
        }


        public override void CancelAll()
        {
            foreach (var id in NotificationSettings.Instance.ScheduleIds)
                this.CancelInternal(id);

            NotificationSettings.Instance.ClearScheduled();
            NotificationManagerCompat
                .From(Application.Context)
                .CancelAll();
        }


        public override bool Cancel(string id)
        {
            var @int = 0;
            if (!Int32.TryParse(id, out @int))
                return false;

            this.CancelInternal(@int);
            NotificationSettings.Instance.RemoveScheduledId(@int);
            return true;
        }


        public override int Badge
        {
            get { return NotificationSettings.Instance.CurrentBadge; }
            set
            {
				try {
					NotificationSettings.Instance.CurrentBadge = value;
					if (value <= 0)
						ME.Leolin.Shortcutbadger.ShortcutBadger.RemoveCount(Application.Context);
					else
						ME.Leolin.Shortcutbadger.ShortcutBadger.ApplyCount(Application.Context, value);
				}
				catch (Exception e) {
					//
				}
            }
        }


        public override void Vibrate(int ms)
        {
            using (var vibrate = (Vibrator)Application.Context.GetSystemService(Context.VibratorService))
            {
                if (!vibrate.HasVibrator)
                    return;

                vibrate.Vibrate(ms);
            }
        }

        void CancelInternal(int notificationId)
        {
            var pending = Helpers.GetNotificationPendingIntent(notificationId);
            pending.Cancel();
            this.alarmManager.Cancel(pending);
            NotificationManagerCompat
                .From(Application.Context)
                .Cancel(notificationId);
        }
    }
}
