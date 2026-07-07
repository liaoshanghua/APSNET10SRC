将 .NET 10 运行时安装包放在此目录（离线部署）

APS 目标框架为 net10.0-windows（含 WinForms 托盘），必须包含三个安装包（缺一不可）：

  1. dotnet-runtime-10.x.x-win-x64.exe
  2. aspnetcore-runtime-10.x.x-win-x64.exe
  3. windowsdesktop-runtime-10.x.x-win-x64.exe

推荐：统一放在仓库根目录 deps\dotnet\（publish 时自动复制到发布包 runtime\）

  APSNET10\deps\dotnet\
    dotnet-runtime-10.x.x-win-x64.exe
    aspnetcore-runtime-10.x.x-win-x64.exe
    windowsdesktop-runtime-10.x.x-win-x64.exe

  若与源码版同目录，也可从源码版同步：

  cd APSNET10
  .\scripts\Sync-RuntimeFromNet10.ps1

  或源码版目录：

  EasyManufacture.Net10\deps\dotnet\

获取安装包：

  cd EasyManufacture.Net10
  .\scripts\Download-DotNetRuntimePack.ps1 -OutputDir ".\deps\dotnet"

  或发布时自动下载：

  .\scripts\Publish-Aps.ps1 -OutputDir ".\publish\api" -WithRuntime

注意：dotnet-sdk-*.exe 是开发 SDK，不能替代上述三个 runtime。

服务器上双击 APS-启动.bat 即可自动安装并启动。
