﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ApiServer.Common.DB
{
    public class BaseDbConfig
    {
        public string ConnectionString { get; set; }
        public DatabaseType DbType { get; set; }
    }

    public enum DatabaseType
    {
        SqlServer = 0,
        MySql = 1,
        Oracle = 2,
        Dm = 3,
        HighGo = 4,
        KingBase = 5,
    }
}
