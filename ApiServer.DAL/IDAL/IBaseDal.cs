﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Item.ApiServer.DAL.IDAL
{
    public interface IBaseDal<T> where T : class
    {
        void AddRange(IEnumerable<T> t);
        void AddRange(params T[] t);
        void DeleteRange(IEnumerable<T> t);
        void DeleteRange(params T[] t);
        void UpdateRange(IEnumerable<T> t);
        void UpdateRange(params T[] t);
        int CountAll();

        IQueryable<T> GetModels(Func<T, bool> whereLambda);

        bool SaveChanges();
    }
}