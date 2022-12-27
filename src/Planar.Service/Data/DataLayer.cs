using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Model;
using Quartz;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.Data
{
    ////public class DataLayer : BaseDataLayer, IJobPropertyDataLayer
    ////{
    ////    public DataLayer(PlanarContext context) : base(context)
    ////    {
    ////    }

    ////    #region JobProperty

    ////    #endregion JobProperty
    ////}
}