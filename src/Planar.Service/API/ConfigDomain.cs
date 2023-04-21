﻿using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Data;
using Planar.Service.Exceptions;
using Planar.Service.Model;
using Planar.Service.Model.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.API
{
    public class ConfigDomain : BaseBL<ConfigDomain, ConfigData>
    {
        public ConfigDomain(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task Delete(string key)
        {
            try
            {
                await DataLayer.RemoveGlobalConfig(key);
                await Flush();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new RestNotFoundException($"global config with key '{key}' not found");
            }
        }

        public async Task Flush(CancellationToken stoppingToken = default)
        {
            var prms = await DataLayer.GetAllGlobalConfig(stoppingToken);
            var dict = prms.ToDictionary(p => p.Key, p => p.Value);
            Global.SetGlobalConfig(dict);
        }

        public async Task<GlobalConfig> Get(string key)
        {
            var data = await DataLayer.GetGlobalConfig(key) ?? throw new RestNotFoundException($"global config with key '{key}' not found");
            data.Value = $"<{Consts.Unauthorized}>";
            return data;
        }

        public async Task<IEnumerable<GlobalConfig>> GetAll()
        {
            var data = await DataLayer.GetAllGlobalConfig();

            if (RoleId == Roles.Administrator) { return data; }

            foreach (var item in data)
            {
                item.Value = $"<{Consts.Unauthorized}>";
            }

            return data;
        }

        public async Task Put(GlobalConfig request)
        {
            var exists = await DataLayer.IsGlobalConfigExists(request.Key);
            if (exists)
            {
                await DataLayer.UpdateGlobalConfig(request);
            }
            else
            {
                await DataLayer.AddGlobalConfig(request);
            }

            await Flush();
        }

        public async Task Add(GlobalConfig request)
        {
            var exists = await DataLayer.IsGlobalConfigExists(request.Key);

            if (exists)
            {
                throw new RestConflictException($"key {request.Key} already exists");
            }

            await DataLayer.AddGlobalConfig(request);
            await Flush();
        }

        public async Task Update(GlobalConfig request)
        {
            var exists = await DataLayer.IsGlobalConfigExists(request.Key);
            if (!exists)
            {
                throw new RestNotFoundException();
            }

            await DataLayer.UpdateGlobalConfig(request);
            await Flush();
        }
    }
}