# DANC - Display Adapter Name Changer

通过注册表快速修改显卡名称的工具

## 简介

DANC (Display Adapter Name Changer) 是一个用于修改 Windows 系统显示适配器（显卡）名称的实用工具。该工具通过直接修改 Windows 注册表中的设备描述信息来实现显卡名称的自定义。

## 功能特性

- **自动检测显示适配器**：自动枚举系统中的所有显示适配器设备
- **修改设备名称**：支持自定义显示适配器的名称
- **备份与恢复**：自动备份原始设备名称，支持一键恢复
- **多语言支持**：支持中文和英文界面
- **管理员权限**：自动检测并请求管理员权限
- **美观界面**：基于 Spectre.Console 的现代化控制台界面

## 系统要求

- Windows 10/11
- .NET 10.0 或更高版本
- 管理员权限（修改注册表需要）

## 使用方法

1. **以管理员身份运行程序**
   - 程序会自动检测管理员权限，如果不是管理员会提示并尝试自动重启

2. **选择操作**
   - 查看显示适配器列表
   - 修改显示适配器名称
   - 备份管理（查看、恢复、删除备份）
   - 语言设置

3. **修改名称**
   - 选择要修改的显示适配器
   - 输入新名称
   - 确认修改（默认选择"否"，需要手动确认）
   - 程序会自动创建备份

4. **恢复备份**
   - 如果修改后出现问题，可以通过备份管理功能恢复原始名称

## 注意事项

**重要提示**：
- 修改注册表存在风险，请谨慎操作
- 建议在修改前确保已创建备份
- 修改后可能需要重启计算机或重新安装驱动程序才能看到更改
- 某些系统更新可能会恢复原始名称

## 技术实现

- **注册表路径**：`HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\PCI\XXX\XXX\DeviceDesc`
- **备份存储**：使用 YAML 格式存储在用户配置目录
- **配置位置**：`%APPDATA%\DisplayAdapterNameChanger\config.yaml`

## 项目结构

```
DisplayAdapterNameChanger/
├── Models/          # 数据模型
├── Core/            # 核心功能模块
│   ├── BackupManager.cs          # 备份管理
│   ├── ConfigManager.cs           # 配置管理
│   ├── DisplayAdapterEnumerator.cs # 显示适配器枚举
│   ├── Localization.cs            # 本地化
│   ├── RegistryOperator.cs       # 注册表操作
│   └── PrivilegeManager.cs      # 权限管理
├── Resources/       # 资源文件（语言文件等）
└── Program.cs       # 主程序入口
```

## 免责声明

本工具仅供学习和研究使用。使用本工具修改系统注册表可能导致系统不稳定或其他问题，使用者需自行承担风险。作者不对使用本工具造成的任何损失负责。

