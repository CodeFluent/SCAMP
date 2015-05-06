﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using DocumentDbRepositories;

namespace DocumentDbRepositories.Implementation
{

    internal class SystemSettingsRepository : ISystemSettingsRepository
    {
        DocDb docdb;

        public SystemSettingsRepository(DocDb docdb)
        {
            this.docdb = docdb;
        }

        // get a list of system administrators
        public async Task<List<ScampUser>> GetSystemAdministrators()
        {
            if (!(await docdb.IsInitialized))
                return null;

            var admins = from u in docdb.Client.CreateDocumentQuery<ScampUser>(docdb.Collection.SelfLink)
                         where u.IsSystemAdmin == true
                         select u;
            var adminList = await admins.AsDocumentQuery().ToListAsync();
            return adminList;
        }

        public async Task<StyleSettings> GetSiteStyleSettings()
        {
            string rtnResult = string.Empty;

            if (!(await docdb.IsInitialized))
                return null;

            // assumption is there is only one document of type "stylesettings"
            var settingQuery = from s in docdb.Client.CreateDocumentQuery<StyleSettings>(docdb.Collection.SelfLink)
                               where s.Type == "stylesettings"
                               select s;
            // execute query and return results
            return await settingQuery.AsDocumentQuery().FirstOrDefaultAsync(); ;
        }
    }
}
