﻿using ApiServer.Model.Model;
using System.Collections.Generic;

namespace ApiServer.Model
{
    public class DataTreeUtil<T, ID> where T : IDataTree<T, ID>
    {
        public static List<T> BuildTreeWithoutRoot(List<T> paramList, ID rootNodeId)
        {
            List<T> returnList = new List<T>();
            //查找根节点
            foreach (T node in paramList)
            {
                //从2级节点开始构造
                if (node.GetParentId().Equals(rootNodeId))
                {
                    returnList.Add(node);
                }
            }
            foreach (T entry in paramList)
            {
                ToTreeChildren(returnList, entry);
            }
            return returnList;
        }

        /// <summary>
        /// 构造只有一个根的树形结构数据
        /// </summary>
        /// <param name="paramList"></param>
        /// <param name="rootNodeId"></param>
        /// <returns></returns>
        public static List<T> BuildTree(List<T> paramList, ID rootNodeId)
        {
            List<T> returnList = new List<T>();
            // 查找根节点
            foreach (T node in paramList)
            {
                //从1级节点开始构造
                if (node.GetId().Equals(rootNodeId))
                {
                    returnList.Add(node);
                }
            }
            foreach (T entry in paramList)
            {
                ToTreeChildren(returnList, entry);
            }
            return returnList;
        }

        // TODO：会造成栈溢出
        private static void ToTreeChildren(List<T> returnList, T entry)
        {
            foreach (T node in returnList)
            {
                if (entry.GetParentId().Equals(node.GetId()))
                {
                    if (node.Children == null)
                    {
                        node.Children = new List<T>();
                    }
                    node.Children.Add(entry);
                }
                if (node.Children != null)
                {
                    ToTreeChildren(node.Children, entry);
                }
            }
        }
    }
}
