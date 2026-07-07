.NET 10 运行时离线包（开发机统一存放，publish 时自动复制到发布目录 runtime\）

请将以下文件放在本目录（*.exe 不提交 Git，体积大）：

  1. dotnet-runtime-10.x.x-win-x64.exe
  2. aspnetcore-runtime-10.x.x-win-x64.exe

可选：
  - dotnet-install.ps1（离线脚本安装）
  - dotnet\ 子目录（已解压的便携运行时，含 dotnet.exe）

获取方式（任选其一）：

  cd EasyManufacture.Net10
  .\scripts\Download-DotNetRuntimePack.ps1 -OutputDir ".\deps\dotnet"

  或

  .\scripts\Publish-Aps.ps1 -OutputDir ".\publish\api" -WithRuntime
  （会先下载到 deps\dotnet，再复制到 publish\api\runtime）

发布后服务器目录：

  publish\api\runtime\*.exe
  由 APS-启动.bat -> Install-ApsDependencies.ps1 自动安装

注意：服务器不需要 .NET SDK（dotnet-sdk-*.exe），只要上述两个 Runtime 安装包。
SDK 不能替代 aspnetcore-runtime，放了 SDK 仍会提示“未检测到 ASP.NET Core 10”。
