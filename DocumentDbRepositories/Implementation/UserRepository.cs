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
    public class UserRepository 
    {
        private readonly DocumentClient _client;
        private readonly DocumentCollection _collection;

        public UserRepository(DocumentClient client, DocumentCollection collection)
        {
            _client = client;
            _collection = collection;
        }
        public async Task CreateUser(ScampUser newUser)
		{
			var created = await _client.CreateDocumentAsync(_collection.SelfLink, newUser);
		}

		public Task<ScampUser> GetUser(string userId)
        {
            var users = from u in _client.CreateDocumentQuery<ScampUser>(_collection.SelfLink)
                        where u.Id == userId
                        select u;
            var userList = users.ToList();
            if (userList.Count == 0)
                return Task.FromResult((ScampUser)null);
            return Task.FromResult(userList[0]);           
        }

        public Task<ScampUser> GetUserByID(string IPID)
        {
            var users = from u in _client.CreateDocumentQuery<ScampUser>(_collection.SelfLink)
                        where u.Id == IPID
                        select u;
            var userList = users.ToList();
            if (userList.Count == 0)
                return Task.FromResult((ScampUser)null);
            return Task.FromResult(userList[0]);
        }

        public async Task<ScampUser> UpdateUser(ScampUser user)
        {
            //TODO: likely need to do more here
            Document updated = await _client.ReplaceDocumentAsync(user);

            // cast 
            return (ScampUser)updated;
        }
    }
}
