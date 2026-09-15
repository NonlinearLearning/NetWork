namespace Terraria.NetWork.Core.Server;


//抽象的消息接口
//
public interface IServerSession
{
    int ConnectionId { get; }
}
