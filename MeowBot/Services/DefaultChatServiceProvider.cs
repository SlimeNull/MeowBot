using RustSharp;

namespace MeowBot.Services
{
    internal static class DefaultChatServiceProvider
    {
        internal static void Register(Func<AppConfig, AiChatServiceBase> instantiateCallback) => m_InstantiateCallback = instantiateCallback;
        private static Func<AppConfig, AiChatServiceBase>? m_InstantiateCallback;
        internal static async Task<Result<AiChatServiceBase, Exception>> Instantiate(AppConfig appConfig)
        {
            var instance = m_InstantiateCallback!.Invoke(appConfig);
            try
            {
                await instance.StartServiceAsync();
            }
            catch (Exception e)
            {
                return Result<AiChatServiceBase, Exception>.Err(e);
            }

            return Result<AiChatServiceBase, Exception>.Ok(instance);
        }
    }
}