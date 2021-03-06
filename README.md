# Fluent.Hub

サーバーアプリケーションを構築する為のフレームワークです。

## How to Use

### 電文モデル定義（IPingPongAppMessageアプリケーションプロトコル）
```csharp
// IPingPongAppMessageアプリケーションプロトコル
public interface IPingPongAppMessage { }

public class Ping : IPingPongAppMessage
{
    public byte ID { get; set; } = 0x01;
}

public class Pong : IPingPongAppMessage
{
    public byte ID { get; set; } = 0x02;
}

public class Tunnel : IPingPongAppMessage
{
    public byte ID { get; set; } = 0x03;
}

// IPingPongAppMessageアプリケーションプロトコルの電文コンバーター
public class PingModelConverter : WrapperModelConverter<IPingPongAppMessage>
{
    protected override IModelConverter<IPingPongAppMessage> MakeConverter()
    {
        return new Ping().ToModelBuilder()
                // Bigエンディアンで通信する
                .ToBigEndian()
                // 1byte目は定数（電文識別子）
                .Constant((byte)0x01)
                // ModelConverter型へ変換
                .ToConverter()
                .ToBaseTypeConverter<Ping, IPingPongAppMessage>();
    }
}

public class PongModelConverter : WrapperModelConverter<IPingPongAppMessage>
{
    protected override IModelConverter<IPingPongAppMessage> MakeConverter()
    {
        return new Pong().ToModelBuilder()
                // Bigエンディアンで通信する
                .ToBigEndian()
                // 1byte目は定数（電文識別子）
                .Constant((byte)0x02)
                // ModelConverter型へ変換
                .ToConverter()
                .ToBaseTypeConverter<Pong, IPingPongAppMessage>();
    }
}

public class TunnelModelConverter : WrapperModelConverter<IPingPongAppMessage>
{
    protected override IModelConverter<IPingPongAppMessage> MakeConverter()
    {
        return new Tunnel().ToModelBuilder()
                // Bigエンディアンで通信する
                .ToBigEndian()
                // 1byte目は定数（電文識別子）
                .Constant((byte)0x03)
                // ModelConverter型へ変換
                .ToConverter()
                .ToBaseTypeConverter<Tunnel, IPingPongAppMessage>();
    }
}

```

### 電文モデル定義（IThirdAppMessageアプリケーションプロトコル）
```csharp
// IThirdAppMessageアプリケーションプロトコル
public interface IThirdAppMessage { }

public class Pang : IThirdAppMessage
{
    public byte ID { get; set; } = 0x01;
    public int Value { get; set; }
    public IInnerModel InnerModel { get; set; }
    public InnerModel InnerModel2 { get; set; }
    public IEnumerable<InnerModel> Array { get; set; }
    public InnerModel[] FixedArray { get; set; }
}
public interface IInnerModel
{
    int Value1 { get; set; }
    int Value2 { get; set; }
}

public class InnerModel : IInnerModel
{
    public int Value1 { get; set; }
    public int Value2 { get; set; }
}
// IThirdAppMessageアプリケーションプロトコルの電文コンバーター
public class PangModelConverter : WrapperModelConverter<IThirdAppMessage>
{
    protected override IModelConverter<IThirdAppMessage> MakeConverter()
    {
        // Pang電文のModelConverterを生成
        return new Pang().ToModelBuilder()
                                // Bigエンディアンで通信する
                                .ToBigEndian()
                                // モデルの初期化が必要なメンバはここで初期化する
                                .Init(m => m.InnerModel = new InnerModel())
                                // 1byte目は定数（電文識別子）
                                .Constant((byte)0x03)
                                // Modelには表現されないけどPaddingブロックなんかがあるなら
                                .Constant(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })
                                // メンバ変換
                                .Property(m => m.Value)
                                // メンバ変換(メンバのメンバ)
                                .Property(m => m.InnerModel.Value1)
                                .Property(m => m.InnerModel.Value2)
                                // 配列の数が電文に含まれてたりするよね。
                                // 書き込むときはメンバの値を書けばいいけど復元する時は読むだけでいいよね。
                                // っていうときはGetProperty。
                                // そして読んだ値を覚えておきたいよねって時にAsTagで読み込んだ値に名前付けておく
                                .GetProperty(m => m.Array.Count()).AsTag("InnerCount")
                                // さらにInnerCountを配列復元する時に使いたいよね
                                .Array("InnerCount", m => m.Array
                                    // Arrayメンバの要素の型InnerModelのModelBuilderを入れ子で
                                    , b => b.Property(mi => mi.Value1)
                                            .Property(mi => mi.Value2))
                                // 固定長の配列もあるよね
                                .FixedArray(5, m => m.FixedArray
                                    , b => b.Property(mi => mi.Value1)
                                            .Property(mi => mi.Value2))
                                // メンバクラスも入れ子で定義できたら便利だよね
                                .Property(m => m.InnerModel2
                                    , b => b.Property(mi => mi.Value1)
                                            .Property(mi => mi.Value2))
                                // ModelConverter型へ変換
                                .ToConverter()
                                // ModelConverter<Pang> -> ModelConverter<IPingPongAppMessage>
                                .ToBaseTypeConverter<Pang, IThirdAppMessage>();
    }
}
```

