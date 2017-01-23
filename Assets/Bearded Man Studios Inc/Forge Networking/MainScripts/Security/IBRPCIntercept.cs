using System.Reflection;

namespace BeardedManStudios.Network
{
	public interface IBRPCIntercept
	{
		bool ValidateRPC(NetworkingStreamRPC method);
		bool ValidateRPC(MethodInfo method);
	}
}