
namespace BeardedManStudios.Network
{
	public class WriteCustomMapping
	{
		public const uint MASTER_SERVER_REGISTER_SERVER = 0;
		public const uint MASTER_SERVER_UNREGISTER_SERVER = 1;
		public const uint MASTER_SERVER_UPDATE_SERVER = 2;
		public const uint MASTER_SERVER_GET_HOSTS = 3;
		public const uint NetWORKING_MANAGER_PLAYER_LOADED_LEVEL = 4;
		public const uint NetWORKING_MANAGER_POLL_PLAYERS = 5;

		public const uint NetWORKED_MONO_BEHAVIOR_MANUAL_PROPERTIES = 6;

		public const uint ROOM_MANAGER_READ_IN_ROOM = 7;
		public const uint ROOM_MANAGER_ENTER_ROOM = 8;
		public const uint ROOM_MANAGER_LEAVE_ROOM = 9;
		public const uint ROOM_MANAGER_WRITE_TO_ROOM = 10;

		public const uint TRANSPORT_OBJECT = 11;

		public const uint CACHE_READ_SERVER = 12;
		public const uint CACHE_READ_CLIENT = 13;
	}
}