namespace Rewind.MVVM.Services
{
    /// <summary>
    /// Лёгкий DI-контейнер. Регистрируется один раз в App.OnStartup
    /// и хранит синглтоны сервисов приложения (навигация, диалоги и т.п.).
    /// Сознательно не тянем полноценный IoC-контейнер — для курсового проекта
    /// ручной локатор нагляднее и достаточен.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<TService>(TService instance) where TService : class
        {
            ArgumentNullException.ThrowIfNull(instance);
            _services[typeof(TService)] = instance;
        }

        public static TService Resolve<TService>() where TService : class
        {
            if (_services.TryGetValue(typeof(TService), out var svc))
                return (TService)svc;
            throw new InvalidOperationException(
                $"Сервис {typeof(TService).Name} не зарегистрирован в ServiceLocator.");
        }

        public static TService? TryResolve<TService>() where TService : class
            => _services.TryGetValue(typeof(TService), out var svc) ? (TService)svc : null;
    }
}
