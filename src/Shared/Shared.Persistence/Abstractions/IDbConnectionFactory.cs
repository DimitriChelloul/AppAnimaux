using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Persistence.Abstractions;

using System.Data;

public interface IDbConnectionFactory
{
    IDbConnection Create();
}

