using Nox.Instances;

namespace api.nox.instance {
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