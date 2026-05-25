using Nox.Instances;

namespace Nox.Instances.Runtime {
	public class Owner : IOwner {
		private readonly string _id;

		public Owner(string id) 
			=> _id = id;
		

		public string GetCategory()
			=> "users";

		public string GetId()
			=> _id;
	}
}