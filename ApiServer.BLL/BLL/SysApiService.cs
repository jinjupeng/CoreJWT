﻿using ApiServer.BLL.IBLL;
using ApiServer.Common;
using ApiServer.Model;
using ApiServer.Model.Entity;
using ApiServer.Model.Model;
using ApiServer.Model.Model.MsgModel;
using Mapster;
using System.Collections.Generic;
using System.Linq;

namespace ApiServer.BLL.BLL
{
    public class SysApiService : ISysApiService
    {
        private readonly IBaseService<Sys_Api> _baseService;
        private readonly IBaseService<Sys_Role_Api> _baseSysRoleApiService;
        private readonly IBaseService<Sys_Role> _baseSysRoleService;
        private readonly IMySystemService _mySystemService;

        public SysApiService(IBaseService<Sys_Api> baseService, IMySystemService mySystemService,
            IBaseService<Sys_Role_Api> baseSysRoleApiService, IBaseService<Sys_Role> baseSysRoleService)
        {
            _baseService = baseService;
            _mySystemService = mySystemService;
            _baseSysRoleApiService = baseSysRoleApiService;
            _baseSysRoleService = baseSysRoleService;
        }

        /// <summary>
        /// 获取到所有的角色和对应的api接口
        /// </summary>
        /// <returns></returns>
        public List<PermissionItem> GetAllApiOfRole()
        {
            List<PermissionItem> permissionItems = new List<PermissionItem>();
            List<Sys_Role> sysRoles = _baseSysRoleService.GetModels(a => a.status == false).ToList(); // 获取所有未禁用的角色
            List<Sys_Api> sysApis = _baseService.GetModels(a => a.status == false).ToList(); // 获取所有未禁用的接口
            List<Sys_Role_Api> sysRoleApis = _baseSysRoleApiService.GetModels(null).ToList();
            foreach (var sysRole in sysRoles)
            {
                foreach (var sysRoleApi in sysRoleApis)
                {
                    if (sysRole.id == sysRoleApi.role_id)
                    {
                        Sys_Api sysApi = sysApis.SingleOrDefault(a => a.id == sysRoleApi.api_id);
                        if (!string.IsNullOrEmpty(sysApi.url))
                        {
                            PermissionItem permissionItem = new PermissionItem
                            {
                                Url = sysApi.url,
                                Role = sysRole.role_code
                            };
                            permissionItems.Add(permissionItem);
                        }
                    }
                }
            }

            return permissionItems;
        }

        public MsgModel GetApiTreeById(string apiNameLike, bool apiStatus)
        {
            MsgModel msg = new MsgModel
            {
                isok = true,
                message = "查询成功！"
            };
            //查找level=1的API节点，即：根节点
            Sys_Api rootSysApi = _baseService.GetModels(s => s.level == 1).Single();
            if (rootSysApi != null)
            {
                long rootApiId = rootSysApi.id;
                List<Sys_Api> sysApis = _mySystemService.SelectApiTree(rootApiId, apiNameLike, apiStatus);
                TypeAdapterConfig<Sys_Api, SysApiNode>.NewConfig().NameMatchingStrategy(NameMatchingStrategy.ToCamelCase);
                List<SysApiNode> sysApiNodes = new List<SysApiNode>();
                foreach (Sys_Api sys_Api in sysApis)
                {
                    //SysApiNode sysApiNode = new SysApiNode
                    //{
                    //    id = sys_Api.id,
                    //    api_pid = sys_Api.api_pid,
                    //    api_pids = sys_Api.api_pids,
                    //    is_leaf = sys_Api.is_leaf,
                    //    api_name = sys_Api.api_name,
                    //    url = sys_Api.url,
                    //    sort = sys_Api.sort,
                    //    level = sys_Api.level,
                    //    status = sys_Api.status
                    //};
                    SysApiNode sysApiNode = sys_Api.BuildAdapter().AdaptToType<SysApiNode>();
                    sysApiNodes.Add(sysApiNode);
                }

                if (!string.IsNullOrEmpty(apiNameLike))
                {
                    //根据api名称等查询会破坏树形结构，返回平面列表
                    msg.data = sysApiNodes;
                    return msg;
                }

                //否则返回树型结构列表
                msg.data = DataTreeUtil<SysApiNode, long>.BuildTree(sysApiNodes, rootApiId);
                return msg;
            }
            return msg;
        }

