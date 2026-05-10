# HRB.Payment.KeyTool.WebApi

页面版授权生成工具。

功能：
- 页面输入 activationCode / clientKey / validDays / otpCode
- 后端验证 activationCode + OTP
- 生成 `.key` 文件并直接下载

## 启动

```bash
export PATH=/home/agentuser/.dotnet:$PATH
cd src/HRB.Payment.KeyTool.WebApi
dotnet restore
dotnet run --urls http://0.0.0.0:63510
```

浏览器访问：
- http://服务器IP:63510/

## 配置

`appsettings.json` 中需要配置：
- SecretKey
- ActivationCode
- OtpSecret

当前仓库里的 `SecretKey` 示例值是占位值，若要生成可被现有客户端识别的 `.key`，必须替换成和客户端一致的真实 `SECRET_KEY`。
