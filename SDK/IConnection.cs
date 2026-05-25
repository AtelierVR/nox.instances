namespace Nox.Instances {
	public interface IConnection {
		public string GetMethod();
		public T      GetData<T>() where T : class;
	}
}