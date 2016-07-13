using System;
using System.Collections.Generic;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home
{
    public static class Setup
    {
        public static IDisposable Run()
        {
            IocContainer.Build();

            var gateways = IocContainer.Resolve<IList<IGateway>>();
            return new Disposer(gateways);
        }

        private class Disposer : IDisposable
        {
            private readonly IEnumerable<IDisposable> _disposables;

            public Disposer(IEnumerable<IDisposable> disposables)
            {
                _disposables = disposables;
            }

            public void Dispose()
            {
                foreach (var disposable in _disposables)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