        public MsgModel UpdateApi(Sys_Api sys_Api)
        {
            MsgModel msg = new MsgModel
            {
                isok = true,
                message = "修改接口配置成功！"
            };
            _baseService.UpdateRange(sys_Api);
            return msg;
        }

        public MsgModel AddApi(Sys_Api sys_Api)
        {
            MsgModel msg = new MsgModel
            {
                isok = true,
                message = "新增接口配置成功！"
            };
            sys_Api.id = new Snowflake().GetId();
            SetApiIdsAndLevel(sys_Api);
            sys_Api.is_leaf = true;//新增的菜单节点都是子节点，没有下级
            Sys_Api parent = new Sys_Api
            {
                id = sys_Api.api_pid,
                is_leaf = false//更新父节点为非子节点。
            };
            _baseService.UpdateRange(parent);
            sys_Api.status = false;//设置是否禁用，新增节点默认可用
            _baseService.AddRange(sys_Api);
            return msg;
        }

        public MsgModel DeleteApi(Sys_Api sys_Api)
        {
            MsgModel msg = new MsgModel
            {
                isok = true,
                message = "删除接口配置成功！"
            };
            // 查找被删除节点的子节点
            List<Sys_Api> myChild = _baseService.GetModels(s => s.api_pids.Contains("[" + sys_Api.id + "]")).ToList();
            if (myChild.Count > 0)
            {
                // "不能删除含有下级API接口的节点"
            }
            //查找被删除节点的父节点
            List<Sys_Api> myFatherChild = _baseService.GetModels(s => s.api_pids.Contains("[" + sys_Api.api_pid + "]")).ToList();
            //我的父节点只有我这一个子节点，而我还要被删除，更新父节点为叶子节点。
            if (myFatherChild.Count == 1)
            {
                Sys_Api parent = new Sys_Api
                {
                    id = sys_Api.api_pid,
                    is_leaf = true // //更新父节点为叶子节点。
                };
                _baseService.UpdateRange(parent);
            }
            // 删除节点
            _baseService.DeleteRange(sys_Api);
            return msg;
        }

        /// <summary>
        /// 设置某子节点的所有祖辈id
        /// </summary>
        /// <param name="child"></param>
        private void SetApiIdsAndLevel(Sys_Api child)
        {
            List<Sys_Api> allApis = _baseService.GetModels(null).ToList();
            foreach (var sysApi in allApis)
            {
                // 从组织列表中找到自己的直接父亲
                if (sysApi.id == child.api_pid)
                {
                    //直接父亲的所有祖辈id + 直接父id = 当前子节点的所有祖辈id
                    //爸爸的所有祖辈 + 爸爸 = 孩子的所有祖辈
                    child.api_pids = sysApi.api_pids + ",[" + child.api_pid + "]";
                    child.level = sysApi.level + 1;
                }
            }
        }

        /// <summary>
        /// 获取某角色勾选的API访问权限
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public List<string> GetCheckedKeys(long roleId)
        {
            return _mySystemService.SelectApiCheckedKeys(roleId);
        }

        /// <summary>
        /// 获取在API分类树中展开的项
        /// </summary>
        /// <returns></returns>
        public List<string> GetExpandedKeys()
        {
            return _mySystemService.SelectApiExpandedKeys();
        }

        /// <summary>
        /// 保存为某角色新勾选的API项
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="checkedIds"></param>
        public MsgModel SaveCheckedKeys(long roleId, List<long> checkedIds)
        {
            MsgModel msg = new MsgModel
            {
                isok = true,
                message = "保存接口权限成功！"
            };
            // 保存之前先删除
            var sysRoleApiList = _baseSysRoleApiService.GetModels(a => a.role_id == roleId);
            _baseSysRoleApiService.DeleteRange(sysRoleApiList);
            _mySystemService.InsertRoleApiIds(roleId, checkedIds);
            return msg;
        }

        /// <summary>
        /// 接口管理：更新接口的禁用状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="status"></param>
        public MsgModel UpdateStatus(long id, bool status)
        {
            Sys_Api sys_Api = _baseService.GetModels(a => a.id == id).SingleOrDefault();
            sys_Api.status = status;
            bool result = _baseService.UpdateRange(sys_Api);

            return MsgModel.Success(result ? "接口禁用状态更新成功！" : "接口禁用状态更新失败！");
        }
    }
}