### server App

```csharp
public class TestServer
{
    public void Run(string[] args)
    {
        // アプリケーションコンテナ
        var appContainer = new ApplicationContainer();
        // IPingPongAppMessage型の電文をやり取りするサーバーアプリケーションを生成
        var app =
            // 待ち受けポートは8089
            appContainer.MakeAppByTcpServer<IPingPongAppMessage>(8089)
            // Ping電文のbyte[] <=> Model変換定義
            .RegisterConverter(new PingModelConverter())
            // Pong電文のbyte[] <=> Model変換定義
            .RegisterConverter(new PongModelConverter())
            // Tunnel電文のbyte[] <=> Model変換定義
            .RegisterConverter(new TunnelModelConverter())
            // Pingを受信したらPongを送信するシーケンス
            .RegisterSequence((IIOContext<IPingPongAppMessage> sender, Ping model) =>
            {
                sender.Write(new Pong());
            });
        // 異なるプロトコルを持つ第3者通信相手を定義
        var thirdApp =
            appContainer.MakeAppByTcpServer<IThirdAppMessage>(8099)
            .RegisterConverter(new PangModelConverter());

        // 3者間シーケンス
        // クライアントからTunnelを受信した時のシーケンス
        appContainer.RegisterSequence((IIOContext<IPingPongAppMessage> sender, Tunnel recvMessage, IEnumerable<IIOContext<IThirdAppMessage>> thirdAppContexts) =>
        {
            // 接続中のIThirdAppMessageプロトコルを持つ相手にPangを送信
            foreach (var thirdContext in thirdAppContexts)
            {
                var pang = new Pang
                {
                    InnerModel = new InnerModel { Value1 = 11, Value2 = 12 },
                    Array = new[] { new InnerModel { Value1 = 1, Value2 = 2 }, new InnerModel { Value1 = 3, Value2 = 4 } },
                    FixedArray = new[] { new InnerModel { Value1 = 5, Value2 = 6 }, new InnerModel { Value1 = 7, Value2 = 8 } },
                    InnerModel2 = new InnerModel { Value1 = 9, Value2 = 10 },
                };
                thirdContext.Write(pang);
            }

            // 送信元のsenderにPongを返送
            sender.Write(new Pong());
        });

        appContainer.Run();
    }
}
```

### client App(IPingPongAppMessage)

```csharp
public class TestClient
{
    public void Run(string[] args)
    {
        // アプリケーションコンテナ
        var appContainer = new ApplicationContainer();
        // IPingPongAppMessage型の電文をやり取りするサーバーアプリケーションを生成
        var app =
            // 待ち受けポートは8089
            appContainer.MakeAppByTcpClient<IPingPongAppMessage>("localhost", 8089)
            // Ping電文のbyte[] <=> Model変換定義
            .RegisterConverter(new PingModelConverter())
            // Pong電文のbyte[] <=> Model変換定義
            .RegisterConverter(new PongModelConverter())
            // Tunnel電文のbyte[] <=> Model変換定義
            .RegisterConverter(new TunnelModelConverter());

        Task.Run((Action)appContainer.Run);


        while (true)
        {
            // Enter to send Ping
            Console.ReadLine();

            // サーバーにPingメッセージを送信
            appContainer.GetApp<IPingPongAppMessage>().InstantSequence((contexts =>
            {
                var server = contexts.FirstOrDefault();
                if (server == null)
                {
                    return;
                }
                // 送信
                server.Write(new Ping());
                // Pongを受信するまで10秒待機
                var pong = server.Read(m => m is Pong, 1000 * 10);

                // send Tunnel
                server.Write(new Tunnel());

                // Pongを受信するまで10秒待機
                var pong2 = server.Read(m => m is Pong, 1000 * 10);
            }));
        }
    }
}
```

### client App（IThirdAppMessage）

```csharp
public class TestOtherClient
{
    public void Run(string[] args)
    {
        // アプリケーションコンテナ
        var appContainer = new ApplicationContainer();
        // IPingPongAppMessage型の電文をやり取りするサーバーアプリケーションを生成
        var app =
            // 待ち受けポートは8089
            appContainer.MakeAppByTcpClient<IThirdAppMessage>("localhost", 8099)
            // Ping電文のbyte[] <=> Model変換定義
            .RegisterConverter(new PangModelConverter())
            .RegisterSequence((IIOContext<IThirdAppMessage> context, Pang model) =>
            {
                appContainer.Logger.Debug("Pang!");
            });

        Task.Run((Action)appContainer.Run);

        Console.ReadLine();
    }
}
```




## install
```
# TCP Server
Install-Package Fluent.Hub.TCP
```

## Licence

[MIT](https://raw.githubusercontent.com/shchy/FluentHub/master/LICENSE)

## Author

[shch](https://github.com/shchy)
