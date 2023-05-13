namespace MeowBot.Services
{
    internal abstract class AiChatServiceBase
    {
        protected AppConfig AppConfig { get; }

        internal AiChatServiceBase(AppConfig config)
        {
            AppConfig = config;
        }

        public virtual Task StartServiceAsync()
        {
            return Task.CompletedTask;
        }

        internal abstract Task<Exception?> AskAsync(AskCommandArgsModel askCommandArgs, Func<string, bool, Task> sendMessageCallback);

        /// <summary>
        /// 检查用户输入的文本是否是给定的命令，并且回复对应的命令
        /// </summary>
        /// <param name="msgTxt">用户输入的文本</param>
        /// <param name="appConfig">应用程序设置</param>
        /// <param name="userId">发送消息的用户ID</param>
        /// <param name="sendMessageCallback">执行消息发送动作的回调</param>
        /// <returns>用户输入的文本是否是当前服务所能处理的命令</returns>
        public abstract Task<bool> HandlePotentialUserCommands(string msgTxt, AppConfig appConfig, long userId, Func<string, bool, Task> sendMessageCallback);
    }

    internal readonly struct AskCommandArgsModel
    {
        public readonly string MsgTxt;
        public readonly string UserNickname;
        public readonly long UserId;

        public AskCommandArgsModel(string msgTxt, string userNickname, long userId)
        {
            MsgTxt = msgTxt;
            UserNickname = userNickname;
            UserId = userId;
        }
    }
}