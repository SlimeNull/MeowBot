# MeowBot

## 摘要

此项目使用[.Net 7](https://learn.microsoft.com/zh-cn/dotnet/core/whats-new/dotnet-7)来调用OpenAI的Api，通过[EleCho.GoCqHttpSdk](https://github.com/OrgEleCho/EleCho.GoCqHttpSdk)来和[go-cqhttp]客户端通信处理消息收发。

## 介绍

此项目为一个QQ机器人，用于在群组内提供便捷的GPT服务，此机器人的详细功能如下：

服务类型|明细
:-|:-
GPT服务|群员可以直接@此Bot并且提问，Bot会通过提供的OpenAi API Key访问Open AI的端口来生成回应
流量管理|对于非白名单用户，Bot会根据应用程序配置的设置限制用户每秒可以使用GPT服务的次数
上下文记忆|对于非白名单用户，Bot会提供最大50条会话上下文的上下文记忆服务
用户命令： ```#help``` |@此Bot并且输入 ```#help``` 来获得Bot支持的所有功能解释
用户命令： ```#reset``` |@此Bot并且输入 ```#reset``` 来重置对话的上下文信息
用户命令： ```#temperature``` |@此Bot并且输入 ```#temperature:<温度信息(0~1)>``` 来调整AI的应答温度，越高的值会带来越随机的结果，反之则会带来越确定以及集中的结果
用户命令： ```#role``` |@此Bot并且输入 ```#role:<角色信息>``` 来从应用程序配置里预设的几种角色信息之中选择一种作为Bot回应该用户时的角色信息
用户命令： ```#custom-role``` |@此Bot并且输入 ```#custom-role:<自定义角色信息>``` 来将用户提供的文本作为Bot回应该用户时的角色信息
用户命令： ```#history``` |@此Bot并且输入 ```#history``` 来显示该用户当前的对话上下文记忆数量

当首次启动此项目时，会自动在项目应用程序目录产生应用**程序配置文件**```AppConfig.json```，以下是每个条目的介绍：

条目|名称|介绍
:-|:-|:-
 ```OpenAiApiKey``` |OpenAI API 密钥|用于调用 OpenAI API 的密钥
 ```BotWebSocketUri``` |go-cqhttp 通信WebSocket地址|此应用程序通过WebSocket来和go-cqhttp通信，对于默认本机通信，使用```ws://localhost:8080```
 ```ChatCompletionApiUrl``` |覆写OpenAI API 地址|当需要使用```https://api.openai.com/v1/chat/completions```以外的地址来调用OpenAI的API时，设置此项
 ```GptModel``` |覆写OpenAI GPT模型|当调用OpenAI的API需要使用```gpt-3.5-turbo```以外的模型时，设置此项
 ```UsageLimitTime``` |非白名单用户限制（秒）|设定非白名单用户在多少秒内能够使用多少次API
 ```UsageLimitCount``` |非白名单用户限制（次数）|设定非白名单用户在多少秒内能够使用多少次API
 ```AccountWhiteList``` |白名单用户QQ号|此名单内的QQ号不受流量管理以及50条上下文记忆的限制
 ```AccountBlackList``` |黑名单用户QQ号|此名单内的QQ号无法获得服务
 ```GptRoleInitText``` |覆写GPT角色语句|在用户使用GPT服务时，如果需要使用```你是一个基于GPT的会话机器人。如果用户询问你一个植根于真理的问题，你会提供解答。如果用户希望你对他们提供的信息发表看法或表达态度，你会礼貌的的拒绝他，并且表示这不是你的设计目的。```以外的角色初始化语句时，设置此项
 ```SystemCommand``` |额外系统指令|需要给GPT会话添加其他系统语句的情况下，设置此项
 ```BuiltinRoles``` |内置角色|需要为用户提供一系列的预设角色时，设置此项

## 关于 EleCho.GoCqHttpSdk 框架

EleCho.GoCqHttpSdk 使用教程:[使用 C# 和 Go-CqHttp 编写 QQ 机器人](https://www.bilibili.com/video/BV1P24y1V7XZ)
