﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ApiServer.JWT
{
    /// <summary>
    /// 用户或角色或其他凭据实体
    /// </summary>
    public class PermissionItem
    {
        /// <summary>
        /// 用户或角色或其他凭据名称
        /// </summary>
        public virtual string Role { get; set; }
        /// <summary>
        /// 请求Url
        /// </summary>
        public virtual string Url { get; set; }
    }
}
