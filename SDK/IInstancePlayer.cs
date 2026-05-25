using Nox.CCK.Utils;

namespace Nox.Instances {
	/// <summary>
	/// Represents a player in an instance.
	/// This is a simplified version of the IUser interface,
	/// containing only the information relevant to instances.
	/// </summary>
	public interface IInstancePlayer {
		/// <summary>
		/// The unique identifier of the player.
		/// </summary>
		public Identifier Identifier { get; }

		/// <summary>
		/// The display name of the player.
		/// </summary>
		public string Display { get; }
	}
}