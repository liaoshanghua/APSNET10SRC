# APS Windows 部署文件（dotnet publish 时自动复制到发布目录，勿手改输出目录里的副本）



此目录为**唯一源码**，包含：



| 文件 | 用途 |

|------|------|

| APS-启动.bat | 手动启动（装依赖 + 前台运行） |

| APS-安装开机自启.bat | **系统开机自启**（无需登录，需管理员） |

| Install-ApsAutoStart.ps1 | 注册计划任务 ONSTART / SYSTEM |

| start-api.bat | 计划任务实际执行的启动脚本 |

| Install-ApsDependencies.ps1 | 检测并安装 .NET 10 |

| runtime/README.txt | 离线安装包说明 |



## 开机无人登录自启



1. 整包 publish 到服务器（如 `D:\publish\api`）

2. 改好 `appsettings.json`（连接串、端口）

3. 右键 **以管理员身份运行** `APS-安装开机自启.bat`

4. 重启验证：`http://服务器:9999/APSAPI/Ping`



发布命令：



```powershell

.\scripts\Publish-Aps.ps1 -WithRuntime

```

