using System;
namespace Acr.Notifications {
	public class Notifications {
		private static INotifications _notifications;
		public static INotifications Instance<T>() where T :INotifications,new(){
			if(_notifications==null)
				_notifications= new T();
			
			return _notifications;
		}
	}
}

