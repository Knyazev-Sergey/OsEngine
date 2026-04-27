# Esunny Connector (via C++ Router)

This connector works in two layers:

1. `OsEngine` C# connector (`EsunnyServer.cs`)
2. Local C++ router process (`EsunnyRouter.exe`) that talks to `libdstartradeapi`

## Build router

Router project file:

- `i:\OsE\esunny\易盛启明星V10交易API_V1.0.1.19-20240118\esunnyV10-demo\project\VS2010\esunny_router\EsunnyRouter.vcxproj`

Build `x64` (`Debug` or `Release`).  
After build, copy output files to:

- `Engine\Routers\Esunny\EsunnyRouter.exe`
- `Engine\Routers\Esunny\libdstartradeapi.dll`

## Connector parameters in OsEngine

- `Router host` - usually `127.0.0.1`
- `Router port` - default `19090`
- `Auto start router` - when `true`, OsEngine starts router exe
- `Router exe path` - e.g. `Engine\Routers\Esunny\EsunnyRouter.exe`
- `Front IP`, `Front port`
- `Account`, `Password`, `AppId`, `LicenseNo`
- `Use UDP` - keep `false` for initial integration
- `Seat index` - default `1`

## Transport protocol

Line-delimited JSON over TCP.

Commands from OsEngine to router:

- `connect`
- `get_securities`
- `get_portfolios`
- `send_order`
- `cancel_order`

Events from router to OsEngine:

- `connected` / `disconnected`
- `log`
- `securities`
- `portfolio`
- `order`
- `trade`
- `my_trade`

## Current limitations

- `ChangeOrderPrice` not implemented (use cancel + new order).
- Candle history requests are not implemented in this first version.
- UDP auth flow is currently reserved; use TCP order flow first.
