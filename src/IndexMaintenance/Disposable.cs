using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IndexMaintenance
{
    class Disposable : IDisposable
    {
        private Action _action;

        public Disposable(Action action)
        {
            _action = action;
        }
        public void Dispose()
        {
            _action();
        }
    }
}
