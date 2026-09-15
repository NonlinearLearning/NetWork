using System.Net;
using System.Net.Sockets;
using System.Threading;
using Terraria.NetWork.Core.Adaptation;
using Terraria.NetWork.Core.Messages;
using Terraria.NetWork.Core.Protocol;
using Terraria.NetWork.Core.Server;
using Terraria.NetWork.Core.Server.Pipeline;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class MessageTcpRoundTripTests
{
    public static void Run()
    {
        RunCase("稳定连接与清理", StableConnectionIdAndCleanupTests);
        RunCase("客户端到服务端单包回环", ClientToServerLoopbackPacketTests);
        RunCase("客户端到服务端首包门禁拒绝", ClientToServerGateRejectsUnexpectedFirstPacketTests);
        RunCase("客户端到服务端预同步零载荷", ClientToServerPreWorldSyncZeroPayloadTests);
        RunCase("客户端到服务端多包顺序", ClientToServerBurstPacketTests);
        RunCase("服务端到客户端单包回环", ServerToClientLoopbackPacketTests);
        RunCase("服务端到客户端零载荷", ServerToClientZeroPayloadPacketTests);
        RunCase("服务端到客户端多包顺序", ServerToClientBurstPacketTests);
        RunCase("单会话发送约束", SessionSendNetMessageGuardTests);
        RunCase("服务端多目标去重与离线跳过", ServerFanoutSkipsOfflineAndDuplicateTargetsTests);
    }

    private static void StableConnectionIdAndCleanupTests()
    {
        PacketDefinitionRegistry.RegisterCore();
        PrintStep("连接标识", "协议注册表已初始化");

        var port = GetFreeTcpPort();
        PrintStep("连接标识", $"已分配 TCP 端口 {port}");
        var registry = new SessionRegistry();
        var sessionConnected = new ManualResetEventSlim(false);
        var sessionDisconnected = new ManualResetEventSlim(false);
        var messageReceivedByClient1 = new ManualResetEventSlim(false);
        var messageReceivedByClient2 = new ManualResetEventSlim(false);
        var connectedSessions = new List<MessageTcpSession>();

        using var server = CreateServer(port, registry);
        server.SessionConnected += session =>
        {
            lock (connectedSessions)
            {
                connectedSessions.Add(session);
                if (connectedSessions.Count == 2)
                {
                    sessionConnected.Set();
                }
            }
        };
        server.SessionDisconnected += _ => sessionDisconnected.Set();

        Ensure(server.Start(), "MessageTcpServer 应当成功启动。");
        PrintStep("连接标识", "服务端已启动");

        using var client1 = new MessageTcpClient("127.0.0.1", port);
        using var client2 = new MessageTcpClient("127.0.0.1", port);
        client1.MessageReceived += _ => messageReceivedByClient1.Set();
        client2.MessageReceived += _ => messageReceivedByClient2.Set();

        client1.ConnectAsync();
        client2.ConnectAsync();
        PrintStep("连接标识", "两个客户端已发起连接");

        Ensure(sessionConnected.Wait(TimeSpan.FromSeconds(5)), "服务端应当观察到两个客户端连接。");
        PrintStep("连接标识", "服务端已观察到两个客户端连接");

        MessageTcpSession[] sessions;
        lock (connectedSessions)
        {
            sessions = connectedSessions.OrderBy(session => session.ConnectionId).ToArray();
        }

        Ensure(sessions.Length == 2, "应当捕获两个服务端会话。");
        Ensure(sessions[0].ConnectionId != sessions[1].ConnectionId, "两个连接必须拥有不同且稳定的 ConnectionId。");
        PrintStep("连接标识", $"已分配稳定连接标识：{sessions[0].ConnectionId}, {sessions[1].ConnectionId}");

        server.SendNetMessage(new SendNetMessage
        {
            Session = [sessions[0], sessions[1]],
            MessageId = (byte)PacketType.RequestWorldData,
            Payload = []
        });
        PrintStep("连接标识", "服务端已发送一条多目标 SendNetMessage");

        Ensure(messageReceivedByClient1.Wait(TimeSpan.FromSeconds(5)), "客户端 1 应当收到多目标消息。");
        Ensure(messageReceivedByClient2.Wait(TimeSpan.FromSeconds(5)), "客户端 2 应当收到多目标消息。");
        PrintStep("连接标识", "两个客户端都已收到多目标消息");

        var disconnectedId = sessions[0].ConnectionId;
        client1.DisconnectAndStop();

        Ensure(sessionDisconnected.Wait(TimeSpan.FromSeconds(5)), "服务端应当观察到断连。");
        PrintStep("连接标识", $"服务端已观察到会话 {disconnectedId} 断开");

        Ensure(!registry.TryGet(disconnectedId, out _), "断连会话必须从 SessionRegistry 中移除。");
        PrintStep("连接标识", "SessionRegistry 清理已验证");

        client2.DisconnectAndStop();
        server.Stop();
        PrintStep("连接标识", "剩余客户端已断开，服务端已停止");
    }

    private static void ClientToServerLoopbackPacketTests()
    {
        PacketDefinitionRegistry.RegisterCore();
        PrintStep("客户端到服务端", "协议注册表已初始化");

        var port = GetFreeTcpPort();
        PrintStep("客户端到服务端", $"已分配 TCP 端口 {port}");
        var serverConnected = new ManualResetEventSlim(false);
        var packetReceived = new ManualResetEventSlim(false);
        HelloPacketRaw? receivedPacket = null;

        using var server = CreateServer(port);
        server.SessionConnected += _ => serverConnected.Set();
        server.PacketReceived += (_, packet) =>
        {
            if (packet is HelloPacketRaw hello)
            {
                receivedPacket = hello;
                packetReceived.Set();
            }
        };

        Ensure(server.Start(), "MessageTcpServer 应当成功启动。");
        PrintStep("客户端到服务端", "服务端已启动");

        using var client = new MessageTcpClient("127.0.0.1", port);
        client.ConnectAsync();
        PrintStep("客户端到服务端", "客户端已发起连接");

        Ensure(serverConnected.Wait(TimeSpan.FromSeconds(5)), "服务端应当观察到客户端连接。");
        PrintStep("客户端到服务端", "服务端已观察到客户端连接");

        client.SendPacket(new HelloPacketRaw
        {
            ClientVersion = "Terraria318"
        });
        PrintStep("客户端到服务端", "客户端已发送 HelloPacketRaw");

        Ensure(packetReceived.Wait(TimeSpan.FromSeconds(5)), "服务端应当成功接收并解码 HelloPacketRaw。");
        Ensure(receivedPacket is not null, "服务端必须拿到解码后的 HelloPacketRaw。");
        var helloPacket = receivedPacket!;
        Ensure(helloPacket.ClientVersion == "Terraria318", $"HelloPacketRaw 版本不匹配。Actual={helloPacket.ClientVersion}");
        PrintStep("客户端到服务端", "服务端已解码 HelloPacketRaw");

        client.DisconnectAndStop();
        server.Stop();
        PrintStep("客户端到服务端", "客户端已断开，服务端已停止");
    }

    private static void ClientToServerGateRejectsUnexpectedFirstPacketTests()
    {
        PacketDefinitionRegistry.RegisterCore();
        PrintStep("首包门禁", "协议注册表已初始化");

        var port = GetFreeTcpPort();
        PrintStep("首包门禁", $"已分配 TCP 端口 {port}");
        var registry = new SessionRegistry();
        var serverConnected = new ManualResetEventSlim(false);
        var messageReceived = new ManualResetEventSlim(false);
        var packetReceived = new ManualResetEventSlim(false);
        MessageTcpSession? connectedSession = null;

        using var server = CreateServer(port, registry);
        server.SessionConnected += session =>
        {
            connectedSession = session;
            serverConnected.Set();
        };
        server.MessageReceived += (_, _) => messageReceived.Set();
        server.PacketReceived += (_, _) => packetReceived.Set();

        Ensure(server.Start(), "MessageTcpServer 应当成功启动。");
        using var client = new MessageTcpClient("127.0.0.1", port);
        client.ConnectAsync();

        Ensure(serverConnected.Wait(TimeSpan.FromSeconds(5)) && connectedSession is not null, "服务端应当捕获已连接会话。");
        var connected = connectedSession!;
        PrintStep("首包门禁", $"服务端已捕获连接会话 {connected.ConnectionId}");

        client.SendPacket(new PasswordPacketRaw
        {
            Password = "secret"
        });
        PrintStep("首包门禁", "客户端首包发送 PasswordPacketRaw");

        Ensure(messageReceived.Wait(TimeSpan.FromSeconds(5)), "门禁拒绝场景下，服务端仍应看到入站消息。");
        Thread.Sleep(200);
        Ensure(!packetReceived.IsSet, "首包门禁拒绝后，不应产生解码后的协议包。");

        Ensure(registry.TryGet(connected.ConnectionId, out var sessionContext) && sessionContext is not null, "应能从 SessionRegistry 中取到会话上下文。");
        var gateContext = sessionContext!;
        Ensure(gateContext.Gate is not null, "门禁结果应当写回 SessionContext。");
        var gate = gateContext.Gate!;
        Ensure(gate.Decision == SessionGateDecision.Boot, $"首包错误应触发 Boot。Actual={gate.Decision}");
        PrintStep("首包门禁", "已验证错误首包被门禁拦截且未进入解码阶段");

        client.DisconnectAndStop();
        server.Stop();
        PrintStep("首包门禁", "客户端已断开，服务端已停止");
    }

    private static void ClientToServerPreWorldSyncZeroPayloadTests()
    {
        PacketDefinitionRegistry.RegisterCore();
        PrintStep("预同步零载荷", "协议注册表已初始化");

        var port = GetFreeTcpPort();
        PrintStep("预同步零载荷", $"已分配 TCP 端口 {port}");
        var registry = new SessionRegistry();
        var stateMachine = new SessionStateMachine();
        var serverConnected = new ManualResetEventSlim(false);
        var packetReceived = new ManualResetEventSlim(false);
        MessageTcpSession? connectedSession = null;
        RequestWorldDataPacket? receivedPacket = null;

        using var server = CreateServer(port, registry);
        server.SessionConnected += session =>
        {
            connectedSession = session;
            serverConnected.Set();
        };
        server.PacketReceived += (_, packet) =>
        {
            if (packet is RequestWorldDataPacket request)
            {
                receivedPacket = request;
                packetReceived.Set();
            }
        };

        Ensure(server.Start(), "MessageTcpServer 应当成功启动。");
        using var client = new MessageTcpClient("127.0.0.1", port);
        client.ConnectAsync();

        Ensure(serverConnected.Wait(TimeSpan.FromSeconds(5)) && connectedSession is not null, "服务端应当捕获已连接会话。");
        var connected = connectedSession!;
        Ensure(registry.TryGet(connected.ConnectionId, out var sessionContext) && sessionContext is not null, "应能从 SessionRegistry 中取到会话上下文。");
        var preWorldSession = sessionContext!;
        stateMachine.MoveToPreWorldSync(preWorldSession, isAuthenticated: true);
        PrintStep("预同步零载荷", $"会话 {connected.ConnectionId} 已切换到 PreWorldSync");

        client.SendPacket(new RequestWorldDataPacket());
        PrintStep("预同步零载荷", "客户端已发送 RequestWorldDataPacket");

        Ensure(packetReceived.Wait(TimeSpan.FromSeconds(5)), "PreWorldSync 状态应允许 RequestWorldDataPacket。");
        Ensure(receivedPacket is not null, "服务端应当解码出 RequestWorldDataPacket。");
        Ensure(preWorldSession.Gate is not null && preWorldSession.Gate.Decision == SessionGateDecision.Allow, "PreWorldSync 下的 6 号包应被允许。");
        PrintStep("预同步零载荷", "已验证 PreWorldSync 状态允许零载荷请求包");

        client.DisconnectAndStop();
        server.Stop();
        PrintStep("预同步零载荷", "客户端已断开，服务端已停止");
    }

    private static void ClientToServerBurstPacketTests()
    {
        PacketDefinitionRegistry.RegisterCore();
        PrintStep("客户端多包", "协议注册表已初始化");

        var port = GetFreeTcpPort();
        PrintStep("客户端多包", $"已分配 TCP 端口 {port}");
        var serverConnected = new ManualResetEventSlim(false);
        var twoPacketsReceived = new ManualResetEventSlim(false);
        var versions = new List<string>();

        using var server = CreateServer(port);
        server.SessionConnected += _ => serverConnected.Set();
        server.PacketReceived += (_, packet) =>
        {
            if (packet is HelloPacketRaw hello)
            {
                lock (versions)
                {
                    versions.Add(hello.ClientVersion);
                    if (versions.Count >= 2)
                    {
                        twoPacketsReceived.Set();
                    }
                }
            }
        };

        Ensure(server.Start(), "MessageTcpServer 应当成功启动。");
        using var client = new MessageTcpClient("127.0.0.1", port);
        client.ConnectAsync();

        Ensure(serverConnected.Wait(TimeSpan.FromSeconds(5)), "服务端应当观察到客户端连接。");
        PrintStep("客户端多包", "服务端已观察到客户端连接");

        client.SendPacket(new HelloPacketRaw { ClientVersion = "Burst-A" });
        client.SendPacket(new HelloPacketRaw { ClientVersion = "Burst-B" });
        PrintStep("客户端多包", "客户端已连续发送两条 HelloPacketRaw");

        Ensure(twoPacketsReceived.Wait(TimeSpan.FromSeconds(5)), "服务端应当收到连续两条 HelloPacketRaw。");

        lock (versions)
        {
            Ensure(versions.Count == 2, $"应当解码出两条 HelloPacketRaw。Actual={versions.Count}");
            Ensure(versions[0] == "Burst-A" && versions[1] == "Burst-B", $"服务端应保留入站顺序。Actual=[{string.Join(",", versions)}]");
        }
        PrintStep("客户端多包", "服务端已按顺序解码两条 HelloPacketRaw");

        client.DisconnectAndStop();
        server.Stop();
        PrintStep("客户端多包", "客户端已断开，服务端已停止");
    }

    private static void ServerToClientLoopbackPacketTests()
    {
        PacketDefinitionRegistry.RegisterCore();
        PrintStep("服务端到客户端", "协议注册表已初始化");

        var port = GetFreeTcpPort();
        PrintStep("服务端到客户端", $"已分配 TCP 端口 {port}");
        var serverConnected = new ManualResetEventSlim(false);
        var clientPacketReceived = new ManualResetEventSlim(false);
        var clientMessageReceived = new ManualResetEventSlim(false);
        MessageTcpSession? connectedSession = null;
        DisconnectPacket? receivedPacket = null;
        NetMessage? receivedMessage = null;

        using var server = CreateServer(port);
        server.SessionConnected += session =>
        {
            connectedSession = session;
            serverConnected.Set();
        };

        Ensure(server.Start(), "MessageTcpServer 应当成功启动。");
        PrintStep("服务端到客户端", "服务端已启动");

        using var client = new MessageTcpClient("127.0.0.1", port);
        client.MessageReceived += message =>
        {
            receivedMessage = message;
            clientMessageReceived.Set();
        };
        client.PacketReceived += packet =>
        {
            if (packet is DisconnectPacket disconnect)
            {
                receivedPacket = disconnect;
                clientPacketReceived.Set();
            }
        };
        client.ConnectAsync();
        PrintStep("服务端到客户端", "客户端已发起连接");

        Ensure(serverConnected.Wait(TimeSpan.FromSeconds(5)) && connectedSession is not null, "服务端应当捕获已连接会话。");
        var connected = connectedSession!;
        PrintStep("服务端到客户端", $"服务端已捕获连接会话 {connected.ConnectionId}");

        connected.SendPacket(new DisconnectPacket
        {
            ReasonKey = "loopback"
        });
        PrintStep("服务端到客户端", "服务端已发送 DisconnectPacket");

        Ensure(clientMessageReceived.Wait(TimeSpan.FromSeconds(5)), "客户端应当收到出站 NetMessage。");
        Ensure(clientPacketReceived.Wait(TimeSpan.FromSeconds(5)), "客户端应当解码出 DisconnectPacket。");
        Ensure(receivedMessage is not null, "客户端必须捕获 NetMessage。");
        var disconnectMessage = receivedMessage!;
        Ensure(disconnectMessage.MessageId == (byte)PacketType.Kick, $"客户端应收到 Kick 消息。Actual={disconnectMessage.MessageId}");
        Ensure(receivedPacket is not null && receivedPacket.ReasonKey == "loopback", "客户端解码出的 DisconnectPacket 内容不正确。");
        PrintStep("服务端到客户端", "客户端已完成 NetMessage 与 DisconnectPacket 解码");

        client.DisconnectAndStop();
        server.Stop();
        PrintStep("服务端到客户端", "客户端已断开，服务端已停止");
    }

    private static void ServerToClientZeroPayloadPacketTests()
    {
        PacketDefinitionRegistry.RegisterCore();
        PrintStep("服务端零载荷", "协议注册表已初始化");

        var port = GetFreeTcpPort();
        PrintStep("服务端零载荷", $"已分配 TCP 端口 {port}");
        var serverConnected = new ManualResetEventSlim(false);
        var clientPacketReceived = new ManualResetEventSlim(false);
        var clientMessageReceived = new ManualResetEventSlim(false);
        MessageTcpSession? connectedSession = null;
        RequestWorldDataPacket? receivedPacket = null;
        NetMessage? receivedMessage = null;

        using var server = CreateServer(port);
        server.SessionConnected += session =>
        {
            connectedSession = session;
            serverConnected.Set();
        };

        Ensure(server.Start(), "MessageTcpServer 应当成功启动。");
        using var client = new MessageTcpClient("127.0.0.1", port);
        client.MessageReceived += message =>
        {
            receivedMessage = message;
            clientMessageReceived.Set();
        };
        client.PacketReceived += packet =>
        {
            if (packet is RequestWorldDataPacket request)
            {
                receivedPacket = request;
                clientPacketReceived.Set();
            }
        };
        client.ConnectAsync();

        Ensure(serverConnected.Wait(TimeSpan.FromSeconds(5)) && connectedSession is not null, "服务端应当捕获已连接会话。");
        var connected = connectedSession!;
        PrintStep("服务端零载荷", $"服务端已捕获连接会话 {connected.ConnectionId}");

        connected.SendPacket(new RequestWorldDataPacket());
        PrintStep("服务端零载荷", "服务端已发送 RequestWorldDataPacket");

        Ensure(clientMessageReceived.Wait(TimeSpan.FromSeconds(5)), "客户端应收到零载荷 NetMessage。");
        Ensure(clientPacketReceived.Wait(TimeSpan.FromSeconds(5)), "客户端应解码出零载荷 RequestWorldDataPacket。");
        Ensure(receivedMessage is not null && receivedMessage.MessageId == (byte)PacketType.RequestWorldData, "客户端收到的零载荷消息号不正确。");
        Ensure(receivedPacket is not null, "客户端必须解码出零载荷 RequestWorldDataPacket。");
        PrintStep("服务端零载荷", "客户端已完成零载荷消息接收与解码");

        client.DisconnectAndStop();
        server.Stop();
        PrintStep("服务端零载荷", "客户端已断开，服务端已停止");
    }

    private static void ServerToClientBurstPacketTests()
    {
        PacketDefinitionRegistry.RegisterCore();
        PrintStep("服务端多包", "协议注册表已初始化");

        var port = GetFreeTcpPort();
        PrintStep("服务端多包", $"已分配 TCP 端口 {port}");
        var serverConnected = new ManualResetEventSlim(false);
        var secondPacketReceived = new ManualResetEventSlim(false);
        MessageTcpSession? connectedSession = null;
        var receivedPacketKinds = new List<byte>();

        using var server = CreateServer(port);
        server.SessionConnected += session =>
        {
            connectedSession = session;
            serverConnected.Set();
        };

        Ensure(server.Start(), "MessageTcpServer 应当成功启动。");
        using var client = new MessageTcpClient("127.0.0.1", port);
        client.PacketReceived += packet =>
        {
            lock (receivedPacketKinds)
            {
                receivedPacketKinds.Add(packet switch
                {
                    DisconnectPacket => (byte)PacketType.Kick,
                    RequestWorldDataPacket => (byte)PacketType.RequestWorldData,
                    _ => 255
                });

                if (receivedPacketKinds.Count >= 2)
                {
                    secondPacketReceived.Set();
                }
            }
        };
        client.ConnectAsync();

        Ensure(serverConnected.Wait(TimeSpan.FromSeconds(5)) && connectedSession is not null, "服务端应当捕获已连接会话。");
        var connected = connectedSession!;
        PrintStep("服务端多包", $"服务端已捕获连接会话 {connected.ConnectionId}");

        connected.SendPacket(new DisconnectPacket { ReasonKey = "burst-a" });
        connected.SendPacket(new RequestWorldDataPacket());
        PrintStep("服务端多包", "服务端已连续发送两条不同数据包");

        Ensure(secondPacketReceived.Wait(TimeSpan.FromSeconds(5)), "客户端应当收到连续两条出站数据包。");

        lock (receivedPacketKinds)
        {
            Ensure(receivedPacketKinds.Count == 2, $"应当解码出两条出站数据包。Actual={receivedPacketKinds.Count}");
            Ensure(receivedPacketKinds[0] == (byte)PacketType.Kick &&
                   receivedPacketKinds[1] == (byte)PacketType.RequestWorldData,
                $"客户端应保留服务端出站顺序。Actual=[{string.Join(",", receivedPacketKinds)}]");
        }
        PrintStep("服务端多包", "客户端已按顺序解码两条服务端数据包");

        client.DisconnectAndStop();
        server.Stop();
        PrintStep("服务端多包", "客户端已断开，服务端已停止");
    }

    private static void SessionSendNetMessageGuardTests()
    {
        PacketDefinitionRegistry.RegisterCore();
        PrintStep("发送约束", "协议注册表已初始化");

        var port = GetFreeTcpPort();
        PrintStep("发送约束", $"已分配 TCP 端口 {port}");
        var serverConnected = new ManualResetEventSlim(false);
        MessageTcpSession? connectedSession = null;

        using var server = CreateServer(port);
        server.SessionConnected += session =>
        {
            connectedSession = session;
            serverConnected.Set();
        };

        Ensure(server.Start(), "MessageTcpServer 应当成功启动。");
        using var client = new MessageTcpClient("127.0.0.1", port);
        client.ConnectAsync();

        Ensure(serverConnected.Wait(TimeSpan.FromSeconds(5)) && connectedSession is not null, "服务端应当捕获已连接会话。");
        var connected = connectedSession!;
        PrintStep("发送约束", $"服务端已捕获连接会话 {connected.ConnectionId}");

        ExpectThrows<InvalidOperationException>(() => connected.SendNetMessage(new SendNetMessage
        {
            Session = [],
            MessageId = (byte)PacketType.RequestWorldData,
            Payload = []
        }), "空目标 SendNetMessage 应当被单会话发送器拒绝。");

        ExpectThrows<InvalidOperationException>(() => connected.SendNetMessage(new SendNetMessage
        {
            Session = [connected, connected],
            MessageId = (byte)PacketType.RequestWorldData,
            Payload = []
        }), "多目标 SendNetMessage 应当被单会话发送器拒绝。");

        ExpectThrows<InvalidOperationException>(() => connected.SendNetMessage(new SendNetMessage
        {
            Session = [new ServerSessionRef { ConnectionId = connected.ConnectionId + 100 }],
            MessageId = (byte)PacketType.RequestWorldData,
            Payload = []
        }), "指向其他会话的 SendNetMessage 应当被单会话发送器拒绝。");
        PrintStep("发送约束", "已验证单会话发送器拒绝空目标、多目标和错目标消息");

        client.DisconnectAndStop();
        server.Stop();
        PrintStep("发送约束", "客户端已断开，服务端已停止");
    }

    private static void ServerFanoutSkipsOfflineAndDuplicateTargetsTests()
    {
        PacketDefinitionRegistry.RegisterCore();
        PrintStep("多目标边界", "协议注册表已初始化");

        var port = GetFreeTcpPort();
        PrintStep("多目标边界", $"已分配 TCP 端口 {port}");
        var sessionConnected = new ManualResetEventSlim(false);
        var client1Received = new ManualResetEventSlim(false);
        var client2Received = new ManualResetEventSlim(false);
        var client1Count = 0;
        var client2Count = 0;
        var connectedSessions = new List<MessageTcpSession>();

        using var server = CreateServer(port);
        server.SessionConnected += session =>
        {
            lock (connectedSessions)
            {
                connectedSessions.Add(session);
                if (connectedSessions.Count == 2)
                {
                    sessionConnected.Set();
                }
            }
        };

        Ensure(server.Start(), "MessageTcpServer 应当成功启动。");
        using var client1 = new MessageTcpClient("127.0.0.1", port);
        using var client2 = new MessageTcpClient("127.0.0.1", port);
        client1.MessageReceived += _ =>
        {
            Interlocked.Increment(ref client1Count);
            client1Received.Set();
        };
        client2.MessageReceived += _ =>
        {
            Interlocked.Increment(ref client2Count);
            client2Received.Set();
        };

        client1.ConnectAsync();
        client2.ConnectAsync();

        Ensure(sessionConnected.Wait(TimeSpan.FromSeconds(5)), "服务端应当观察到两个客户端连接。");
        PrintStep("多目标边界", "服务端已观察到两个客户端连接");

        MessageTcpSession[] sessions;
        lock (connectedSessions)
        {
            sessions = connectedSessions.OrderBy(session => session.ConnectionId).ToArray();
        }

        server.SendNetMessage(new SendNetMessage
        {
            Session = [sessions[0], sessions[0], sessions[1], new ServerSessionRef { ConnectionId = 99999 }],
            MessageId = (byte)PacketType.RequestWorldData,
            Payload = []
        });
        PrintStep("多目标边界", "服务端已发送包含重复目标和离线目标的消息");

        Ensure(client1Received.Wait(TimeSpan.FromSeconds(5)), "在线客户端 1 应当收到去重后的消息。");
        Ensure(client2Received.Wait(TimeSpan.FromSeconds(5)), "在线客户端 2 应当收到去重后的消息。");
        Thread.Sleep(200);

        Ensure(client1Count == 1 && client2Count == 1,
            $"重复目标或离线目标不应造成额外投递。Actual={client1Count},{client2Count}");
        PrintStep("多目标边界", "已验证重复目标去重且离线目标被跳过");

        client1.DisconnectAndStop();
        client2.DisconnectAndStop();
        server.Stop();
        PrintStep("多目标边界", "客户端已断开，服务端已停止");
    }

    private static MessageTcpServer CreateServer(int port, SessionRegistry? registry = null)
    {
        return new MessageTcpServer(
            IPAddress.Loopback,
            port,
            new ReceiveNetMessagePipeline(new SessionGateMiddleware(new SessionGate())),
            registry);
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static void RunCase(string name, Action test)
    {
        Console.WriteLine($"[回环] 开始执行{name}测试");
        test();
        Console.WriteLine($"[回环] {name}测试通过");
    }

    private static void PrintStep(string scope, string message)
    {
        Console.WriteLine($"[回环][{scope}] {message}");
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void ExpectThrows<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
            throw new InvalidOperationException(message);
        }
        catch (TException)
        {
        }
    }
}
