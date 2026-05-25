using Nox.CCK.Utils;
namespace Nox.Instances {
	/// <summary>
	/// Represents a live instance of a world,
	/// including its current state and players.
	/// </summary>
	public interface IInstance {
		/// <summary>
		/// A unique identifier for this instance, used for fetching and referencing.
		/// </summary>
		public uint Id { get; }

		/// <summary>
		/// The server address where this instance is hosted.
		/// </summary>
		public string Server { get; }

		/// <summary>
		/// Short identifier for this instance, unique within the server.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// A human-readable title for this instance,
		/// often derived from the world's name or set by the owner.
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// A description of the instance, which may include details about the world,
		/// the current session, or any custom information provided by the owner.
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// A URL or identifier for a thumbnail image representing this instance,
		/// often used in lists or search results to visually identify the instance.
		/// </summary>
		public string Thumbnail { get; }

		/// <summary>
		/// The identifier of the user who owns or created this instance.
		/// </summary>
		public Identifier Owner { get; }

		/// <summary>
		/// The identifier of the world that this instance is running.
		/// </summary>
		public Identifier World { get; }

		/// <summary>
		/// An array of tags associated with this instance,
		/// which can be used for categorization,
		/// search filtering,
		/// or providing additional metadata about the instance.
		/// </summary>
		public string[] Tags { get; }

		/// <summary>
		/// The connection information for this instance, including the protocol,
		/// port, and any necessary credentials or tokens for joining the instance.
		/// </summary>
		public IConnection Connection { get; }

		/// <summary>
		/// The current number of client connected to this instance,
		/// which may be used to determine how many players are currently in the instance
		/// and whether there is room for more players to join.
		/// </summary>
		public ushort ClientCount { get; }

		/// <summary>
		/// An array of players currently in this instance, including their identifiers, display names.
		/// </summary>
		public IInstancePlayer[] Players { get; }

		/// <summary>
		/// The maximum number of players that can join this instance,
		/// which may be determined by the world's settings or server limitations.
		/// If is 0, it means there is no limit and any number of players can join.
		/// </summary>
		public ushort Capacity { get; }

		/// <summary>
		/// The unique identifier for this instance, combining the server address and instance ID,
		/// used for globally referencing this instance across the network.
		/// </summary>
		public Identifier Identifier { get; }
	}
}