﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentHub.IO.Extension
{
    public interface ISuspendedDisposal : IDisposable
    {
        void Register(Action method);
        void Cancel();
    }
}
