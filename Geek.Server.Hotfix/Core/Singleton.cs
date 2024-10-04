namespace Server.Logic.Core
{
	public class Singleton<T>
	{
		private static T instance;

		public static T Instance
		{
			get
			{
				if (instance == null)
				{
					instance = (T)Activator.CreateInstance(typeof(T), true);
				}
				return instance;
			}
			protected set => instance = value;
		}
	}
}